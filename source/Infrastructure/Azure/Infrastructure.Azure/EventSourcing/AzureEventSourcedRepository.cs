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
        private readonly Func<Guid, Tuple<IMemento, DateTime?>> getMementoFromCache;
        private readonly Action<Guid> markCacheAsStale;

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
                            new Tuple<IMemento, DateTime?>(memento, DateTime.UtcNow),
                            new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(30) });
                    };
                this.getMementoFromCache = id => (Tuple<IMemento, DateTime?>)this.cache.Get(GetPartitionKey(id));
                this.markCacheAsStale = id =>
                {
                    var key = GetPartitionKey(id);
                    var item = (Tuple<IMemento, DateTime?>)this.cache.Get(key);
                    if (item != null && item.Item2.HasValue)
                    {
                        item = new Tuple<IMemento, DateTime?>(item.Item1, null);
                        this.cache.Set(
                            key,
                            item,
                            new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(30) });
                    }
                };
            }
            else
            {
                // if no cache object or is not a cache originator, then no-op
                this.cacheMementoIfApplicable = o => { };
                this.getMementoFromCache = id => { return null; };
                this.markCacheAsStale = id => { };
            }
        }

        public T Find(Guid id)
        {
            var cachedMemento = this.getMementoFromCache(id);
            if (cachedMemento != null && cachedMemento.Item1 != null)
            {
                // NOTE: if we had a guarantee that this is running in a single process, there is
                // no need to check if there are new events after the cached version.
                IEnumerable<IVersionedEvent> deserialized;
                if (!cachedMemento.Item2.HasValue || cachedMemento.Item2.Value < DateTime.UtcNow.AddSeconds(-1))
                {
                    deserialized = this.eventStore.Load(GetPartitionKey(id), cachedMemento.Item1.Version + 1).Select(this.Deserialize);
                }
                else
                {
                    // if the cache entry was updated in the last seconds, then there is a high possibility that it is not stale
                    // (because we typically have a single writer for high contention aggregates). This is why why optimistcally avoid
                    // getting the new events from the EventStore since the last memento was created. In the low probable case
                    // where we get an exception on save, then we mark the cache item as stale so when the command gets
                    // reprocessed, this time we get the new events from the EventStore.
                    deserialized = Enumerable.Empty<IVersionedEvent>();
                }

                return this.originatorEntityFactory.Invoke(id, cachedMemento.Item1, deserialized);
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
            try
            {
                this.eventStore.Save(partitionKey, serialized);
            }
            catch
            {
                this.markCacheAsStale(eventSourced.Id);
                throw;
            }

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