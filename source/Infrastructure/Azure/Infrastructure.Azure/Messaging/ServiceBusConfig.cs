// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
// http://go.microsoft.com/fwlink/p/?LinkID=258575
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

namespace Infrastructure.Azure.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Infrastructure.Azure.Instrumentation;
    using Infrastructure.Azure.Messaging.Handling;
    using Infrastructure.Messaging.Handling;
    using Infrastructure.Serialization;
    using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.ServiceBus;
    using Microsoft.Practices.TransientFaultHandling;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;

    public class ServiceBusConfig
    {
        private const string RuleName = "Custom";
        private bool initialized;
        private ServiceBusSettings settings;

        public ServiceBusConfig(ServiceBusSettings settings)
        {
            this.settings = settings;
        }

        public void Initialize()
        {
            var retryStrategy = new Incremental(3, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
            var retryPolicy = new RetryPolicy<ServiceBusTransientErrorDetectionStrategy>(retryStrategy);
            var tokenProvider = TokenProvider.CreateSharedSecretTokenProvider(settings.TokenIssuer, settings.TokenAccessKey);
            var serviceUri = ServiceBusEnvironment.CreateServiceUri(settings.ServiceUriScheme, settings.ServiceNamespace, settings.ServicePath);
            var namespaceManager = new NamespaceManager(serviceUri, tokenProvider);

            this.settings.Topics.AsParallel().ForAll(topic =>
            {
                retryPolicy.ExecuteAction(() => CreateTopicIfNotExists(namespaceManager, topic));
                topic.Subscriptions.AsParallel().ForAll(subscription =>
                {
                    retryPolicy.ExecuteAction(() => CreateSubscriptionIfNotExists(namespaceManager, topic, subscription));
                    retryPolicy.ExecuteAction(() => UpdateRules(namespaceManager, topic, subscription));
                });
            });

            // Execute migration support actions only after all the previous ones have been completed.
            foreach (var topic in this.settings.Topics)
            {
                foreach (var action in topic.MigrationSupport)
                {
                    retryPolicy.ExecuteAction(() => UpdateSubscriptionIfExists(namespaceManager, topic, action));
                }
            }

            this.initialized = true;
        }

        // Can't really infer the topic from the subscription, since subscriptions of the same 
        // name can exist across different topics (i.e. "all" currently)
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Instrumentation disposed by receiver")]
        public EventProcessor CreateEventProcessor(string subscription, IEventHandler handler, ITextSerializer serializer, bool instrumentationEnabled = false)
        {
            if (!this.initialized)
                throw new InvalidOperationException("Service bus configuration has not been initialized.");

            TopicSettings topicSettings = null;
            SubscriptionSettings subscriptionSettings = null;

            foreach (var settings in this.settings.Topics.Where(t => t.IsEventBus))
            {
                subscriptionSettings = settings.Subscriptions.Find(s => s.Name == subscription);
                if (subscriptionSettings != null)
                {
                    topicSettings = settings;
                    break;
                }
            }

            if (subscriptionSettings == null)
            {
                throw new ArgumentOutOfRangeException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "Subscription '{0}' has not been registered for an event bus topic in the service bus configuration.",
                        subscription));
            }

            IMessageReceiver receiver;

            if (subscriptionSettings.RequiresSession)
            {
                var instrumentation = new SessionSubscriptionReceiverInstrumentation(subscription, instrumentationEnabled);
                try
                {
                    receiver = (IMessageReceiver)new SessionSubscriptionReceiver(this.settings, topicSettings.Path, subscription, true, instrumentation);
                }
                catch
                {
                    instrumentation.Dispose();
                    throw;
                }
            }
            else
            {
                var instrumentation = new SubscriptionReceiverInstrumentation(subscription, instrumentationEnabled);
                try
                {
                    receiver = (IMessageReceiver)new SubscriptionReceiver(this.settings, topicSettings.Path, subscription, true, instrumentation);
                }
                catch
                {
                    instrumentation.Dispose();
                    throw;
                }
            }

            EventProcessor processor;
            try
            {
                processor = new EventProcessor(receiver, serializer);
            }
            catch
            {
                using (receiver as IDisposable) { }
                throw;
            }

            try
            {
                processor.Register(handler);

                return processor;
            }
            catch
            {
                processor.Dispose();
                throw;
            }
        }

        private void CreateTopicIfNotExists(NamespaceManager namespaceManager, TopicSettings topic)
        {
            var topicDescription =
                new TopicDescription(topic.Path)
                {
                    RequiresDuplicateDetection = true,
                    DuplicateDetectionHistoryTimeWindow = topic.DuplicateDetectionHistoryTimeWindow,
                };

            try
            {
                namespaceManager.CreateTopic(topicDescription);
            }
            catch (MessagingEntityAlreadyExistsException) { }
        }

        private void CreateSubscriptionIfNotExists(NamespaceManager namespaceManager, TopicSettings topic, SubscriptionSettings subscription)
        {
            var subscriptionDescription =
                new SubscriptionDescription(topic.Path, subscription.Name)
                {
                    RequiresSession = subscription.RequiresSession,
                    LockDuration = TimeSpan.FromSeconds(150),
                };

            try
            {
                namespaceManager.CreateSubscription(subscriptionDescription);
            }
            catch (MessagingEntityAlreadyExistsException) { }
        }

        private static void UpdateSubscriptionIfExists(NamespaceManager namespaceManager, TopicSettings topic, UpdateSubscriptionIfExists action)
        {
            if (string.IsNullOrWhiteSpace(action.Name)) throw new ArgumentException("action");
            if (string.IsNullOrWhiteSpace(action.SqlFilter)) throw new ArgumentException("action");

            UpdateSqlFilter(namespaceManager, action.SqlFilter, action.Name, topic.Path);
        }

        private static void UpdateRules(NamespaceManager namespaceManager, TopicSettings topic, SubscriptionSettings subscription)
        {
            string sqlExpression = null;
            if (!string.IsNullOrWhiteSpace(subscription.SqlFilter))
            {
                sqlExpression = subscription.SqlFilter;
            }

            UpdateSqlFilter(namespaceManager, sqlExpression, subscription.Name, topic.Path);
        }

        private static void UpdateSqlFilter(NamespaceManager namespaceManager, string sqlExpression, string subscriptionName, string topicPath)
        {
            bool needsReset = false;
            List<RuleDescription> existingRules;
            try
            {
                existingRules = namespaceManager.GetRules(topicPath, subscriptionName).ToList();
            }
            catch (MessagingEntityNotFoundException)
            {
                // the subscription does not exist, no need to update rules.
                return;
            }
            if (existingRules.Count != 1)
            {
                needsReset = true;
            }
            else
            {
                var existingRule = existingRules.First();
                if (sqlExpression != null && existingRule.Name == RuleDescription.DefaultRuleName)
                {
                    needsReset = true;
                }
                else if (sqlExpression == null && existingRule.Name != RuleDescription.DefaultRuleName)
                {
                    needsReset = true;
                }
                else if (sqlExpression != null && existingRule.Name != RuleName)
                {
                    needsReset = true;
                }
                else if (sqlExpression != null && existingRule.Name == RuleName)
                {
                    var filter = existingRule.Filter as SqlFilter;
                    if (filter == null || filter.SqlExpression != sqlExpression)
                    {
                        needsReset = true;
                    }
                }
            }

            if (needsReset)
            {
                MessagingFactory factory = null;
                try
                {
                    factory = MessagingFactory.Create(namespaceManager.Address, namespaceManager.Settings.TokenProvider);
                    SubscriptionClient client = null;
                    try
                    {
                        client = factory.CreateSubscriptionClient(topicPath, subscriptionName);

                        // first add the default rule, so no new messages are lost while we are updating the subscription
                        TryAddRule(client, new RuleDescription(RuleDescription.DefaultRuleName, new TrueFilter()));

                        // then delete every rule but the Default one
                        foreach (var existing in existingRules.Where(x => x.Name != RuleDescription.DefaultRuleName))
                        {
                            TryRemoveRule(client, existing.Name);
                        }

                        if (sqlExpression != null)
                        {
                            // Add the desired rule.
                            TryAddRule(client, new RuleDescription(RuleName, new SqlFilter(sqlExpression)));

                            // once the desired rule was added, delete the default rule.
                            TryRemoveRule(client, RuleDescription.DefaultRuleName);
                        }
                    }
                    finally
                    {
                        if (client != null) client.Close();
                    }
                }
                finally
                {
                    if (factory != null) factory.Close();
                }
            }
        }

        private static void TryAddRule(SubscriptionClient client, RuleDescription rule)
        {
            // try / catch is because there could be other processes initializing at the same time.
            try
            {
                client.AddRule(rule);
            }
            catch (MessagingEntityAlreadyExistsException) { }
        }

        private static void TryRemoveRule(SubscriptionClient client, string ruleName)
        {
            // try / catch is because there could be other processes initializing at the same time.
            try
            {
                client.RemoveRule(ruleName);
            }
            catch (MessagingEntityNotFoundException) { }
        }
    }
}
