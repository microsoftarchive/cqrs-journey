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
	using System.Transactions;

    public class OrmRepository : DbContext, IRepository
    {
		private IEventBus eventBus;

        public OrmRepository()
            : this("ConferenceRegistration")
        {
        }

		public OrmRepository(string nameOrConnectionString)
			// TODO: we need the actual handlers for the in-memory buses here!!!
			: this(nameOrConnectionString, new MemoryEventBus())
		{
		}

		public OrmRepository(string nameOrConnectionString, IEventBus eventBus)
		{
			this.eventBus = eventBus;
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

				var publisher = aggregate as IEventPublisher;
				if (publisher != null)
					this.eventBus.Publish(publisher.Events);

				scope.Complete();
			}
        }

        // Define the available entity sets for the database.
        public virtual DbSet<ConferenceSeatsAvailability> ConferenceSeats { get; private set; }
    }
}
