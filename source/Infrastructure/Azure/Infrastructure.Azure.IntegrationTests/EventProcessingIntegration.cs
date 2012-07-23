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

namespace Infrastructure.Azure.IntegrationTests.EventBusIntegration
{
    using System;
    using System.Threading;
    using Infrastructure.Azure.Messaging;
    using Infrastructure.Azure.Messaging.Handling;
    using Infrastructure.Messaging;
    using Infrastructure.Messaging.Handling;
    using Infrastructure.Serialization;
    using Microsoft.ServiceBus.Messaging;
    using Moq;
    using Moq.Protected;
    using Xunit;

    public class given_an_azure_event_bus : given_a_topic_and_subscription
    {
        private const int TimeoutPeriod = 20000;

        [Fact]
        public void when_receiving_event_then_calls_handler()
        {
            var processor = new EventProcessor(new SubscriptionReceiver(this.Settings, this.Topic, this.Subscription), new JsonTextSerializer());
            var bus = new EventBus(new TopicSender(this.Settings, this.Topic), new StandardMetadataProvider(), new JsonTextSerializer());

            var e = new ManualResetEventSlim();
            var handler = new FooEventHandler(e);

            processor.Register(handler);

            processor.Start();

            try
            {
                bus.Publish(new FooEvent());

                e.Wait(TimeoutPeriod);

                Assert.True(handler.Called);
            }
            finally
            {
                processor.Stop();
            }
        }

        [Fact]
        public void when_receiving_event_then_calls_handler_with_envelope()
        {
            var processor = new EventProcessor(new SubscriptionReceiver(this.Settings, this.Topic, this.Subscription), new JsonTextSerializer());
            var bus = new EventBus(new TopicSender(this.Settings, this.Topic), new StandardMetadataProvider(), new JsonTextSerializer());

            var e = new ManualResetEventSlim();
            var handler = new FooEnvelopedEventHandler(e);

            processor.Register(handler);

            processor.Start();

            try
            {
                bus.Publish(new FooEvent());

                e.Wait(TimeoutPeriod);

                Assert.True(handler.Called);
            }
            finally
            {
                processor.Stop();
            }
        }

        [Fact]
        public void when_receiving_event_published_with_message_and_correlation_ids_then_calls_handler_with_envelope()
        {
            var processor = new EventProcessor(new SubscriptionReceiver(this.Settings, this.Topic, this.Subscription), new JsonTextSerializer());
            var bus = new EventBus(new TopicSender(this.Settings, this.Topic), new StandardMetadataProvider(), new JsonTextSerializer());

            var e = new ManualResetEventSlim();
            var handler = new FooEnvelopedEventHandler(e);

            processor.Register(handler);

            processor.Start();

            try
            {
                bus.Publish(new Envelope<IEvent>(new FooEvent()) { CorrelationId = "correlation", MessageId = "message" });

                e.Wait(TimeoutPeriod);

                Assert.True(handler.Called);
                Assert.Equal("correlation", handler.CorrelationId);
                Assert.Equal("message", handler.MessageId);
            }
            finally
            {
                processor.Stop();
            }
        }

        [Fact]
        public void when_receiving_not_registered_event_then_ignores()
        {
            var receiverMock = new Mock<SubscriptionReceiver>(this.Settings, this.Topic, this.Subscription, false);
            var processor = new EventProcessor(receiverMock.Object, new JsonTextSerializer());
            var bus = new EventBus(new TopicSender(this.Settings, this.Topic), new StandardMetadataProvider(), new JsonTextSerializer());

            var e = new ManualResetEventSlim();
            var handler = new FooEventHandler(e);

            receiverMock.Protected().Setup("InvokeMessageHandler", ItExpr.IsAny<BrokeredMessage>()).Callback(() => e.Set());

            processor.Register(handler);

            processor.Start();

            try
            {
                bus.Publish(new BarEvent());

                e.Wait(TimeoutPeriod);
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
            var processor = new EventProcessor(new SubscriptionReceiver(this.Settings, this.Topic, this.Subscription), new JsonTextSerializer());
            var bus = new EventBus(new TopicSender(this.Settings, this.Topic), new StandardMetadataProvider(), new JsonTextSerializer());

            var fooEvent = new ManualResetEventSlim();
            var fooHandler = new FooEventHandler(fooEvent);

            var barEvent = new ManualResetEventSlim();
            var barHandler = new BarEventHandler(barEvent);

            processor.Register(fooHandler);
            processor.Register(barHandler);

            processor.Start();

            try
            {
                bus.Publish(new IEvent[] { new FooEvent(), new BarEvent() });

                fooEvent.Wait(TimeoutPeriod);
                barEvent.Wait(TimeoutPeriod);

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
                this.SourceId = Guid.NewGuid();
            }
            public Guid SourceId { get; set; }
        }

        [Serializable]
        public class BarEvent : IEvent
        {
            public BarEvent()
            {
                this.SourceId = Guid.NewGuid();
            }
            public Guid SourceId { get; set; }
        }

        public class FooEventHandler : IEventHandler<FooEvent>
        {
            private ManualResetEventSlim e;

            public FooEventHandler(ManualResetEventSlim e)
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

        public class FooEnvelopedEventHandler : IEnvelopedEventHandler<FooEvent>
        {
            private ManualResetEventSlim e;

            public FooEnvelopedEventHandler(ManualResetEventSlim e)
            {
                this.e = e;
            }

            public void Handle(Envelope<FooEvent> command)
            {
                this.Called = true;
                this.MessageId = command.MessageId;
                this.CorrelationId = command.CorrelationId;
                e.Set();
            }

            public bool Called { get; set; }

            public string MessageId { get; set; }

            public string CorrelationId { get; set; }
        }

        public class BarEventHandler : IEventHandler<BarEvent>
        {
            private ManualResetEventSlim e;

            public BarEventHandler(ManualResetEventSlim e)
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
