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

namespace Registration.Database
{
    using System;
    using System.Linq;
    using Common;

    public sealed class SeatsAvailabilityRepository : IRepository<SeatsAvailability>, IDisposable
    {
        private readonly string nameOrConnectionString;
        private readonly IEventBus eventBus;

        public SeatsAvailabilityRepository()
            : this(new MemoryEventBus())
        {
        }

        public SeatsAvailabilityRepository(IEventBus eventBus)
            : this(null, eventBus)
        {
            this.eventBus = eventBus;
        }

        public SeatsAvailabilityRepository(string nameOrConnectionString, IEventBus eventBus)
        {
            this.nameOrConnectionString = nameOrConnectionString;
            this.eventBus = eventBus;
        }

        private RegistrationDbContext dbContext;
        private RegistrationDbContext GetOrCreateContext()
        {
            if (this.dbContext == null)
            {
                this.dbContext = string.IsNullOrEmpty(this.nameOrConnectionString) 
                    ? new RegistrationDbContext() 
                    : new RegistrationDbContext(this.nameOrConnectionString);
            }

            return this.dbContext;
        }

        public SeatsAvailability Find(Guid id)
        {
            var context = GetOrCreateContext();
            return context.Set<SeatsAvailability>().Include("Seats").FirstOrDefault(x => x.Id == id);
        }

        public void Save(SeatsAvailability aggregate)
        {
            var context = GetOrCreateContext();
            var entry = context.Entry(aggregate);

            if (entry.State == System.Data.EntityState.Detached)
                context.Set<SeatsAvailability>().Add(aggregate);

            // Can't have transactions across storage and message bus.
            context.SaveChanges();

            var publisher = aggregate as IEventPublisher;
            if (publisher != null)
                this.eventBus.Publish(publisher.Events);
        }

        public void Dispose()
        {
            using (this.dbContext) { }
        }
    }
}
