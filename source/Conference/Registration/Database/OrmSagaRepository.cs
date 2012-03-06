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

namespace Registration.Database
{
	using System;
	using System.Data.Entity;
	using System.Data;
	using Common;
	using System.Linq;
	using System.Transactions;

	public class OrmSagaRepository : DbContext, ISagaRepository
	{
		private ICommandBus commandBus;

		public OrmSagaRepository()
			: this("ConferenceRegistrationSagas")
		{
		}

		public OrmSagaRepository(string nameOrConnectionString)
			: this(nameOrConnectionString, new MemoryCommandBus())
		{
		}

		public OrmSagaRepository(ICommandBus commandBus)
			: this("ConferenceRegistrationSagas", commandBus)
		{
		}

		public OrmSagaRepository(string nameOrConnectionString, ICommandBus commandBus)
			: base(nameOrConnectionString)
		{
			this.commandBus = commandBus;
		}

		public T Find<T>(Guid id) where T : class, IAggregateRoot
		{
			return this.Set<T>().Find(id);
		}

		public void Save<T>(T aggregate) where T : class, IAggregateRoot
		{
			var entry = this.Entry(aggregate);

			// Add if the object was not loaded from the repository.
			if (entry.State == EntityState.Detached) this.Set<T>().Add(aggregate);

			// Otherwise, do nothing as the ORM already tracks 
			// attached entities that need to be saved (or not).

			using (var scope = new TransactionScope())
			{	
				this.SaveChanges();

				var commandPublisher = aggregate as ICommandPublisher;
				if (commandPublisher != null)
				    this.commandBus.Send(commandPublisher.Commands);

				scope.Complete();
			}
		}

		public IQueryable<T> Query<T>() where T : class, IAggregateRoot
		{
			return this.Set<T>();
		}

		// Define the available entity sets for the database.
		public virtual DbSet<RegistrationProcessSaga> RegistrationProcesses { get; private set; }
	}
}
