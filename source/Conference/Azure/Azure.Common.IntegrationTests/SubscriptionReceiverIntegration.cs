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

namespace Azure.IntegrationTests
{
    using System;
    using Azure.Messaging;
    using Xunit;

    public class SubscriptionReceiverIntegration
    {
        public class given_existing_topic : given_messaging_settings, IDisposable
        {
            private string topic = Guid.NewGuid().ToString();

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
                new SubscriptionReceiver(this.Settings, this.topic, Guid.NewGuid().ToString());
            }
        }

        public class given_existing_subscription : given_messaging_settings, IDisposable
        {
            private string topic = Guid.NewGuid().ToString();
            private string subscription = Guid.NewGuid().ToString();

            public given_existing_subscription()
            {
                this.Settings.CreateSubscription(topic, subscription);
            }

            public void Dispose()
            {
                this.Settings.TryDeleteSubscription(topic, subscription);
                this.Settings.TryDeleteTopic(topic);
            }

            [Fact]
            public void when_receiver_created_then_ignores_error_on_recreating_subscription()
            {
                new SubscriptionReceiver(this.Settings, this.topic, Guid.NewGuid().ToString());
            }
        }

        public class given_a_receiver : given_a_topic_and_subscription
        {
            [Fact]
            public void when_starting_twice_then_throws()
            {
                var receiver = new SubscriptionReceiver(this.Settings, this.Topic, this.Subscription);

                receiver.Start();

                Assert.Throws<InvalidOperationException>(() => receiver.Start());
            }

            [Fact]
            public void when_stopping_without_starting_then_throws()
            {
                var receiver = new SubscriptionReceiver(this.Settings, this.Topic, this.Subscription);

                Assert.Throws<InvalidOperationException>(() => receiver.Stop());
            }

            [Fact]
            public void when_disposing_not_started_then_no_op()
            {
                var receiver = new SubscriptionReceiver(this.Settings, this.Topic, this.Subscription);

                receiver.Dispose();
            }

            [Fact]
            public void when_disposing_started_then_stops()
            {
                var receiver = new SubscriptionReceiver(this.Settings, this.Topic, this.Subscription);

                receiver.Start();
                receiver.Dispose();

                Assert.Throws<InvalidOperationException>(() => receiver.Stop());
            }

        }
    }
}
