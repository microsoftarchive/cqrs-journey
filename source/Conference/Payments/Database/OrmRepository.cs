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

namespace Payments.Database
{
    using System;
    using System.Data.Entity;
    using System.Transactions;
    using Common;

    public class OrmRepository : DbContext, IRepository<ThirdPartyProcessorPayment>
    {
        private IEventBus eventBus;

        public OrmRepository()
            : this("Payments")
        {
        }

        public OrmRepository(string nameOrConnectionString)
            : this(nameOrConnectionString, new MemoryEventBus())
        {
        }

        public OrmRepository(IEventBus eventBus)
            : this("Payments", eventBus)
        {
        }

        public OrmRepository(string nameOrConnectionString, IEventBus eventBus)
            : base(nameOrConnectionString)
        {
            this.eventBus = eventBus;
        }

        public ThirdPartyProcessorPayment Find(Guid id)
        {
            return this.Set<ThirdPartyProcessorPayment>().Find(id);
        }

        public void Save(ThirdPartyProcessorPayment aggregate)
        {
            var entry = this.Entry(aggregate);

            if (entry.State == System.Data.EntityState.Detached)
                this.Set<ThirdPartyProcessorPayment>().Add(aggregate);

            using (var scope = new TransactionScope())
            {
                this.SaveChanges();

                var publisher = aggregate as IEventPublisher;
                if (publisher != null)
                    this.eventBus.Publish(publisher.Events);

                scope.Complete();
            }
        }

        public virtual DbSet<ThirdPartyProcessorPayment> ThirdPartyProcessorPayments { get; private set; }
    }
}
