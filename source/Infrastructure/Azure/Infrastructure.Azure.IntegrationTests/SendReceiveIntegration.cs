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
    public class given_a_sender_and_receiver : IDisposable
    {
        private MessagingSettings settings;
        private string topic = "Test-" + Guid.NewGuid().ToString();
        private string subscription = "Test-" + Guid.NewGuid().ToString();

        public given_a_sender_and_receiver()
        {
            this.settings = InfrastructureSettings.ReadMessaging("Settings.xml");
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
            Data data = new Data { Id = Guid.NewGuid(), Title = "Foo" };
            Data received = null;
            using (var receiver = new SubscriptionReceiver(this.settings, this.topic, this.subscription))
            {
                var signal = new ManualResetEventSlim();

                receiver.MessageReceived += (o, e) =>
                {
                    received = e.Message.GetBody<Data>();
                    signal.Set();
                };

                receiver.Start();

                sender.SendAsync(() => new BrokeredMessage(data));

                signal.Wait();
            }

            Assert.NotNull(received);
            Assert.Equal(data.Id, received.Id);
            Assert.Equal(data.Title, received.Title);
        }

        [Fact]
        public void when_gets_transient_error_on_receive_then_retries()
        {
            var sender = new TopicSender(this.settings, this.topic);
            Data data = new Data { Id = Guid.NewGuid(), Title = "Foo" };
            Data received = null;
            using (var receiver = new TestableSubscriptionReceiver(this.settings, this.topic, this.subscription, new Incremental(3, TimeSpan.Zero, TimeSpan.Zero), new Incremental(0, TimeSpan.Zero, TimeSpan.Zero)))
            {
                var attempt = 0;
                var currentDelegate = receiver.DoReceiveMessageDelegate;
                receiver.DoReceiveMessageDelegate =
                    () =>
                    {
                        if (attempt++ < 1) { throw new TimeoutException(); }
                        return currentDelegate();
                    };

                var signal = new ManualResetEventSlim();

                receiver.MessageReceived += (o, e) =>
                {
                    received = e.Message.GetBody<Data>();
                    signal.Set();
                };

                receiver.Start();

                sender.SendAsync(() => new BrokeredMessage(data));

                Assert.True(signal.Wait(TimeSpan.FromSeconds(10)), "Test timed out");
            }

            Assert.NotNull(received);
            Assert.Equal(data.Id, received.Id);
            Assert.Equal(data.Title, received.Title);
        }

        [Fact]
        public void when_gets_transient_error_several_times_on_receive_then_retries_until_failure()
        {
            var attempt = 0;
            var sender = new TopicSender(this.settings, this.topic);
            Data data = new Data { Id = Guid.NewGuid(), Title = "Foo" };
            Data received = null;
            using (var receiver = new TestableSubscriptionReceiver(this.settings, this.topic, this.subscription, new Incremental(3, TimeSpan.Zero, TimeSpan.Zero), new Incremental(0, TimeSpan.Zero, TimeSpan.Zero)))
            {
                var signal = new ManualResetEventSlim();

                receiver.DoReceiveMessageDelegate =
                    () =>
                    {
                        if (attempt++ == 3) { signal.Set(); }
                        throw new TimeoutException();
                    };

                receiver.MessageReceived += (o, e) =>
                {
                    received = e.Message.GetBody<Data>();
                };

                receiver.Start();

                sender.SendAsync(() => new BrokeredMessage(data));

                Assert.True(signal.Wait(TimeSpan.FromSeconds(10)), "Test timed out");
                Thread.Sleep(TimeSpan.FromMilliseconds(500));
            }

            Thread.Sleep(TimeSpan.FromSeconds(1));
            Assert.Null(received);
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

    public class TestableSubscriptionReceiver : SubscriptionReceiver
    {
        public TestableSubscriptionReceiver(MessagingSettings settings, string topic, string subscription, RetryStrategy background, RetryStrategy blocking)
            : base(settings, topic, subscription, background, blocking)
        {
            this.DoReceiveMessageDelegate = base.DoReceiveMessage;
        }

        public Func<BrokeredMessage> DoReceiveMessageDelegate;

        protected override BrokeredMessage DoReceiveMessage()
        {
            return this.DoReceiveMessageDelegate();
        }
    }
}
