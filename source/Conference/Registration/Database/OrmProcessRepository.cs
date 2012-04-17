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
    using System.Data.Entity;
    using System.Linq;
    using Common;

    public class OrmProcessRepository : DbContext, IProcessRepository
    {
        private readonly ICommandBus commandBus;

        public OrmProcessRepository()
            : this("ConferenceRegistrationProcesses")
        {
        }

        public OrmProcessRepository(string nameOrConnectionString)
            : this(nameOrConnectionString, new MemoryCommandBus())
        {
        }

        public OrmProcessRepository(ICommandBus commandBus)
            : this("ConferenceRegistrationProcesses", commandBus)
        {
        }

        public OrmProcessRepository(string nameOrConnectionString, ICommandBus commandBus)
            : base(nameOrConnectionString)
        {
            this.commandBus = commandBus;
        }

        public T Find<T>(Guid id) where T : class, IAggregateRoot
        {
            return this.Set<T>().Find(id);
        }

        public IQueryable<T> Query<T>() where T : class, IAggregateRoot
        {
            return this.Set<T>();
        }

        public void Save<T>(T aggregate) where T : class, IAggregateRoot
        {
            var entry = this.Entry(aggregate);

            if (entry.State == System.Data.EntityState.Detached)
                this.Set<T>().Add(aggregate);

            // Can't have transactions across storage and message bus.
            this.SaveChanges();

            var commandPublisher = aggregate as ICommandPublisher;
            if (commandPublisher != null)
                this.commandBus.Send(commandPublisher.Commands);
        }

        // Define the available entity sets for the database.
        public virtual DbSet<RegistrationProcess> RegistrationProcesses { get; private set; }
    }
}
