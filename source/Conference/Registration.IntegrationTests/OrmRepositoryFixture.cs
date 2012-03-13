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

namespace Registration.Tests
{
	using System;
	using Registration.Database;
	using Xunit;
	using Common;
	using System.Collections.Generic;
	using System.Data.Entity;
	using Moq;

	public class OrmRepositoryFixture
	{
		public OrmRepositoryFixture()
		{
			using (var context = new TestOrmRepository(Mock.Of<IEventBus>()))
			{
				if (context.Database.Exists()) context.Database.Delete();

				context.Database.Create();
			}
		}

		[Fact]
		public void WhenSavingEntity_ThenCanRetrieveIt()
		{
			var id = Guid.NewGuid();

			using (var context = new TestOrmRepository(Mock.Of<IEventBus>()))
			{
				var aggregate = new OrmTestAggregate(id);
				context.Save(aggregate);
			}

			using (var context = new TestOrmRepository(Mock.Of<IEventBus>()))
			{
				var aggregate = context.Find<OrmTestAggregate>(id);

				Assert.NotNull(aggregate);
			}
		}

		[Fact]
		public void WhenSavingEntityTwice_ThenCanReloadIt()
		{
			var id = Guid.NewGuid();

			using (var context = new TestOrmRepository(Mock.Of<IEventBus>()))
			{
				var aggregate = new OrmTestAggregate(id);
				context.Save(aggregate);
			}

			using (var context = new TestOrmRepository(Mock.Of<IEventBus>()))
			{
				var aggregate = context.Find<OrmTestAggregate>(id);
				aggregate.Title = "CQRS Journey";

				context.Save(aggregate);

				context.Entry(aggregate).Reload();

				Assert.Equal("CQRS Journey", aggregate.Title);
			}
		}

		[Fact]
		public void WhenEntityExposesEvent_ThenRepositoryPublishesIt()
		{
			var bus = new Mock<IEventBus>();
			var events = new List<IEvent>();

			bus.Setup(x => x.Publish(It.IsAny<IEnumerable<IEvent>>()))
				.Callback<IEnumerable<IEvent>>(x => events.AddRange(x));

			var @event = new TestEvent();

			using (var context = new TestOrmRepository(bus.Object))
			{
				var aggregate = new OrmTestAggregate(Guid.NewGuid());
				aggregate.AddEvent(@event);
				context.Save(aggregate);
			}

			Assert.Equal(1, events.Count);
			Assert.True(events.Contains(@event));
		}

		public class TestOrmRepository : OrmRepository
		{
			public TestOrmRepository(IEventBus eventBus)
				: base("TestOrmRepository", eventBus)
			{
			}

			public DbSet<OrmTestAggregate> TestAggregates { get; set; }
		}

		public class TestEvent : IEvent
		{
		}
	}

	public class OrmTestAggregate : IAggregateRoot, IEventPublisher
	{
		private List<IEvent> events = new List<IEvent>();

		protected OrmTestAggregate() { }

		public OrmTestAggregate(Guid id)
		{
			this.Id = id;
		}

		public Guid Id { get; set; }
		public string Title { get; set; }

		public void AddEvent(IEvent @event)
		{
			this.events.Add(@event);
		}

		public IEnumerable<IEvent> Events { get { return this.events; } }
	}
}
