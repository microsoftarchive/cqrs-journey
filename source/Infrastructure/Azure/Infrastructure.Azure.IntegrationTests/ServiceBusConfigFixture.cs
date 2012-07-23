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

namespace Infrastructure.Azure.IntegrationTests.ServiceBusConfigFixture
{
    using System;
    using System.Linq;
    using System.Threading;
    using Infrastructure.Azure.Messaging;
    using Infrastructure.Messaging;
    using Infrastructure.Messaging.Handling;
    using Infrastructure.Serialization;
    using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.ServiceBus;
    using Microsoft.Practices.TransientFaultHandling;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;
    using Moq;
    using Xunit;

    public class given_service_bus_config : IDisposable
    {
        private ServiceBusSettings settings;
        private NamespaceManager namespaceManager;
        private RetryPolicy<ServiceBusTransientErrorDetectionStrategy> retryPolicy;
        private ServiceBusConfig sut;

        public given_service_bus_config()
        {
            System.Diagnostics.Trace.Listeners.Clear();
            this.settings = InfrastructureSettings.Read("Settings.xml").ServiceBus;
            foreach (var topic in this.settings.Topics)
            {
                topic.Path = topic.Path + Guid.NewGuid().ToString();
            }

            var tokenProvider = TokenProvider.CreateSharedSecretTokenProvider(this.settings.TokenIssuer, this.settings.TokenAccessKey);
            var serviceUri = ServiceBusEnvironment.CreateServiceUri(this.settings.ServiceUriScheme, this.settings.ServiceNamespace, this.settings.ServicePath);
            this.namespaceManager = new NamespaceManager(serviceUri, tokenProvider);

            var retryStrategy = new Incremental(3, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
            this.retryPolicy = new RetryPolicy<ServiceBusTransientErrorDetectionStrategy>(retryStrategy);

            this.sut = new ServiceBusConfig(this.settings);

            Cleanup();
        }

        public void Dispose()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            foreach (var topic in this.settings.Topics)
            {
                retryPolicy.ExecuteAction(() =>
                {
                    try { this.namespaceManager.DeleteTopic(topic.Path); }
                    catch (MessagingEntityNotFoundException) { }
                });
            }
        }

        [Fact]
        public void when_initialized_then_creates_topics()
        {
            this.sut.Initialize();

            var topics = this.settings.Topics.Select(topic => new
                {
                    Topic = topic,
                    Description = this.retryPolicy.ExecuteAction(() => this.namespaceManager.GetTopic(topic.Path))
                }).ToList();

            Assert.False(topics.Any(tuple => tuple.Description == null));
        }

        [Fact]
        public void when_initialized_then_creates_subscriptions_with_filters()
        {
            this.sut.Initialize();

            var subscriptions = this.settings.Topics
                .SelectMany(topic => topic.Subscriptions.Select(subscription => new { Topic = topic, Subscription = subscription }))
                .Select(tuple => new
                    {
                        Subscription = tuple.Subscription,
                        Description = this.retryPolicy.ExecuteAction(() => this.namespaceManager.GetSubscription(tuple.Topic.Path, tuple.Subscription.Name)),
                        Rule = this.retryPolicy.ExecuteAction(() => this.namespaceManager.GetRules(tuple.Topic.Path, tuple.Subscription.Name).FirstOrDefault(x => x.Name == "Custom"))
                    }).ToList();

            Assert.True(subscriptions.All(tuple => tuple.Description != null));
            Assert.True(subscriptions.All(tuple => tuple.Subscription.RequiresSession == tuple.Description.RequiresSession));
            Assert.True(subscriptions.All(tuple =>
                (string.IsNullOrWhiteSpace(tuple.Subscription.SqlFilter) && tuple.Rule == null)
                || (!string.IsNullOrWhiteSpace(tuple.Subscription.SqlFilter) && ((SqlFilter)tuple.Rule.Filter).SqlExpression == tuple.Subscription.SqlFilter)));
        }

