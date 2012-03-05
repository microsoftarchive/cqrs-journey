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
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using Xunit;
	using Common;

	public class SagaRepoFixture
	{
		[Fact]
		public void WhenSagaPublishesEvent_ThenAnotherSagaIsRehidrated()
		{
			var repo = new MemorySagaRepository();
			var events = new MemoryEventBus(
				new RegistrationSagaUserDeactivatedHandler(repo));
			var commands = new MemoryCommandBus(
				new BeginRegistrationCommandHandler(repo),
				new DeactivateUserCommandHandler(events));

			var userId = Guid.NewGuid();

			commands.Send(new BeginRegistrationCommand(userId));

			// Saga is created.
			Assert.Equal(1, repo.Query<RegistrationSaga>().Count());

			commands.Send(new DeactivateUserCommand(userId));

			Assert.True(events.Events.OfType<UserDeactivated>().Any());

			Assert.True(repo.Query<RegistrationSaga>().Single(x => x.UserId == userId).IsDeleted);
		}

		class BeginRegistrationCommand : ICommand
		{
			public BeginRegistrationCommand(Guid userId)
			{
				this.Id = Guid.NewGuid();
				this.UserId = userId;
			}

			public Guid Id { get; set; }
			public Guid UserId { get; set; }
		}

		class BeginRegistrationCommandHandler : IHandleCommand<BeginRegistrationCommand>
		{
			private ISagaRepository repo;

			public BeginRegistrationCommandHandler(ISagaRepository repo)
			{
				this.repo = repo;
			}

			public void Handle(BeginRegistrationCommand command)
			{
				var saga = new RegistrationSaga(command.Id, command.UserId);

				this.repo.Save(saga);
			}
		}

		class DeactivateUserCommand : ICommand
		{
			public DeactivateUserCommand(Guid userId)
			{
				this.Id = Guid.NewGuid();
				this.UserId = userId;
			}

			public Guid Id { get; private set; }
			public Guid UserId { get; set; }
		}

		class DeactivateUserCommandHandler : IHandleCommand<DeactivateUserCommand>
		{
			private IEventBus events;

			public DeactivateUserCommandHandler(IEventBus events)
			{
				this.events = events;
			}

			public void Handle(DeactivateUserCommand command)
			{
				// Invoke some biz logic in the domain.
				this.events.Publish(new UserDeactivated(command.UserId));
			}
		}

		class UserDeactivated : IEvent
		{
			public UserDeactivated(Guid userId)
			{
				this.UserId = userId;
			}

			public Guid UserId { get; set; }
		}

		class RegistrationSagaUserDeactivatedHandler : IHandleEvent<UserDeactivated>
		{
			private ISagaRepository repo;

			public RegistrationSagaUserDeactivatedHandler(ISagaRepository repo)
			{
				this.repo = repo;
			}

			public void Handle(UserDeactivated @event)
			{
				// Route the event to the corresponding saga by correlation id.
				var saga = this.repo.Query<RegistrationSaga>().SingleOrDefault(x => !x.IsDeleted && x.UserId == @event.UserId);
				if (saga != null)
				{
					saga.Handle(@event);
					this.repo.Save(saga);
				}
			}
		}

		class RegistrationSaga : IAggregateRoot, IHandleEvent<UserDeactivated>
		{
			public RegistrationSaga(Guid id, Guid userId)
			{
				this.Id = id;
				this.UserId = userId;
			}

			protected RegistrationSaga() { }

			// Dependencies.
			public ICommandBus Commands { get; set; }
			public IEventBus Events { get; set; }

			public Guid Id { get; private set; }
			public Guid UserId { get; private set; }
			public bool IsDeleted { get; private set; }

			public void Handle(UserDeactivated @event)
			{
				if (@event.UserId == this.UserId)
					this.IsDeleted = true;
			}
		}

		class MemorySagaRepository : ISagaRepository
		{
			private List<IAggregateRoot> aggregates = new List<IAggregateRoot>();

			public IQueryable<T> Query<T>() where T : IAggregateRoot
			{
				return this.aggregates.OfType<T>().AsQueryable();
			}

			public T Find<T>(Guid id) where T : class, IAggregateRoot
			{
				return this.aggregates.OfType<T>().FirstOrDefault(x => x.Id == id);
			}

			public void Save<T>(T aggregate) where T : class, IAggregateRoot
			{
				if (!this.aggregates.Contains(aggregate))
					this.aggregates.Add(aggregate);
			}
		}

		class MemoryCommandBus : ICommandBus
		{
			private object[] handlers;
			private List<ICommand> commands = new List<ICommand>();

			public MemoryCommandBus(params object[] handlers)
			{
				this.handlers = handlers;
			}

			public void Send(ICommand command)
			{
				this.commands.Add(command);

				var handlerType = typeof(IHandleCommand<>).MakeGenericType(command.GetType());

				foreach (dynamic handler in this.handlers
					.Where(x => handlerType.IsAssignableFrom(x.GetType())))
				{
					handler.Handle((dynamic)command);
				}
			}

			public IEnumerable<ICommand> Commands
			{
				get { return this.commands; }
			}
		}

		class MemoryEventBus : IEventBus
		{
			private object[] handlers;
			private List<IEvent> events = new List<IEvent>();

			public MemoryEventBus(params object[] handlers)
			{
				this.handlers = handlers;
			}

			public void Publish(IEvent @event)
			{
				this.events.Add(@event);

				var handlerType = typeof(IHandleEvent<>).MakeGenericType(@event.GetType());

				foreach (dynamic handler in this.handlers
					.Where(x => handlerType.IsAssignableFrom(x.GetType())))
				{
					handler.Handle((dynamic)@event);
				}
			}

			public IEnumerable<IEvent> Events { get { return this.events; } }
		}
	}
}
