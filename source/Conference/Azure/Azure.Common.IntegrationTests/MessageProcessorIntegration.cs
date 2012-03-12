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

namespace Azure.IntegrationTests.MessageProcessorIntegration
{
    using System;
    using System.IO;
    using System.Threading;
    using Azure.Messaging;
    using Common;
    using Microsoft.ServiceBus.Messaging;
    using Xunit;

    public class given_a_processor : given_a_topic_and_subscription
    {
        [Fact]
        public void when_type_can_not_be_loaded_then_abandons_message()
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
            var message = new BrokeredMessage(stream, true);
            message.Properties["Type"] = "foo";
            message.Properties["Assembly"] = "bar";
            sender.Send(message);

            waiter.Wait(5000);

            Assert.Null(processor.Payload);
            Assert.Null(processor.PayloadType);
        }

        [Fact]
        public void when_type_can_be_loaded_from_name_then_calls_process_message()
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
            var message = new BrokeredMessage(stream, true);
            message.Properties["Type"] = typeof(string).FullName;
            message.Properties["Assembly"] = typeof(string).Assembly.GetName().Name;
            sender.Send(message);

            waiter.Wait(5000);

            Assert.NotNull(processor.Payload);
            Assert.Equal(typeof(string), processor.PayloadType);
        }

        [Fact]
        public void when_type_can_be_loaded_from_assembly_then_calls_process_message()
        {
            var waiter = new ManualResetEventSlim();
            var sender = new TopicSender(this.Settings, this.Topic);
            var processor = new FakeProcessor(
                waiter,
                new SubscriptionReceiver(this.Settings, this.Topic, this.Subscription),
                new BinarySerializer());

            processor.Start();

            var stream = new MemoryStream();
            new BinarySerializer().Serialize(stream, new Data());
            stream.Position = 0;
            var message = new BrokeredMessage(stream, true);
            message.Properties["Type"] = typeof(Data).FullName;
            message.Properties["Assembly"] = typeof(Data).Assembly.GetName().Name;
            sender.Send(message);

            waiter.Wait(5000);

            Assert.NotNull(processor.Payload);
            Assert.Equal(typeof(Data), processor.PayloadType);
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

        protected override void ProcessMessage(object payload, Type payloadType)
        {
            this.Payload = payload;
            this.PayloadType = payloadType;

            this.waiter.Set();
        }

        public object Payload { get; private set; }
        public Type PayloadType { get; private set; }
    }

    [Serializable]
    public class Data
    {
    }
}