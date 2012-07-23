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

namespace Infrastructure.Azure.IntegrationTests.MessageProcessorIntegration
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using Infrastructure.Azure.Messaging;
    using Infrastructure.Azure.Messaging.Handling;
    using Infrastructure.Serialization;
    using Microsoft.ServiceBus.Messaging;
    using Moq;
    using Moq.Protected;
    using Xunit;

    public class given_a_processor : given_a_topic_and_subscription
    {
        [Fact]
        public void when_message_received_then_calls_process_message()
        {
            var waiter = new ManualResetEventSlim();
            var sender = new TopicSender(this.Settings, this.Topic);
            var processor = new FakeProcessor(
                waiter,
                new SubscriptionReceiver(this.Settings, this.Topic, this.Subscription),
                new JsonTextSerializer());

            processor.Start();

            var messageId = Guid.NewGuid().ToString();
            var correlationId = Guid.NewGuid().ToString();
            var stream = new MemoryStream();
            new JsonTextSerializer().Serialize(new StreamWriter(stream), "Foo");
            stream.Position = 0;
            sender.SendAsync(() => new BrokeredMessage(stream, true) { MessageId = messageId, CorrelationId = correlationId });

            waiter.Wait(5000);

            Assert.NotNull(processor.Payload);
            Assert.Equal(messageId, processor.MessageId);
            Assert.Equal(correlationId, processor.CorrelationId);
        }

        [Fact]
        public void when_processing_throws_then_sends_message_to_dead_letter()
        {
            var failCount = 0;
            var waiter = new ManualResetEventSlim();
            var sender = new TopicSender(this.Settings, this.Topic);
            var processor = new Mock<MessageProcessor>(
                new SubscriptionReceiver(this.Settings, this.Topic, this.Subscription), new JsonTextSerializer()) { CallBase = true };

            processor.Protected()
                .Setup("ProcessMessage", ItExpr.IsAny<string>(), ItExpr.IsAny<object>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>())
                .Callback(() =>
                {
                    failCount++;
                    if (failCount == 5)
                        waiter.Set();

                    throw new ArgumentException();
                });

            processor.Object.Start();

            var stream = new MemoryStream();
            new JsonTextSerializer().Serialize(new StreamWriter(stream), "Foo");
            stream.Position = 0;
            sender.SendAsync(() => new BrokeredMessage(stream, true));

            waiter.Wait(5000);

            var deadReceiver = this.Settings.CreateMessageReceiver(this.Topic, this.Subscription);

            var deadMessage = deadReceiver.Receive(TimeSpan.FromSeconds(5));

            processor.Object.Dispose();

            Assert.NotNull(deadMessage);
            var data = new JsonTextSerializer().Deserialize(new StreamReader(deadMessage.GetBody<Stream>()));

            Assert.Equal("Foo", (string)data);
        }

        [Fact]
        public void when_message_fails_to_deserialize_then_dead_letters_message()
        {
            var waiter = new ManualResetEventSlim();
            var sender = new TopicSender(this.Settings, this.Topic);
            var processor = new FakeProcessor(
                waiter,
                new SubscriptionReceiver(this.Settings, this.Topic, this.Subscription),
                new JsonTextSerializer());

            processor.Start();

            var data = new JsonTextSerializer().Serialize(new Data());
            data = data.Replace(typeof(Data).FullName, "Some.TypeName.Cannot.Resolve");
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            stream.Position = 0;

            sender.SendAsync(() => new BrokeredMessage(stream, true));

            waiter.Wait(5000);

            var deadReceiver = this.Settings.CreateMessageReceiver(this.Topic, this.Subscription);
            var deadMessage = deadReceiver.Receive(TimeSpan.FromSeconds(5));

            processor.Dispose();

            Assert.NotNull(deadMessage);
            var payload = new StreamReader(deadMessage.GetBody<Stream>()).ReadToEnd();

            Assert.Contains("Some.TypeName.Cannot.Resolve", payload);
        }
    }

    public class FakeProcessor : MessageProcessor
    {
        private ManualResetEventSlim waiter;

        public FakeProcessor(ManualResetEventSlim waiter, IMessageReceiver receiver, ITextSerializer serializer)
            : base(receiver, serializer)
        {
            this.waiter = waiter;
        }

        protected override void ProcessMessage(string traceIdentifier, object payload, string messageId, string correlationId)
        {
            this.Payload = payload;
            this.MessageId = messageId;
            this.CorrelationId = correlationId;

            this.waiter.Set();
        }

        public object Payload { get; private set; }
        public string MessageId { get; private set; }
        public string CorrelationId { get; private set; }
    }

    [Serializable]
    public class Data
    {
    }
}
