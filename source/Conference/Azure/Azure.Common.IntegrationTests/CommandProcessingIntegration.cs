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

namespace Azure.IntegrationTests.CommandProcessingIntegration
{
    using System;
    using System.Threading;
    using Azure;
    using Azure.Messaging;
    using Common;
    using Xunit;

    public class given_an_azure_command_bus : given_a_topic_and_subscription
    {
        [Fact]
        public void when_receiving_command_then_calls_handler()
        {
            var processor = new CommandProcessor(new SubscriptionReceiver(this.Settings, this.Topic, this.Subscription), new BinarySerializer());
            var bus = new CommandBus(new TopicSender(this.Settings, this.Topic), new MetadataProvider(), new BinarySerializer());

            var e = new ManualResetEvent(false);
            var handler = new FooCommandHandler(e);

            processor.Register(handler);

            processor.Start();

            try
            {
                bus.Send(new FooCommand());

                e.WaitOne(5000);

                Assert.True(handler.Called);
            }
            finally
            {
                processor.Stop();
            }
        }

        [Fact]
        public void when_receiving_not_registered_command_then_ignores()
        {
            var receiver = new SubscriptionReceiver(this.Settings, this.Topic, this.Subscription);
            var processor = new CommandProcessor(receiver, new BinarySerializer());
            var bus = new CommandBus(new TopicSender(this.Settings, this.Topic), new MetadataProvider(), new BinarySerializer());

            var e = new ManualResetEvent(false);
            var handler = new FooCommandHandler(e);

            receiver.MessageReceived += (sender, args) => e.Set();

            processor.Register(handler);

            processor.Start();

            try
            {
                bus.Send(new BarCommand());

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
        public void when_sending_multiple_commands_then_calls_all_handlers()
        {
            var processor = new CommandProcessor(new SubscriptionReceiver(this.Settings, this.Topic, this.Subscription), new BinarySerializer());
            var bus = new CommandBus(new TopicSender(this.Settings, this.Topic), new MetadataProvider(), new BinarySerializer());

            var fooEvent = new ManualResetEvent(false);
            var fooHandler = new FooCommandHandler(fooEvent);

            var barEvent = new ManualResetEvent(false);
            var barHandler = new BarCommandHandler(barEvent);

            processor.Register(fooHandler);
            processor.Register(barHandler);

            processor.Start();

            try
            {
                bus.Send(new ICommand[] { new FooCommand(), new BarCommand() });

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

        public class FooCommandHandler : ICommandHandler<FooCommand>
        {
            private ManualResetEvent e;

            public FooCommandHandler(ManualResetEvent e)
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
            private ManualResetEvent e;

            public BarCommandHandler(ManualResetEvent e)
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
