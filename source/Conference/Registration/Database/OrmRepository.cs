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
    using System.Transactions;
    using Common;

    public class OrmRepository : DbContext, IRepository
    {
        private IEventBus eventBus;

        public OrmRepository()
            : this("ConferenceRegistration")
        {
        }

        public OrmRepository(string nameOrConnectionString)
            : this(nameOrConnectionString, new MemoryEventBus())
        {
        }

        public OrmRepository(IEventBus eventBus)
            : this("ConferenceRegistration", eventBus)
        {
        }

        public OrmRepository(string nameOrConnectionString, IEventBus eventBus)
            : base(nameOrConnectionString)
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

            if (entry.State == System.Data.EntityState.Detached)
                this.Set<T>().Add(aggregate);

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
        public virtual DbSet<SeatsAvailability> ConferenceSeats { get; private set; }
        public virtual DbSet<Order> Orders { get; private set; }
    }
}
