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

namespace Azure.IntegrationTests.MessageProcessorIntegration
{
    using System;
    using System.IO;
    using System.Threading;
    using Azure.Messaging;
    using Infrastructure.Serialization;
    using Microsoft.ServiceBus.Messaging;
    using Moq;
    using Moq.Protected;
    using Xunit;

    public class given_a_processor : given_a_topic_and_subscription
    {
        [Fact]
        public void when_message_receivedthen_calls_process_message()
        {
            var waiter = new ManualResetEventSlim();
            var sender = new TopicSender(this.Settings, this.Topic);
            var processor = new FakeProcessor(
                waiter,
                new SubscriptionReceiver(this.Settings, this.Topic, this.Subscription),
                new BinarySerializer());

            processor.Start();

            var stream = new MemoryStream();
            new BinarySerializer().Serialize(stream, "Foo");
            stream.Position = 0;
            sender.Send(new BrokeredMessage(stream, true));

            waiter.Wait(5000);

            Assert.NotNull(processor.Payload);
        }

        [Fact]
        public void when_processing_throws_then_sends_message_to_dead_letter()
        {
            var waiter = new ManualResetEventSlim();
            var sender = new TopicSender(this.Settings, this.Topic);
            var processor = new Mock<MessageProcessor>(
                new SubscriptionReceiver(this.Settings, this.Topic, this.Subscription), new BinarySerializer()) { CallBase = true };

            processor.Protected()
                .Setup("ProcessMessage", ItExpr.IsAny<object>())
                .Callback(() =>
                {
                    waiter.Set();
                    throw new ArgumentException();
                });

            processor.Object.Start();

            var stream = new MemoryStream();
            new BinarySerializer().Serialize(stream, "Foo");
            stream.Position = 0;
            sender.Send(new BrokeredMessage(stream, true));

            waiter.Wait(5000);

            var deadReceiver = this.Settings.CreateMessageReceiver(this.Topic, this.Subscription);

            var deadMessage = deadReceiver.Receive(TimeSpan.FromSeconds(5));

            processor.Object.Dispose();

            Assert.NotNull(deadMessage);
            var data = new BinarySerializer().Deserialize(deadMessage.GetBody<Stream>());

            Assert.Equal("Foo", (string)data);
        }
    }

    public class FakeProcessor : MessageProcessor
    {
        private ManualResetEventSlim waiter;

        public FakeProcessor(ManualResetEventSlim waiter, IMessageReceiver receiver, ISerializer serializer)
            : base(receiver, serializer)
        {
            this.waiter = waiter;
        }

        protected override void ProcessMessage(object payload)
        {
            this.Payload = payload;

            this.waiter.Set();
        }

        public object Payload { get; private set; }
    }

    [Serializable]
    public class Data
    {
    }
}
