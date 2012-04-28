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
    using Infrastructure.EventSourcing;
    using Infrastructure.Serialization;
    using Infrastructure.Util;

    // TODO: This is a basic implementation of the event store that could be optimized in the future.
    // It does not do any snapshots, which the SeatsAvailability will probably need (even if those snapshots could just be in memory)
    public class AzureEventSourcedRepository<T> : IEventSourcedRepository<T> where T : class, IEventSourced
    {
        private readonly IEventStore eventStore;
        private readonly IEventStoreBusPublisher publisher;
        private readonly ITextSerializer serializer;
        private readonly Func<Guid, IEnumerable<IVersionedEvent>, T> entityFactory;
        private readonly string sourceType;

        public AzureEventSourcedRepository(IEventStore eventStore, IEventStoreBusPublisher publisher, ITextSerializer serializer)
        {
            this.eventStore = eventStore;
            this.publisher = publisher;
            this.serializer = serializer;

            // TODO: could be replaced with a compiled lambda to make it more performant
            var constructor = typeof (T).GetConstructor(new[] {typeof (Guid), typeof (IEnumerable<IVersionedEvent>)});
            if (constructor == null)
            {
                throw new InvalidCastException(
                    "Type T must have a constructor with the following signature: .ctor(Guid, IEnumerable<IVersionedEvent>)");
            }
            this.entityFactory = (id, events) => (T) constructor.Invoke(new object[] {id, events});

            // Could potentially use DataAnnotations to get a friendly/unique name in case of collisions between BCs.
            this.sourceType = typeof(T).Name;
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

            var partitionKey = this.GetPartitionKey(eventSourced.Id);
            this.eventStore.Save(partitionKey, serialized);

            this.publisher.SendAsync(partitionKey);
        }

        private string GetPartitionKey(Guid id)
        {
            return this.sourceType + "_" + id.ToString();
        }

        private EventData Serialize(IVersionedEvent e)
        {
            using (var writer = new StringWriter())
            {
                this.serializer.Serialize(writer, e);
                return new EventData { Version = e.Version, Payload = writer.ToString(), SourceType = this.sourceType, EventType = e.GetType().Name };
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