        [Fact]
        public void when_initialized_then_subscriptions_updates_existing_filters()
        {
            settings.Topics = settings.Topics.Take(1).ToList();
            var topic = settings.Topics.First();
            topic.Subscriptions = topic.Subscriptions.Take(1).ToList();
            var subscription = topic.Subscriptions.First();
            subscription.SqlFilter = "TypeName='MyTypeA'";
            this.sut.Initialize();

            var rule = this.retryPolicy.ExecuteAction(() => this.namespaceManager.GetRules(topic.Path, subscription.Name).Single());
            Assert.Equal("TypeName='MyTypeA'", ((SqlFilter)rule.Filter).SqlExpression);

            subscription.SqlFilter = "TypeName='MyTypeB'";
            this.sut.Initialize();

            rule = this.retryPolicy.ExecuteAction(() => this.namespaceManager.GetRules(topic.Path, subscription.Name).Single());
            Assert.Equal("TypeName='MyTypeB'", ((SqlFilter)rule.Filter).SqlExpression);
        }

        [Fact]
        public void when_creating_processor_with_uninitialized_config_then_throws()
        {
            Assert.Throws<InvalidOperationException>(() => this.sut.CreateEventProcessor("all", Mock.Of<IEventHandler>(), Mock.Of<ITextSerializer>()));
        }

        [Fact]
        public void when_creating_processor_but_no_event_bus_topic_then_throws()
        {
            foreach (var topic in this.settings.Topics)
            {
                topic.IsEventBus = false;
            }
            this.sut.Initialize();

            Assert.Throws<ArgumentOutOfRangeException>(() => this.sut.CreateEventProcessor("all", Mock.Of<IEventHandler>(), Mock.Of<ITextSerializer>()));
        }

        [Fact]
        public void when_creating_processor_for_unconfigured_subscription_then_throws()
        {
            this.sut.Initialize();

            Assert.Throws<ArgumentOutOfRangeException>(() => this.sut.CreateEventProcessor("a", Mock.Of<IEventHandler>(), Mock.Of<ITextSerializer>()));
        }

        [Fact]
        public void when_creating_processor_then_receives_from_specified_subscription()
        {
            this.sut.Initialize();

            var waiter = new ManualResetEventSlim();
            var handler = new Mock<IEventHandler<AnEvent>>();
            var serializer = new JsonTextSerializer();
            var ev = new AnEvent();
            handler.Setup(x => x.Handle(It.IsAny<AnEvent>()))
                .Callback(() => waiter.Set());

            var processor = this.sut.CreateEventProcessor("log", handler.Object, serializer);

            processor.Start();

            var sender = new TopicSender(this.settings, this.settings.Topics.First(t => t.Path.StartsWith("conference/events")).Path);
            var bus = new EventBus(sender, new StandardMetadataProvider(), serializer);
            bus.Publish(ev);

            waiter.Wait(5000);

            handler.Verify(x => x.Handle(It.Is<AnEvent>(e => e.SourceId == ev.SourceId)));
        }

        [Fact]
        public void runs_migration_support_actions()
        {
            settings.Topics = settings.Topics.Take(1).ToList();
            var topic = settings.Topics.First();
            topic.Subscriptions = topic.Subscriptions.Take(1).ToList();
            topic.MigrationSupport.Clear();
            var subscription = topic.Subscriptions.First();
            subscription.SqlFilter = "TypeName='MyTypeA'";
            this.sut.Initialize();

            var rule = this.retryPolicy.ExecuteAction(() => this.namespaceManager.GetRules(topic.Path, subscription.Name).Single());
            Assert.Equal("TypeName='MyTypeA'", ((SqlFilter)rule.Filter).SqlExpression);

            topic.MigrationSupport.Add(new UpdateSubscriptionIfExists { Name = subscription.Name, SqlFilter = "1=0" });
            this.sut.Initialize();

            rule = this.retryPolicy.ExecuteAction(() => this.namespaceManager.GetRules(topic.Path, subscription.Name).Single());
            Assert.Equal("1=0", ((SqlFilter)rule.Filter).SqlExpression);
        }

        [Fact]
        public void migration_support_action_noops_if_subscription_does_not_exist()
        {
            settings.Topics = settings.Topics.Take(1).ToList();
            var topic = settings.Topics.First();
            topic.Subscriptions.Clear();
            topic.MigrationSupport.Clear();
            topic.MigrationSupport.Add(new UpdateSubscriptionIfExists { Name = "TestSubscription", SqlFilter = "1=0" });
            this.sut.Initialize();

            var subscriptions = this.retryPolicy.ExecuteAction(() => this.namespaceManager.GetSubscriptions(topic.Path)).ToList();
            Assert.Equal(0, subscriptions.Count);
        }

        public class AnEvent : IEvent
        {
            public AnEvent()
            {
                this.SourceId = Guid.NewGuid();
            }
            public Guid SourceId { get; set; }
        }
    }
}
