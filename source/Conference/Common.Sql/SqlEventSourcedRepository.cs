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

namespace Common.Sql
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    // TODO: This is an extremely basic implementation of the event store (straw man), that will be replaced in the future.
    // It does not check for event versions before committing, nor is transactional with the event bus.
    // It does not do any snapshots either, which the SeatsAvailability will definitely need.
    public class SqlEventSourcedRepository<T> : IEventSourcedRepository<T> where T : class, IEventSourced
    {
        private readonly IEventBus eventBus;
        private readonly ISerializer serializer;
        private readonly Func<EventStoreDbContext> contextFactory;
        private readonly ConstructorInfo constructor;

        public SqlEventSourcedRepository(IEventBus eventBus, ISerializer serializer, Func<EventStoreDbContext> contextFactory)
        {
            this.eventBus = eventBus;
            this.serializer = serializer;
            this.contextFactory = contextFactory;
            this.constructor = typeof(T).GetConstructor(new[] { typeof(Guid), typeof(IEnumerable<IVersionedEvent>) });
            if (this.constructor == null)
            {
                throw new InvalidCastException("Type T must have a constructor with the following signature: .ctor(Guid, IEnumerable<IVersionedEvent>)");
            }
        }

        public T Find(Guid id)
        {
            using (var context = this.contextFactory.Invoke())
            {
                var deserialized = context.Set<Event>().Where(x => x.AggregateId == id).OrderBy(x => x.Version).AsEnumerable()
                    .Select(x => this.serializer.Deserialize(new MemoryStream(x.Payload))).Cast<IVersionedEvent>();

                if (deserialized.Any())
                {
                    return (T) constructor.Invoke(new object[] { id, deserialized });
                }

                return null;
            }
        }

        public void Save(T aggregateRoot)
        {
            // TODO: guarantee that only incremental versions of the event are stored
            var events = aggregateRoot.Events.ToArray();
            using (var context = this.contextFactory.Invoke())
            {
                foreach (var e in events)
                {
                    using (var stream = new MemoryStream())
                    {
                        this.serializer.Serialize(stream, e);
                        var serialized = new Event { AggregateId = e.SourceId, Version = e.Version, Payload = stream.ToArray() };
                        context.Set<Event>().Add(serialized);
                    }
                }

                context.SaveChanges();
            }

            // TODO: guarantee delivery or roll back, or have a way to resume after a system crash
            this.eventBus.Publish(events);
        }
    }
}