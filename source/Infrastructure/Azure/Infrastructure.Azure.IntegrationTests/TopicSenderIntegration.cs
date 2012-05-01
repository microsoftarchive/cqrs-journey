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

namespace Infrastructure.Azure.IntegrationTests.TopicSenderIntegration
{
    using System;
    using System.Collections.Generic;
    using Infrastructure.Azure.Messaging;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;
    using Xunit;

    public class given_a_topic_sender : IDisposable
    {
        private MessagingSettings settings;
        private string topic = "Test-" + Guid.NewGuid().ToString();
        private SubscriptionClient subscriptionClient;
        private TopicSender sut;

        public given_a_topic_sender()
        {
            this.settings = InfrastructureSettings.ReadMessaging("Settings.xml");
            this.sut = new TopicSender(this.settings, this.topic);

            var tokenProvider = TokenProvider.CreateSharedSecretTokenProvider(settings.TokenIssuer, settings.TokenAccessKey);
            var serviceUri = ServiceBusEnvironment.CreateServiceUri(settings.ServiceUriScheme, settings.ServiceNamespace, settings.ServicePath);

            var manager = new NamespaceManager(serviceUri, tokenProvider);
            manager.CreateSubscription(topic, "Test");

            var messagingFactory = MessagingFactory.Create(serviceUri, tokenProvider);
            this.subscriptionClient = messagingFactory.CreateSubscriptionClient(topic, "Test");

        }

        public void Dispose()
        {
            this.settings.TryDeleteTopic(this.topic);
        }

        [Fact]
        public void when_sending_message_async_then_succeeds()
        {
            var payload = Guid.NewGuid().ToString();

            sut.SendAsync(new BrokeredMessage(payload));

            var message = subscriptionClient.Receive(TimeSpan.FromSeconds(5));
            Assert.Equal(payload, message.GetBody<string>());
        }

        [Fact]
        public void when_sending_message_batch_async_then_succeeds()
        {
            var payload1 = Guid.NewGuid().ToString();
            var payload2 = Guid.NewGuid().ToString();

            sut.SendAsync(new[] { new BrokeredMessage(payload1), new BrokeredMessage(payload2) });

            var messages = new List<string>
                               {
                                   this.subscriptionClient.Receive(TimeSpan.FromSeconds(5)).GetBody<string>(),
                                   this.subscriptionClient.Receive(TimeSpan.FromSeconds(2)).GetBody<string>()
                               };
            Assert.Contains(payload1, messages);
            Assert.Contains(payload2, messages);
        }

        [Fact]
        public void when_sending_message_then_succeeds()
        {
            var payload = Guid.NewGuid().ToString();
            sut.Send(() => new BrokeredMessage(payload));

            var message = subscriptionClient.Receive();
            Assert.Equal(payload, message.GetBody<string>());
        }
    }
}
