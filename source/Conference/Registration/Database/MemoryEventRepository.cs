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
    using System.Collections.Concurrent;
    using System.Linq;
    using Common;

    // TODO: This is an extremely basic implementation of the event store, that will be replaced in the future.
    // It is not persistent, nor checks for event versions before committing.
    public class MemoryEventRepository<T> : IRepository<T> where T : class, IAggregateRoot, IEventPublisher
    {
        private readonly IEventBus eventBus;
        private readonly ConcurrentDictionary<Guid, ConcurrentStack<IEvent>> eventStore = new ConcurrentDictionary<Guid, ConcurrentStack<IEvent>>();

        public MemoryEventRepository(IEventBus eventBus)
        {
            this.eventBus = eventBus;
        }

        public T Find(Guid id)
        {
            ConcurrentStack<IEvent> list;
            if (this.eventStore.TryGetValue(id, out list) && list.Count > 0)
            {
                return (T)Activator.CreateInstance(typeof(T), list.ToList());
            }

            return null;
        }

        public void Save(T aggregateRoot)
        {
            var events = aggregateRoot.Events.ToArray();

            var list = this.eventStore.GetOrAdd(aggregateRoot.Id, _ => new ConcurrentStack<IEvent>());
            list.PushRange(events);
            
            // TODO: guarantee delivery
            this.eventBus.Publish(events);
        }
    }
}
