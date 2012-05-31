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
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Linq;
    using System.Linq.Expressions;
    using Infrastructure.Messaging;
    using Infrastructure.Processes;
    using Infrastructure.Serialization;

    public class SqlProcessDataContext<T> : IProcessDataContext<T> where T : class, IProcess
    {
        private readonly ICommandBus commandBus;
        private readonly DbContext context;
        private readonly ITextSerializer serializer;

        public SqlProcessDataContext(Func<DbContext> contextFactory, ICommandBus commandBus, ITextSerializer serializer)
        {
            this.commandBus = commandBus;
            this.context = contextFactory.Invoke();
            this.serializer = serializer;
        }

        public T Find(Guid id)
        {
            return Find(process => process.Id == id);
        }

        public T Find(Expression<Func<T, bool>> predicate)
        {
            var process = this.context.Set<T>().Where(predicate).FirstOrDefault();
            if (process == null)
                return default(T);

            var pendingCommands = this.context.Set<PendingCommandsEntity>().Find(process.Id);
            if (pendingCommands != null)
            {
                // Must dispatch pending commands before the process 
                // can be further used.
                var commands = this.serializer.Deserialize<IEnumerable<Envelope<ICommand>>>(pendingCommands.Commands).OfType<Envelope<ICommand>>().ToList();
                var commandIndex = 0;

                // Here we try again, one by one. Anyone might fail, so we have to keep 
                // decreasing the pending commands count until no more are left.
                try
                {
                    for (int i = 0; i < commands.Count; i++)
                    {
                        this.commandBus.Send(commands[i]);
                        commandIndex++;
                    }
                }
                catch (Exception) // We catch a generic exception as we don't know what implementation of ICommandBus we might be using.
                {
                    pendingCommands.Commands = this.serializer.Serialize(commands.Skip(commandIndex));
                    this.context.SaveChanges();
                    // If this fails, we propagate the exception.
                    throw;
                }

                // If succeed, we delete the pending commands.
                this.context.Set<PendingCommandsEntity>().Remove(pendingCommands);
                this.context.SaveChanges();
            }

            return process;
        }

        public void Save(T process)
        {
            var entry = this.context.Entry(process);

            if (entry.State == System.Data.EntityState.Detached)
                this.context.Set<T>().Add(process);

            var commandIndex = 0;
            var commands = process.Commands.ToList();

            try
            {
                for (int i = 0; i < commands.Count; i++)
                {
                    this.commandBus.Send(commands[i]);
                    commandIndex++;
                }
            }
            catch (Exception) // We catch a generic exception as we don't know what implementation of ICommandBus we might be using.
            {
                var pending = this.context.Set<PendingCommandsEntity>().Find(process.Id);
                if (pending == null)
                {
                    pending = new PendingCommandsEntity(process.Id);
                    this.context.Set<PendingCommandsEntity>().Add(pending);
                }

                pending.Commands = this.serializer.Serialize(commands.Skip(commandIndex));
            }

            // Saves both the state of the process as well as the pending commands if any.
            this.context.SaveChanges();
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
