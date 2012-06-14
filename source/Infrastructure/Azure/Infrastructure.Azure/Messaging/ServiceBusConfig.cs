// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
// http://cqrsjourney.github.com/contributors/members
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

namespace Infrastructure.Azure.Messaging
{
    using System;
    using System.Globalization;
    using Infrastructure.Azure.Messaging.Handling;
    using Infrastructure.Messaging.Handling;
    using Infrastructure.Serialization;
    using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.ServiceBus;
    using Microsoft.Practices.TransientFaultHandling;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;

    public class ServiceBusConfig
    {
        private bool initialized;
        private ServiceBusSettings settings;
        private MessagingFactory messagingFactory;

        public ServiceBusConfig(ServiceBusSettings settings)
        {
            this.settings = settings;
        }

        public MessagingFactory MessagingFactory
        {
            get { return this.messagingFactory; }
        }

        public void Initialize()
        {
            var retryStrategy = new Incremental(3, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
            var retryPolicy = new RetryPolicy<ServiceBusTransientErrorDetectionStrategy>(retryStrategy);
            var tokenProvider = TokenProvider.CreateSharedSecretTokenProvider(settings.TokenIssuer, settings.TokenAccessKey);
            var serviceUri = ServiceBusEnvironment.CreateServiceUri(settings.ServiceUriScheme, settings.ServiceNamespace, settings.ServicePath);
            var namespaceManager = new NamespaceManager(serviceUri, tokenProvider);

            this.messagingFactory = MessagingFactory.Create(serviceUri, tokenProvider);

            foreach (var topic in this.settings.Topics)
            {
                retryPolicy.ExecuteAction(() => CreateTopicIfNotExists(namespaceManager, topic));
                foreach (var subscription in topic.Subscriptions)
                {
                    retryPolicy.ExecuteAction(() => CreateSubscriptionIfNotExists(namespaceManager, topic, subscription));
                }
            }

            this.initialized = true;
        }

        // Can't really infer the topic from the subscription, since subscriptions of the same 
        // name can exist across different topics (i.e. "all" currently)
        public EventProcessor CreateEventProcessor(string subscription, IEventHandler handler, ITextSerializer serializer)
        {
            if (!this.initialized)
                throw new InvalidOperationException("Service bus configuration has not been initialized.");

            var topicSettings = this.settings.Topics.Find(x => x.IsEventBus);
            if (topicSettings == null)
                throw new ArgumentOutOfRangeException("No topic has been marked with the IsEventBus attribute. Cannot create event processor.");

            var subscriptionSettings = topicSettings.Subscriptions.Find(x => x.Name == subscription);
            if (subscriptionSettings == null)
                throw new ArgumentOutOfRangeException(string.Format(
                    CultureInfo.CurrentCulture,
                    "Subscription '{0}' for topic '{1}' has not been registered in the service bus configuration.",
                    subscription, topicSettings.Path));

            var receiver = subscriptionSettings.RequiresSession ?
                (IMessageReceiver)new SessionSubscriptionReceiver(this.messagingFactory, topicSettings.Path, subscription) :
                (IMessageReceiver)new SubscriptionReceiver(this.messagingFactory, topicSettings.Path, subscription);

            var processor = new EventProcessor(receiver, serializer);
            processor.Register(handler);

            return processor;
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
                    RequiresSession = subscription.RequiresSession
                };

            try
            {
                namespaceManager.CreateSubscription(subscriptionDescription);
            }
            catch (MessagingEntityAlreadyExistsException) { }
        }
    }
}
