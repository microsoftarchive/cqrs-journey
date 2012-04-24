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

namespace Infrastructure.Sql.Processes
{
    using System;
    using System.Data.Entity;
    using System.Linq;
    using System.Linq.Expressions;
    using Infrastructure.Messaging;
    using Infrastructure.Processes;

    // TODO: This is an extremely basic implementation of the event store (straw man), that will be replaced in the future.
    // It is not transactional with the event bus.
    // Does this even belong to a reusable infrastructure?
    public class SqlProcessDataContext<T> : IProcessDataContext<T> where T : class, IProcess
    {
        private readonly ICommandBus commandBus;
        private readonly DbContext context;

        public SqlProcessDataContext(Func<DbContext> contextFactory, ICommandBus commandBus)
        {
            this.commandBus = commandBus;
            this.context = contextFactory.Invoke();
        }

        public T Find(Guid id)
        {
            return this.context.Set<T>().Find(id);
        }

        public T Find(Expression<Func<T, bool>> predicate)
        {
            return this.context.Set<T>().Where(predicate).FirstOrDefault();
        }

        public void Save(T process)
        {
            var entry = this.context.Entry(process);

            if (entry.State == System.Data.EntityState.Detached)
                this.context.Set<T>().Add(process);

            // Can't have transactions across storage and message bus.
            this.context.SaveChanges();

            this.commandBus.Send(process.Commands);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SqlProcessDataContext()
        {
            this.Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.context.Dispose();
            }
        }
    }
}
