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

            // TODO: ideally this could be improved to avoid 2 roundtrips to the server.
            var undispatchedMessages = this.context.Set<UndispatchedMessages>().Find(process.Id);
            
            this.DispatchMessages(undispatchedMessages);

            return process;
        }

        public void Save(T process)
        {
            var entry = this.context.Entry(process);

            if (entry.State == System.Data.EntityState.Detached)
                this.context.Set<T>().Add(process);

            var commands = process.Commands.ToList();
            UndispatchedMessages undispatched = null;
            if (commands.Count > 0)
            {
                // if there are pending commands to send, we store them as undispatched.
                undispatched = new UndispatchedMessages(process.Id)
                                   {
                                       Commands = this.serializer.Serialize(commands)
                                   };
                this.context.Set<UndispatchedMessages>().Add(undispatched);
            }

            this.context.SaveChanges();

            this.DispatchMessages(undispatched, commands);
        }

        private void DispatchMessages(UndispatchedMessages undispatched, List<Envelope<ICommand>> deserializedCommands = null)
        {
            if (undispatched != null)
            {
                if (deserializedCommands == null)
                {
                    deserializedCommands = this.serializer.Deserialize<IEnumerable<Envelope<ICommand>>>(undispatched.Commands).ToList();
                }

                var originalCommandsCount = deserializedCommands.Count;
                try
                {
                    while (deserializedCommands.Count > 0)
                    {
                        this.commandBus.Send(deserializedCommands.First());
                        deserializedCommands.RemoveAt(0);
                    }

                    // we remove all the undispatched messages for this process
                    this.context.Set<UndispatchedMessages>().Remove(undispatched);
                    this.context.SaveChanges();
                }
                catch (Exception) 
                {
                    // We catch a generic exception as we don't know what implementation of ICommandBus we might be using.
                    if (originalCommandsCount != deserializedCommands.Count)
                    {
                        // if we were able to send some commands, then updates the undispatched messages.
                        undispatched.Commands = this.serializer.Serialize(deserializedCommands);
                        this.context.SaveChanges();
                    }

                    throw;
                }
            }
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
