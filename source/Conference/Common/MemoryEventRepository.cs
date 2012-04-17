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

namespace Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;

    // TODO: This is an extremely basic implementation of the event store, that will be replaced in the future.
    // It is not persistent, nor checks for event versions before committing, nor is transactional with the event bus.
    public class MemoryEventRepository<T> : IRepository<T> where T : class, IEventSourcedAggregateRoot
    {
        private readonly IEventBus eventBus;
        private readonly ConcurrentDictionary<Guid, ConcurrentQueue<IDomainEvent>> eventStore = new ConcurrentDictionary<Guid, ConcurrentQueue<IDomainEvent>>();

        public MemoryEventRepository(IEventBus eventBus)
        {
            this.eventBus = eventBus;
        }

        public T Find(Guid id)
        {
            ConcurrentQueue<IDomainEvent> list;
            if (this.eventStore.TryGetValue(id, out list) && list.Count > 0)
            {
                return (T)Activator.CreateInstance(typeof(T), list.ToList());
            }

            return null;
        }

        public void Save(T aggregateRoot)
        {
            var events = aggregateRoot.Events.ToArray();

            // TODO: guarantee that only incremental versions of the event are stored
            var list = this.eventStore.GetOrAdd(aggregateRoot.Id, _ => new ConcurrentQueue<IDomainEvent>());
            foreach (var e in events)
            {
                list.Enqueue(e);
            }
            
            // TODO: guarantee delivery or roll back, or have a way to resume after a system crash
            this.eventBus.Publish(events);
        }
    }
}
