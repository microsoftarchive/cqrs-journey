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

namespace Infrastructure.Azure.EventSourcing
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Infrastructure.Azure;
    using Infrastructure.EventSourcing;
    using Infrastructure.Messaging;
    using Infrastructure.Serialization;
    using Infrastructure.Util;

    // TODO: This is an extremely basic implementation of the event store (straw man), that will be replaced in the future.
    // It does not check for event versions before committing, nor is transactional with the event bus.
    // It does not do any snapshots either, which the SeatsAvailability will definitely need.
    public class AzureEventSourcedRepository<T> : IEventSourcedRepository<T> where T : class, IEventSourced
    {
        private readonly IEventStore eventStore;
        private readonly IEventBus eventBus;
        private readonly ITextSerializer serializer;
        private readonly Func<Guid, IEnumerable<IVersionedEvent>, T> entityFactory;

        public AzureEventSourcedRepository(IEventStore eventStore, IEventBus eventBus, ITextSerializer serializer)
        {
            this.eventStore = eventStore;
            this.eventBus = eventBus;
            this.serializer = serializer;

            // TODO: could be replaced with a compiled lambda to make it more performant
            var constructor = typeof (T).GetConstructor(new[] {typeof (Guid), typeof (IEnumerable<IVersionedEvent>)});
            if (constructor == null)
            {
                throw new InvalidCastException(
                    "Type T must have a constructor with the following signature: .ctor(Guid, IEnumerable<IVersionedEvent>)");
            }
            this.entityFactory = (id, events) => (T) constructor.Invoke(new object[] {id, events});
        }

        public T Find(Guid id)
        {
            var deserialized = this.eventStore.Load(GetPartitionKey(id), 0)
                .Select(this.Deserialize)
                .AsCachedAnyEnumerable();

            if (deserialized.Any())
            {
                return entityFactory.Invoke(id, deserialized);
            }

            return null;
        }

        public void Save(T eventSourced)
        {
            // TODO: guarantee that only incremental versions of the event are stored
            var events = eventSourced.Events.ToArray();
            var serialized = events.Select(this.Serialize);

            this.eventStore.Save(this.GetPartitionKey(eventSourced.Id), serialized);

            // TODO: guarantee delivery or roll back, or have a way to resume after a system crash
            // will actually notify a component that will download the pending events for this aggregate and publish
            this.eventBus.Publish(events);
        }

        private string GetPartitionKey(Guid id)
        {
            // could contain a prefix for the type too.
            return id.ToString();
        }

        private EventData Serialize(IVersionedEvent e)
        {
            using (var writer = new StringWriter())
            {
                this.serializer.Serialize(writer, e);
                return new EventData { Version = e.Version, Payload = writer.ToString(), EventType = e.GetType().Name };
            }
        }

        private IVersionedEvent Deserialize(EventData @event)
        {
            using (var reader = new StringReader(@event.Payload))
            {
                return (IVersionedEvent)this.serializer.Deserialize(reader);
            }
        }
    }
}