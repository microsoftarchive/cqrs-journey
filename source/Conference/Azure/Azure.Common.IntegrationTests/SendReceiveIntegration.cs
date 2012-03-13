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

namespace Azure.IntegrationTests.SendReceiveIntegration
{
    using System;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Xml.Serialization;
    using Azure.Messaging;
    using Microsoft.ServiceBus.Messaging;
    using Xunit;

    /// <summary>
    /// Tests the send/receive behavior.
    /// </summary>
    public class given_a_sender_and_receiver : IDisposable
    {
        private static readonly XmlSerializer serializer = new XmlSerializer(typeof(MessagingSettings));
        private MessagingSettings settings;
        private string topic = Guid.NewGuid().ToString();
        private string subscription = Guid.NewGuid().ToString();

        public given_a_sender_and_receiver()
        {
            using (var file = File.OpenRead("Settings.xml"))
            {
                this.settings = (MessagingSettings)serializer.Deserialize(file);
            }
        }

        public void Dispose()
        {
            this.settings.TryDeleteSubscription(this.topic, this.subscription);
            this.settings.TryDeleteTopic(this.topic);
        }

        [Fact]
        public void when_sending_message_then_can_receive_it()
        {
            var sender = new TopicSender(this.settings, this.topic);
            var receiver = new SubscriptionReceiver(this.settings, this.topic, this.subscription);
            var signal = new ManualResetEvent(false);

            var message = default(BrokeredMessage);

            receiver.MessageReceived += (o, e) =>
            {
                message = e.Message;
                signal.Set();
            };

            receiver.Start();

            var data = new Data { Id = Guid.NewGuid(), Title = "Foo" };
            sender.Send(new BrokeredMessage(data));

            signal.WaitOne(5000);

            Assert.NotNull(message);

            var received = message.GetBody<Data>();

            Assert.Equal(data.Id, received.Id);
            Assert.Equal(data.Title, received.Title);
        }
    }

    [DataContract]
    public class Data
    {
        [DataMember]
        public Guid Id { get; set; }
        [DataMember]
        public string Title { get; set; }
    }
}
