// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
// http://go.microsoft.com/fwlink/p/?LinkID=258575
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
    using System.Data.Entity.Infrastructure;
    using System.Diagnostics;
    using System.Linq;
    using System.Linq.Expressions;
    using Infrastructure.Messaging;
    using Infrastructure.Processes;
    using Infrastructure.Serialization;
    using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.SqlAzure;
    using Microsoft.Practices.TransientFaultHandling;

    /// <summary>
    /// Data context used to persist instances of <see cref="IProcessManager"/> (also known as Sagas in the CQRS community) using Entity Framework.
    /// </summary>
    /// <typeparam name="T">The entity type to persist.</typeparam>
    /// <remarks>
    /// <para>See <see cref="http://go.microsoft.com/fwlink/p/?LinkID=258564">Reference 6</see> for a description of what is a Process Manager.</para>
    /// <para>This is a very basic implementation, and would benefit from several optimizations. 
    /// For example, it would be very valuable to provide asynchronous APIs to avoid blocking I/O calls.
    /// It would also benefit from dispatching commands asynchronously (but in a resilient way), similar to what the EventStoreBusPublisher does.
    /// See <see cref="http://go.microsoft.com/fwlink/p/?LinkID=258557"> Journey chapter 7</see> for more potential performance and scalability optimizations.</para>
    /// <para>There are a few things that we learnt along the way regarding Process Managers, which we might do differently with the new insights that we
    /// now have. See <see cref="http://go.microsoft.com/fwlink/p/?LinkID=258558"> Journey lessons learnt</see> for more information.</para>
    /// </remarks>
    public class SqlProcessManagerDataContext<T> : IProcessManagerDataContext<T> where T : class, IProcessManager
    {
        private readonly ICommandBus commandBus;
        private readonly DbContext context;
        private readonly ITextSerializer serializer;
        private readonly RetryPolicy<SqlAzureTransientErrorDetectionStrategy> retryPolicy;

        public SqlProcessManagerDataContext(Func<DbContext> contextFactory, ICommandBus commandBus, ITextSerializer serializer)
        {
            this.commandBus = commandBus;
            this.context = contextFactory.Invoke();
            this.serializer = serializer;

            this.retryPolicy = new RetryPolicy<SqlAzureTransientErrorDetectionStrategy>(new Incremental(3, TimeSpan.Zero, TimeSpan.FromSeconds(1)) { FastFirstRetry = true });
            this.retryPolicy.Retrying += (s, e) => 
                Trace.TraceWarning("An error occurred in attempt number {1} to save the process manager state: {0}", e.LastException.Message, e.CurrentRetryCount);
        }

        public T Find(Guid id)
        {
            return Find(pm => pm.Id == id, true);
        }

        public T Find(Expression<Func<T, bool>> predicate, bool includeCompleted = false)
        {
            T pm = null;
            if (!includeCompleted)
            {
                // first try to get the non-completed, in case the table is indexed by Completed, or there is more
                // than one process manager that fulfills the predicate but only 1 is not completed.
                pm = this.retryPolicy.ExecuteAction(() => 
                    this.context.Set<T>().Where(predicate.And(x => x.Completed == false)).FirstOrDefault());
            }

            if (pm == null)
            {
                pm = this.retryPolicy.ExecuteAction(() => 
                    this.context.Set<T>().Where(predicate).FirstOrDefault());
            }

            if (pm != null)
            {
                // TODO: ideally this could be improved to avoid 2 roundtrips to the server.
                var undispatchedMessages = this.context.Set<UndispatchedMessages>().Find(pm.Id);
                try
                {
                    this.DispatchMessages(undispatchedMessages);
                }
                catch (DbUpdateConcurrencyException)
                {
                    // if another thread already dispatched the messages, ignore
                    Trace.TraceWarning("Concurrency exception while marking commands as dispatched for process manager with ID {0} in Find method.", pm.Id);
                    
                    this.context.Entry(undispatchedMessages).Reload();

                    undispatchedMessages = this.context.Set<UndispatchedMessages>().Find(pm.Id);

                    // undispatchedMessages should be null, as we do not have a rowguid to do optimistic locking, other than when the row is deleted.
                    // Nevertheless, we try dispatching just in case the DB schema is changed to provide optimistic locking.
                    this.DispatchMessages(undispatchedMessages);
                }

                if (!pm.Completed || includeCompleted)
                {
                    return pm;
                }
            }

            return null;
        }

        /// <summary>
        /// Saves the state of the process manager and publishes the commands in a resilient way.
        /// </summary>
        /// <param name="processManager">The instance to save.</param>
        /// <remarks>For explanation of the implementation details, see <see cref="http://go.microsoft.com/fwlink/p/?LinkID=258557"> Journey chapter 7</see>.</remarks>
        public void Save(T processManager)
        {
            var entry = this.context.Entry(processManager);

            if (entry.State == System.Data.EntityState.Detached)
                this.context.Set<T>().Add(processManager);

            var commands = processManager.Commands.ToList();
            UndispatchedMessages undispatched = null;
            if (commands.Count > 0)
            {
                // if there are pending commands to send, we store them as undispatched.
                undispatched = new UndispatchedMessages(processManager.Id)
                                   {
                                       Commands = this.serializer.Serialize(commands)
                                   };
                this.context.Set<UndispatchedMessages>().Add(undispatched);
            }

            try
            {
                this.retryPolicy.ExecuteAction(() => this.context.SaveChanges());
            }
            catch (DbUpdateConcurrencyException e)
            {
                throw new ConcurrencyException(e.Message, e);
            }

            try
            {
                this.DispatchMessages(undispatched, commands);
            }
            catch (DbUpdateConcurrencyException)
            {
                // if another thread already dispatched the messages, ignore
                Trace.TraceWarning("Ignoring concurrency exception while marking commands as dispatched for process manager with ID {0} in Save method.", processManager.Id);
            }
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
                }
                catch (Exception)
                {
                    // We catch a generic exception as we don't know what implementation of ICommandBus we might be using.
                    if (originalCommandsCount != deserializedCommands.Count)
                    {
                        // if we were able to send some commands, then updates the undispatched messages.
                        undispatched.Commands = this.serializer.Serialize(deserializedCommands);
                        try
                        {
                            this.context.SaveChanges();
                        }
                        catch (DbUpdateConcurrencyException)
                        {
                            // if another thread already dispatched the messages, ignore and surface original exception instead
                        }
                    }

                    throw;
                }

                // we remove all the undispatched messages for this process manager.
                this.context.Set<UndispatchedMessages>().Remove(undispatched);
                this.retryPolicy.ExecuteAction(() => this.context.SaveChanges());
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SqlProcessManagerDataContext()
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
