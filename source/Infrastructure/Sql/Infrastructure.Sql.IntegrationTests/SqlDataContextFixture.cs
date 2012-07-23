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

namespace Infrastructure.Sql.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Linq;
    using Infrastructure.Database;
    using Infrastructure.Messaging;
    using Infrastructure.Sql.Database;
    using Moq;
    using Xunit;

    public class SqlDataContextFixture : IDisposable
    {
        public SqlDataContextFixture()
        {
            using (var dbContext = new TestDbContext())
            {
                dbContext.Database.Delete();
                dbContext.Database.Create();
            }
        }

        public void Dispose()
        {
            using (var dbContext = new TestDbContext())
            {
                dbContext.Database.Delete();
            }
        }

        [Fact]
        public void WhenSavingAggregateRoot_ThenCanRetrieveIt()
        {
            var id = Guid.NewGuid();

            using (var context = new SqlDataContext<TestAggregateRoot>(() => new TestDbContext(), Mock.Of<IEventBus>()))
            {
                var aggregateRoot = new TestAggregateRoot(id) { Title = "test" };

                context.Save(aggregateRoot);
            }

            using (var context = new SqlDataContext<TestAggregateRoot>(() => new TestDbContext(), Mock.Of<IEventBus>()))
            {
                var aggregateRoot = context.Find(id);

                Assert.NotNull(aggregateRoot);
                Assert.Equal("test", aggregateRoot.Title);
            }
        }

        [Fact]
        public void WhenSavingEntityTwice_ThenCanReloadIt()
        {
            var id = Guid.NewGuid();

            using (var context = new SqlDataContext<TestAggregateRoot>(() => new TestDbContext(), Mock.Of<IEventBus>()))
            {
                var aggregateRoot = new TestAggregateRoot(id);
                context.Save(aggregateRoot);
            }

            using (var context = new SqlDataContext<TestAggregateRoot>(() => new TestDbContext(), Mock.Of<IEventBus>()))
            {
                var aggregateRoot = context.Find(id);
                aggregateRoot.Title = "test";

                context.Save(aggregateRoot);
            }

            using (var context = new SqlDataContext<TestAggregateRoot>(() => new TestDbContext(), Mock.Of<IEventBus>()))
            {
                var aggregateRoot = context.Find(id);

                Assert.Equal("test", aggregateRoot.Title);
            }
        }

        [Fact]
        public void WhenEntityExposesEvent_ThenRepositoryPublishesIt()
        {
            var busMock = new Mock<IEventBus>();
            var events = new List<IEvent>();

            busMock
                .Setup(x => x.Publish(It.IsAny<IEnumerable<Envelope<IEvent>>>()))
                .Callback<IEnumerable<Envelope<IEvent>>>(x => events.AddRange(x.Select(e => e.Body)));

            var @event = new TestEvent();

            using (var context = new SqlDataContext<TestEventPublishingAggregateRoot>(() => new TestDbContext(), busMock.Object))
            {
                var aggregate = new TestEventPublishingAggregateRoot(Guid.NewGuid());
                aggregate.AddEvent(@event);
                context.Save(aggregate);
            }

            Assert.Equal(1, events.Count);
            Assert.True(events.Contains(@event));
        }

        public class TestDbContext : DbContext
        {
            public TestDbContext()
                : base("TestDbContext")
            {
            }

            public DbSet<TestAggregateRoot> TestAggregateRoots { get; set; }

            public DbSet<TestEventPublishingAggregateRoot> TestEventPublishingAggregateRoot { get; set; }
        }

        public class TestEvent : IEvent
        {
            public Guid SourceId { get; set; }
        }
    }

    public class TestAggregateRoot : IAggregateRoot
    {
        protected TestAggregateRoot() { }

        public TestAggregateRoot(Guid id)
        {
            this.Id = id;
        }

        public Guid Id { get; set; }
        public string Title { get; set; }
    }

    public class TestEventPublishingAggregateRoot : TestAggregateRoot, IEventPublisher
    {
        private List<IEvent> events = new List<IEvent>();

        protected TestEventPublishingAggregateRoot() { }

        public TestEventPublishingAggregateRoot(Guid id)
            : base(id)
        {
        }

        public void AddEvent(IEvent @event)
        {
            this.events.Add(@event);
        }

        public IEnumerable<IEvent> Events { get { return this.events; } }
    }
}
