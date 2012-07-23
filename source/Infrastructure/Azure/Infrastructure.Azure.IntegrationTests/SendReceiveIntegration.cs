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

namespace Infrastructure.Azure.IntegrationTests.SendReceiveIntegration
{
    using System;
    using System.Runtime.Serialization;
    using System.Threading;
    using Infrastructure.Azure.Messaging;
    using Microsoft.Practices.TransientFaultHandling;
    using Microsoft.ServiceBus.Messaging;
    using Xunit;

    /// <summary>
    /// Tests the send/receive behavior.
    /// </summary>
    public class given_a_sender_and_receiver : given_a_topic_and_subscription
    {
        [Fact]
        public void when_sending_message_then_can_receive_it()
        {
            var sender = new TopicSender(this.Settings, this.Topic);
            Data data = new Data { Id = Guid.NewGuid(), Title = "Foo" };
            Data received = null;
            using (var receiver = new SubscriptionReceiver(this.Settings, this.Topic, this.Subscription))
            {
                var signal = new ManualResetEventSlim();

                receiver.Start(
                    m =>
                    {
                        received = m.GetBody<Data>();
                        signal.Set();
                        return MessageReleaseAction.CompleteMessage;
                    });

                sender.SendAsync(() => new BrokeredMessage(data));

                signal.Wait();
            }

            Assert.NotNull(received);
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
