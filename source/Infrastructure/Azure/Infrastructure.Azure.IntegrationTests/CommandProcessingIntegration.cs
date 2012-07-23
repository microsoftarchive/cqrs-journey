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

namespace Infrastructure.Azure.IntegrationTests.CommandProcessingIntegration
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

    public class given_an_azure_command_bus : given_a_topic_and_subscription
    {
        private const int TimeoutPeriod = 20000;

        [Fact]
        public void when_receiving_command_then_calls_handler()
        {
            var processor = new CommandProcessor(new SubscriptionReceiver(this.Settings, this.Topic, this.Subscription), new JsonTextSerializer());
            var bus = new CommandBus(new TopicSender(this.Settings, this.Topic), new StandardMetadataProvider(), new JsonTextSerializer());

            var e = new ManualResetEventSlim();
            var handler = new FooCommandHandler(e);

            processor.Register(handler);

            processor.Start();

            try
            {
                bus.Send(new FooCommand());

                e.Wait(TimeoutPeriod);

                Assert.True(handler.Called);
            }
            finally
            {
                processor.Stop();
            }
        }

        [Fact]
        public void when_same_handler_handles_multiple_commands_then_gets_called_for_all()
        {
            var processor = new CommandProcessor(new SubscriptionReceiver(this.Settings, this.Topic, this.Subscription), new JsonTextSerializer());
            var bus = new CommandBus(new TopicSender(this.Settings, this.Topic), new StandardMetadataProvider(), new JsonTextSerializer());

            var fooWaiter = new ManualResetEventSlim();
            var barWaiter = new ManualResetEventSlim();
            var handler = new MultipleHandler(fooWaiter, barWaiter);

            processor.Register(handler);

            processor.Start();

            try
            {
                bus.Send(new FooCommand());
                bus.Send(new BarCommand());

                fooWaiter.Wait(TimeoutPeriod);
                barWaiter.Wait(TimeoutPeriod);

                Assert.True(handler.HandledFooCommand);
                Assert.True(handler.HandledBarCommand);
            }
            finally
            {
                processor.Stop();
            }
        }

        [Fact]
        public void when_receiving_not_registered_command_then_ignores()
        {
            var receiverMock = new Mock<SubscriptionReceiver>(this.Settings, this.Topic, this.Subscription, false);
            var processor = new CommandProcessor(receiverMock.Object, new JsonTextSerializer());
            var bus = new CommandBus(new TopicSender(this.Settings, this.Topic), new StandardMetadataProvider(), new JsonTextSerializer());

            var e = new ManualResetEventSlim();
            var handler = new FooCommandHandler(e);

            receiverMock.Protected().Setup("InvokeMessageHandler", ItExpr.IsAny<BrokeredMessage>()).Callback(() => e.Set());

            processor.Register(handler);

            processor.Start();

            try
            {
                bus.Send(new BarCommand());

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
        public void when_sending_multiple_commands_then_calls_all_handlers()
        {
            var processor = new CommandProcessor(new SubscriptionReceiver(this.Settings, this.Topic, this.Subscription), new JsonTextSerializer());
            var bus = new CommandBus(new TopicSender(this.Settings, this.Topic), new StandardMetadataProvider(), new JsonTextSerializer());

            var fooEvent = new ManualResetEventSlim();
            var fooHandler = new FooCommandHandler(fooEvent);

            var barEvent = new ManualResetEventSlim();
            var barHandler = new BarCommandHandler(barEvent);

            processor.Register(fooHandler);
            processor.Register(barHandler);

            processor.Start();

            try
            {
                bus.Send(new ICommand[] { new FooCommand(), new BarCommand() });

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

        [Fact]
        public void when_sending_command_with_delay_then_sets_message_enqueue_time()
        {
            var sender = new Mock<IMessageSender>();
            var bus = new CommandBus(sender.Object, new StandardMetadataProvider(), new JsonTextSerializer());

            BrokeredMessage message = null;
            sender.Setup(x => x.Send(It.IsAny<Func<BrokeredMessage>>()))
                .Callback<Func<BrokeredMessage>>(mf => message = mf());

            bus.Send(new Envelope<ICommand>(new FooCommand()) { Delay = TimeSpan.FromMinutes(5) });

            Assert.NotNull(message);
            Assert.True(message.ScheduledEnqueueTimeUtc > DateTime.UtcNow.Add(TimeSpan.FromMinutes(4)));
        }

        [Fact]
        public void when_sending_multiple_commands_with_delay_then_sets_message_enqueue_time()
        {
            var sender = new Mock<IMessageSender>();
            var bus = new CommandBus(sender.Object, new StandardMetadataProvider(), new JsonTextSerializer());

            BrokeredMessage message = null;
            sender.Setup(x => x.Send(It.IsAny<Func<BrokeredMessage>>()))
                .Callback<Func<BrokeredMessage>>(mf =>
                {
                    var m = mf();
                    if (m.ScheduledEnqueueTimeUtc > DateTime.UtcNow.Add(TimeSpan.FromMinutes(4))) message = m;
                });

            bus.Send(new[] 
            {
                new Envelope<ICommand>(new FooCommand()) { Delay = TimeSpan.FromMinutes(5) }, 
                new Envelope<ICommand>(new BarCommand())
            });

            Assert.NotNull(message);
        }

        [Serializable]
        public class FooCommand : ICommand
        {
            public FooCommand()
            {
                this.Id = Guid.NewGuid();
            }
            public Guid Id { get; set; }
        }

        [Serializable]
        public class BarCommand : ICommand
        {
            public BarCommand()
            {
                this.Id = Guid.NewGuid();
            }
            public Guid Id { get; set; }
        }

        public class MultipleHandler : ICommandHandler<FooCommand>, ICommandHandler<BarCommand>
        {
            private ManualResetEventSlim fooWaiter;
            private ManualResetEventSlim barWaiter;

            public MultipleHandler(ManualResetEventSlim fooWaiter, ManualResetEventSlim barWaiter)
            {
                this.fooWaiter = fooWaiter;
                this.barWaiter = barWaiter;
            }

            public bool HandledBarCommand { get; private set; }
            public bool HandledFooCommand { get; private set; }

            public void Handle(BarCommand command)
            {
                this.HandledBarCommand = true;
                this.barWaiter.Set();
            }

            public void Handle(FooCommand command)
            {
                this.HandledFooCommand = true;
                this.fooWaiter.Set();
            }
        }

        public class FooCommandHandler : ICommandHandler<FooCommand>
        {
            private ManualResetEventSlim e;

            public FooCommandHandler(ManualResetEventSlim e)
            {
                this.e = e;
            }

            public void Handle(FooCommand command)
            {
                this.Called = true;
                e.Set();
            }

            public bool Called { get; set; }
        }

        public class BarCommandHandler : ICommandHandler<BarCommand>
        {
            private ManualResetEventSlim e;

            public BarCommandHandler(ManualResetEventSlim e)
            {
                this.e = e;
            }

            public void Handle(BarCommand command)
            {
                this.Called = true;
                e.Set();
            }

            public bool Called { get; set; }
        }
    }
}
