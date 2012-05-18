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
    using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.ServiceBus;
    using Microsoft.Practices.TransientFaultHandling;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;

    public class ServiceBusConfig
    {
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

            foreach (var topic in this.settings.Topics)
            {
                retryPolicy.ExecuteAction(() => CreateTopicIfNotExists(namespaceManager, topic));
                foreach (var subscription in topic.Subscriptions)
                {
                    retryPolicy.ExecuteAction(() => CreateSubscriptionIfNotExists(namespaceManager, topic, subscription));
                }
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
            try
            {
                namespaceManager.CreateSubscription(topic.Path, subscription.Name);
            }
            catch (MessagingEntityAlreadyExistsException) { }
        }

    }
}
