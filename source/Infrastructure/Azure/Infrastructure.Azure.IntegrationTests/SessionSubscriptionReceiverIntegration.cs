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

namespace Infrastructure.Azure.IntegrationTests.SessionSubscriptionReceiverIntegration
{
    using System;
    using System.Threading;
    using Infrastructure.Azure.Messaging;
    using Microsoft.ServiceBus.Messaging;
    using Xunit;

    public class given_existing_topic : given_messaging_settings, IDisposable
    {
        private string topic = "cqrsjourney-" + Guid.NewGuid().ToString();

        public given_existing_topic()
        {
            this.Settings.CreateTopic(topic);
        }

        public void Dispose()
        {
            this.Settings.TryDeleteTopic(topic);
        }

        [Fact]
        public void when_receiver_created_then_ignores_error_on_recreating_topic()
        {
            new SessionSubscriptionReceiver(this.Settings, this.topic, Guid.NewGuid().ToString());
        }
    }

    public class given_existing_subscription : given_a_topic_and_subscription, IDisposable
    {
        [Fact]
        public void when_receiver_created_then_ignores_error_on_recreating_subscription()
        {
            new SessionSubscriptionReceiver(this.Settings, this.Topic, this.Subscription);
        }
    }

    public class given_a_receiver : given_messaging_settings
    {
        private string Topic;
        private string Subscription;

        public given_a_receiver()
        {
            this.Topic = "cqrsjourney-" + Guid.NewGuid().ToString();
            this.Subscription = "cqrsjourney-" + Guid.NewGuid().ToString();

            // Creates the topic too.
            this.Settings.CreateSubscription(new SubscriptionDescription(this.Topic, this.Subscription) { RequiresSession = true });
        }

        [Fact]
        public void when_sending_message_with_session_then_session_receiver_gets_it()
        {
            var client = this.Settings.CreateSubscriptionClient(this.Topic, this.Subscription);
            var sender = this.Settings.CreateTopicClient(this.Topic);
            var signal = new ManualResetEventSlim();
            var body = Guid.NewGuid().ToString();

            var receiver = new SessionSubscriptionReceiver(this.Settings, this.Topic, this.Subscription);

            sender.Send(new BrokeredMessage(Guid.NewGuid().ToString()));
            sender.Send(new BrokeredMessage(body) { SessionId = "foo" });

            var received = "";

            receiver.MessageReceived += (s, e) =>
            {
                received = e.Message.GetBody<string>();
                signal.Set();
            };

            receiver.Start();

            signal.Wait();

            receiver.Stop();

            Assert.Equal(body, received);
        }

        [Fact]
        public void when_starting_twice_then_ignores_second_request()
        {
            var receiver = new SessionSubscriptionReceiver(this.Settings, this.Topic, this.Subscription);

            receiver.Start();

            receiver.Start();
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
    }
}
