// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// Copyright (c) Microsoft Corporation and contributors http://cqrsjourney.github.com/contributors/members
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

namespace Azure.Messaging
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;

    /// <summary>
    /// Implements an asynchronous sender of messages to an Azure 
    /// service bus topic.
    /// </summary>
    public class TopicSender : IMessageSender
    {
        private readonly TokenProvider tokenProvider;
        private readonly Uri serviceUri;
        private readonly MessagingSettings settings;
        private string topic;

        /// <summary>
        /// Initializes a new instance of the <see cref="TopicSender"/> class, 
        /// automatically creating the given topic if it does not exist.
        /// </summary>
        public TopicSender(MessagingSettings settings, string topic)
        {
            this.settings = settings;
            this.topic = topic;

            this.tokenProvider = TokenProvider.CreateSharedSecretTokenProvider(settings.TokenIssuer, settings.TokenAccessKey);
            this.serviceUri = ServiceBusEnvironment.CreateServiceUri(settings.ServiceUriScheme, settings.ServiceNamespace, settings.ServicePath);

            try
            {
                new NamespaceManager(this.serviceUri, this.tokenProvider)
                    .CreateTopic(
                        new TopicDescription(topic)
                        {
                            RequiresDuplicateDetection = true,
                            DuplicateDetectionHistoryTimeWindow = TimeSpan.FromMinutes(30)
                        });
            }
            catch (MessagingEntityAlreadyExistsException)
            { }
        }

        /// <summary>
        /// Asynchronously sends the specified message.
        /// </summary>
        public void Send(BrokeredMessage message)
        {
            var factory = MessagingFactory.Create(this.serviceUri, this.tokenProvider);
            var client = factory.CreateTopicClient(this.topic);

            // TODO: what about retries? Watch-out for message reuse. Need to recreate it before retry.
            // Always send async.
            client.Async(message, client.BeginSend, client.EndSend);
        }

        public void Send(IEnumerable<BrokeredMessage> messages)
        {
            // TODO: batch/transactional sending? Is it just wrapping with a TransactionScope?
            foreach (var message in messages)
            {
                this.Send(message);
            }
        }

    }
}
