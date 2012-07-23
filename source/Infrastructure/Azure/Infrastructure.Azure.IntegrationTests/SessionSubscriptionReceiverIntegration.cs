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

namespace Infrastructure.Azure.IntegrationTests.SessionSubscriptionReceiverIntegration
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Threading;
    using Infrastructure.Azure.Messaging;
    using Microsoft.ServiceBus.Messaging;
    using Xunit;

    public class given_a_receiver : given_messaging_settings, IDisposable
    {
        private string Topic;
        private string Subscription;

        public given_a_receiver()
        {
            this.Topic = "cqrsjourney-test-" + Guid.NewGuid().ToString();
            this.Subscription = "test-" + Guid.NewGuid().ToString();

            // This creates the topic too.
            this.Settings.CreateSubscription(new SubscriptionDescription(this.Topic, this.Subscription) { RequiresSession = true });
        }

        public void Dispose()
        {
            this.Settings.TryDeleteTopic(this.Topic);
        }

        [Fact]
        public void when_sending_message_with_session_then_session_receiver_gets_it()
        {
            var sender = this.Settings.CreateTopicClient(this.Topic);
            var signal = new ManualResetEventSlim();
            var body = Guid.NewGuid().ToString();

            var receiver = new SessionSubscriptionReceiver(this.Settings, this.Topic, this.Subscription);

            sender.Send(new BrokeredMessage(Guid.NewGuid().ToString()));
            sender.Send(new BrokeredMessage(body) { SessionId = "foo" });

            var received = "";

            receiver.Start(
                m =>
                {
                    received = m.GetBody<string>();
                    signal.Set();
                    return MessageReleaseAction.CompleteMessage;
                });

            signal.Wait();

            receiver.Stop();

            Assert.Equal(body, received);
        }

        [Fact]
        public void when_sending_message_with_session_then_session_receiver_gets_both_messages_fast()
        {
            var sender = this.Settings.CreateTopicClient(this.Topic);
            var signal = new AutoResetEvent(false);
            var body1 = Guid.NewGuid().ToString();
            var body2 = Guid.NewGuid().ToString();
            var stopWatch = new Stopwatch();

            var receiver = new SessionSubscriptionReceiver(this.Settings, this.Topic, this.Subscription);

            sender.Send(new BrokeredMessage(body1) { SessionId = "foo" });
            sender.Send(new BrokeredMessage(body2) { SessionId = "bar" });

            var received = new ConcurrentBag<string>();

            receiver.Start(
                m =>
                {
                    received.Add(m.GetBody<string>());
                    signal.Set();
                    return MessageReleaseAction.CompleteMessage;
                });

            signal.WaitOne();
            stopWatch.Start();
            signal.WaitOne();
            stopWatch.Stop();

            receiver.Stop();

            Assert.Contains(body1, received);
            Assert.Contains(body2, received);
            Assert.InRange(stopWatch.Elapsed, TimeSpan.Zero, TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void when_sending_message_with_same_session_at_different_times_then_session_receiver_gets_all()
        {
            var sender = this.Settings.CreateTopicClient(this.Topic);
            var signal = new AutoResetEvent(false);
            var body1 = Guid.NewGuid().ToString();
            var body2 = Guid.NewGuid().ToString();
            var body3 = Guid.NewGuid().ToString();

            var receiver = new SessionSubscriptionReceiver(this.Settings, this.Topic, this.Subscription);
            var stopWatch = new Stopwatch();
            var received = new ConcurrentBag<string>();

            receiver.Start(
                m =>
                {
                    received.Add(m.GetBody<string>());
                    signal.Set();
                    return MessageReleaseAction.CompleteMessage;
                });

            sender.Send(new BrokeredMessage(body1) { SessionId = "foo" });
            stopWatch.Start();
            signal.WaitOne();

            sender.Send(new BrokeredMessage(body2) { SessionId = "bar" });
            signal.WaitOne();

            sender.Send(new BrokeredMessage(body3) { SessionId = "foo" });
            signal.WaitOne();
            stopWatch.Stop();

            receiver.Stop();

            Assert.Contains(body1, received);
            Assert.Contains(body2, received);
            Assert.Contains(body3, received);
            Assert.InRange(stopWatch.Elapsed, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        }

        [Fact]
        public void when_starting_twice_then_ignores_second_request()
        {
            var receiver = new SessionSubscriptionReceiver(this.Settings, this.Topic, this.Subscription);

            receiver.Start(m => MessageReleaseAction.CompleteMessage);
            receiver.Start(m => MessageReleaseAction.CompleteMessage);

            receiver.Stop();
        }

        [Fact]
        public void when_stopping_without_starting_then_ignores_request()
        {
            var receiver = new SessionSubscriptionReceiver(this.Settings, this.Topic, this.Subscription);

            receiver.Stop();
        }

        [Fact]
        public void when_disposing_not_started_then_no_op()
        {
            var receiver = new SessionSubscriptionReceiver(this.Settings, this.Topic, this.Subscription);

            receiver.Dispose();
        }

        [Fact]
        public void when_sending_message_to_different_sessions_then_processes_concurrently()
        {
            var sender = this.Settings.CreateTopicClient(this.Topic);
            var message1received = new ManualResetEvent(false);
            var message2received = new ManualResetEvent(false);
            var body1 = Guid.NewGuid().ToString();
            var body2 = Guid.NewGuid().ToString();

            var receiver = new SessionSubscriptionReceiver(this.Settings, this.Topic, this.Subscription);
            var stopWatch = new Stopwatch();

            receiver.Start(
                m =>
                {
                    var msg = m.GetBody<string>();
                    if (msg == body1)
                    {
                        message1received.Set();
                        // do not continue until we verify that the 2nd message is received, hence implying parallelism
                        message2received.WaitOne();
                    }
                    else
                    {
                        message2received.Set();
                    }

                    return MessageReleaseAction.CompleteMessage;
                });

            sender.Send(new BrokeredMessage(body1) { SessionId = "foo" });
            stopWatch.Start();
            Assert.True(message1received.WaitOne(10000));

            sender.Send(new BrokeredMessage(body2) { SessionId = "bar" });
            Assert.True(message2received.WaitOne(10000));
            stopWatch.Stop();

            receiver.Stop();

            Assert.InRange(stopWatch.Elapsed, TimeSpan.Zero, TimeSpan.FromSeconds(10));
        }
    }
}
