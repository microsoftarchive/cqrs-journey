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

namespace Azure.IntegrationTests.EventBusIntegration
{
    using System;
    using System.Threading;
    using Azure;
    using Azure.Messaging;
    using Common;
    using Xunit;

    public class given_an_azure_event_bus : given_a_topic_and_subscription
    {
        [Fact]
        public void when_receiving_event_then_calls_handler()
        {
            var processor = new EventProcessor(new SubscriptionReceiver(this.Settings, this.Topic, this.Subscription), new BinarySerializer());
            var bus = new EventBus(new TopicSender(this.Settings, this.Topic), new BinarySerializer());

            var e = new ManualResetEvent(false);
            var handler = new FooEventHandler(e);

            processor.Register(handler);

            processor.Start();

            try
            {
                bus.Publish(new FooEvent());

                e.WaitOne(5000);

                Assert.True(handler.Called);
            }
            finally
            {
                processor.Stop();
            }
        }

        [Fact]
        public void when_receiving_not_registered_event_then_ignores()
        {
            var receiver = new SubscriptionReceiver(this.Settings, this.Topic, this.Subscription);
            var processor = new EventProcessor(receiver, new BinarySerializer());
            var bus = new EventBus(new TopicSender(this.Settings, this.Topic), new BinarySerializer());

            var e = new ManualResetEvent(false);
            var handler = new FooEventHandler(e);

            receiver.MessageReceived += (sender, args) => e.Set();

            processor.Register(handler);

            processor.Start();

            try
            {
                bus.Publish(new BarEvent());

                e.WaitOne(5000);
                // Give the other event handler some time.
                Thread.Sleep(100);

                Assert.False(handler.Called);
            }
            finally
            {
                processor.Stop();
            }
        }

        [Fact]
        public void when_sending_multiple_events_then_calls_all_handlers()
        {
            var processor = new EventProcessor(new SubscriptionReceiver(this.Settings, this.Topic, this.Subscription), new BinarySerializer());
            var bus = new EventBus(new TopicSender(this.Settings, this.Topic), new BinarySerializer());

            var fooEvent = new ManualResetEvent(false);
            var fooHandler = new FooEventHandler(fooEvent);

            var barEvent = new ManualResetEvent(false);
            var barHandler = new BarEventHandler(barEvent);

            processor.Register(fooHandler);
            processor.Register(barHandler);

            processor.Start();

            try
            {
                bus.Publish(new IEvent[] { new FooEvent(), new BarEvent() });

                fooEvent.WaitOne(5000);
                barEvent.WaitOne(5000);

                Assert.True(fooHandler.Called);
                Assert.True(barHandler.Called);
            }
            finally
            {
                processor.Stop();
            }
        }

        [Serializable]
        public class FooEvent : IEvent
        {
            public FooEvent()
            {
                this.Id = Guid.NewGuid();
            }
            public Guid Id { get; set; }
        }

        [Serializable]
        public class BarEvent : IEvent
        {
            public BarEvent()
            {
                this.Id = Guid.NewGuid();
            }
            public Guid Id { get; set; }
        }

        public class FooEventHandler : IEventHandler<FooEvent>
        {
            private ManualResetEvent e;

            public FooEventHandler(ManualResetEvent e)
            {
                this.e = e;
            }

            public void Handle(FooEvent command)
            {
                this.Called = true;
                e.Set();
            }

            public bool Called { get; set; }
        }

        public class BarEventHandler : IEventHandler<BarEvent>
        {
            private ManualResetEvent e;

            public BarEventHandler(ManualResetEvent e)
            {
                this.e = e;
            }

            public void Handle(BarEvent command)
            {
                this.Called = true;
                e.Set();
            }

            public bool Called { get; set; }
        }
    }
}
