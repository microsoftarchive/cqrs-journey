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
    using System.Runtime.Caching;
    using Infrastructure.EventSourcing;
    using Infrastructure.Serialization;
    using Infrastructure.Util;

    // NOTE: This is a basic implementation of the event store that could be optimized in the future.
    public class AzureEventSourcedRepository<T> : IEventSourcedRepository<T> where T : class, IEventSourced
    {
        // Could potentially use DataAnnotations to get a friendly/unique name in case of collisions between BCs.
        private static readonly string sourceType = typeof(T).Name;
        private readonly IEventStore eventStore;
        private readonly IEventStoreBusPublisher publisher;
        private readonly ITextSerializer serializer;
        private readonly Func<Guid, IEnumerable<IVersionedEvent>, T> entityFactory;
        private readonly Func<Guid, IMemento, IEnumerable<IVersionedEvent>, T> originatorEntityFactory;
        private readonly IMetadataProvider metadataProvider;
        private readonly ObjectCache cache;
        private readonly Action<T> cacheMementoIfApplicable;
        private readonly Func<Guid, IMemento> getMementoFromCache;

        public AzureEventSourcedRepository(IEventStore eventStore, IEventStoreBusPublisher publisher, ITextSerializer serializer, IMetadataProvider metadataProvider, ObjectCache cache)
        {
            this.eventStore = eventStore;
            this.publisher = publisher;
            this.serializer = serializer;
            this.metadataProvider = metadataProvider;
            this.cache = cache;

            // TODO: could be replaced with a compiled lambda to make it more performant
            var constructor = typeof(T).GetConstructor(new[] { typeof(Guid), typeof(IEnumerable<IVersionedEvent>) });
            if (constructor == null)
            {
                throw new InvalidCastException(
                    "Type T must have a constructor with the following signature: .ctor(Guid, IEnumerable<IVersionedEvent>)");
            }
            this.entityFactory = (id, events) => (T)constructor.Invoke(new object[] { id, events });

            if (typeof(IMementoOriginator).IsAssignableFrom(typeof(T)) && this.cache != null)
            {
                // TODO: could be replaced with a compiled lambda to make it more performant
                var mementoConstructor = typeof(T).GetConstructor(new[] { typeof(Guid), typeof(IMemento), typeof(IEnumerable<IVersionedEvent>) });
                if (mementoConstructor == null)
                {
                    throw new InvalidCastException(
                        "Type T must have a constructor with the following signature: .ctor(Guid, IMemento, IEnumerable<IVersionedEvent>)");
                }
                this.originatorEntityFactory = (id, memento, events) => (T)mementoConstructor.Invoke(new object[] { id, memento, events });
                this.cacheMementoIfApplicable = (T originator) =>
                    {
                        string key = GetPartitionKey(originator.Id);
                        var memento = ((IMementoOriginator)originator).SaveToMemento();
                        this.cache.Set(
                            key,
                            memento,
                            new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(30) });
                    };
                this.getMementoFromCache = id => (IMemento)this.cache.Get(GetPartitionKey(id));
            }
            else
            {
                // if no cache object or is not a memento originator, then no-op
                this.cacheMementoIfApplicable = o => { };
                this.getMementoFromCache = id => { return null; };
            }
        }

        public T Find(Guid id)
        {
            var memento = this.getMementoFromCache(id);
            if (memento != null)
            {
                // NOTE: if we had a guarantee that this is running in a single process, there is
                // no need to check if there are new events after the cached version.
                var deserialized = this.eventStore.Load(GetPartitionKey(id), memento.Version + 1)
                    .Select(this.Deserialize);

                return this.originatorEntityFactory.Invoke(id, memento, deserialized);
            }
            else
            {
                var deserialized = this.eventStore.Load(GetPartitionKey(id), 0)
                    .Select(this.Deserialize)
                    .AsCachedAnyEnumerable();

                if (deserialized.Any())
                {
                    return this.entityFactory.Invoke(id, deserialized);
                }
            }

            return null;
        }


        public T Get(Guid id)
        {
            var entity = this.Find(id);
            if (entity == null)
            {
                throw new EntityNotFoundException(id, sourceType);
            }

            return entity;
        }

        public void Save(T eventSourced, string correlationId)
        {
            // TODO: guarantee that only incremental versions of the event are stored
            var events = eventSourced.Events.ToArray();
            var serialized = events.Select(e => this.Serialize(e, correlationId));

            var partitionKey = this.GetPartitionKey(eventSourced.Id);
            this.eventStore.Save(partitionKey, serialized);

            this.publisher.SendAsync(partitionKey, events.Length);

            this.cacheMementoIfApplicable.Invoke(eventSourced);
        }

        private string GetPartitionKey(Guid id)
        {
            return sourceType + "_" + id.ToString();
        }

        private EventData Serialize(IVersionedEvent e, string correlationId)
        {
            using (var writer = new StringWriter())
            {
                this.serializer.Serialize(writer, e);
                var metadata = this.metadataProvider.GetMetadata(e);
                return new EventData
                           {
                               Version = e.Version,
                               SourceId = e.SourceId.ToString(),
                               Payload = writer.ToString(),
                               SourceType = sourceType,
                               CorrelationId = correlationId,
                               // Standard metadata
                               AssemblyName = metadata.TryGetValue(StandardMetadata.AssemblyName),
                               Namespace = metadata.TryGetValue(StandardMetadata.Namespace),
                               TypeName = metadata.TryGetValue(StandardMetadata.TypeName),
                               FullName = metadata.TryGetValue(StandardMetadata.FullName),
                           };
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