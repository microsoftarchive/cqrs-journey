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

namespace Infrastructure.Azure.EventSourcing
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data.Services.Client;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using AutoMapper;
    using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.AzureStorage;
    using Microsoft.Practices.TransientFaultHandling;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;

    /// <summary>
    /// Implements an event store using Windows Azure Table Storage.
    /// </summary>
    /// <remarks>
    /// <para> This class works closely related to <see cref="EventStoreBusPublisher"/> and <see cref="AzureEventSourcedRepository{T}"/>, and provides a resilient mechanism to 
    /// store events, and also manage which events are pending for publishing to an event bus.</para>
    /// <para>Ideally, it would be very valuable to provide asynchronous APIs to avoid blocking I/O calls.</para>
    /// <para>See <see cref="http://go.microsoft.com/fwlink/p/?LinkID=258557"> Journey chapter 7</see> for more potential performance and scalability optimizations.</para>
    /// </remarks>
    public class EventStore : IEventStore, IPendingEventsQueue
    {
        private const string UnpublishedRowKeyPrefix = "Unpublished_";
        private const string UnpublishedRowKeyPrefixUpperLimit = "Unpublished`";
        private const string RowKeyVersionUpperLimit = "9999999999";
        private readonly CloudStorageAccount account;
        private readonly string tableName;
        private readonly CloudTableClient tableClient;
        private readonly Microsoft.Practices.TransientFaultHandling.RetryPolicy pendingEventsQueueRetryPolicy;
        private readonly Microsoft.Practices.TransientFaultHandling.RetryPolicy eventStoreRetryPolicy;

        static EventStore()
        {
            Mapper.CreateMap<EventTableServiceEntity, EventData>();
            Mapper.CreateMap<EventData, EventTableServiceEntity>();
        }

        public EventStore(CloudStorageAccount account, string tableName)
        {
            if (account == null) throw new ArgumentNullException("account");
            if (tableName == null) throw new ArgumentNullException("tableName");
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("tableName");

            this.account = account;
            this.tableName = tableName;
            this.tableClient = account.CreateCloudTableClient();
            this.tableClient.RetryPolicy = RetryPolicies.NoRetry();

            // TODO: This could be injected.
            var backgroundRetryStrategy = new ExponentialBackoff(10, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(1));
            var blockingRetryStrategy = new Incremental(3, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
            this.pendingEventsQueueRetryPolicy = new RetryPolicy<StorageTransientErrorDetectionStrategy>(backgroundRetryStrategy);
            this.pendingEventsQueueRetryPolicy.Retrying += (s, e) =>
            {
                var handler = this.Retrying;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }

                Trace.TraceWarning("An error occurred in attempt number {1} to access table storage (PendingEventsQueue): {0}", e.LastException.Message, e.CurrentRetryCount);
            };
            this.eventStoreRetryPolicy = new RetryPolicy<StorageTransientErrorDetectionStrategy>(blockingRetryStrategy);
            this.eventStoreRetryPolicy.Retrying += (s, e) => Trace.TraceWarning(
                "An error occurred in attempt number {1} to access table storage (EventStore): {0}",
                e.LastException.Message,
                e.CurrentRetryCount);

            this.eventStoreRetryPolicy.ExecuteAction(() => tableClient.CreateTableIfNotExist(tableName));
        }

        /// <summary>
        /// Notifies that the sender is retrying due to a transient fault.
        /// </summary>
        public event EventHandler Retrying;

        public IEnumerable<EventData> Load(string partitionKey, int version)
        {
            var minRowKey = version.ToString("D10");
            var query = this.GetEntitiesQuery(partitionKey, minRowKey, RowKeyVersionUpperLimit);
            // TODO: use async APIs, continuation tokens
            var all = this.eventStoreRetryPolicy.ExecuteAction(() => query.Execute());
            return all.Select(x => Mapper.Map(x, new EventData { Version = int.Parse(x.RowKey) }));
        }

        public void Save(string partitionKey, IEnumerable<EventData> events)
        {
            var context = this.tableClient.GetDataServiceContext();
            foreach (var eventData in events)
            {
                string creationDate = DateTime.UtcNow.ToString("o");
                var formattedVersion = eventData.Version.ToString("D10");
                context.AddObject(
                    this.tableName,
                    Mapper.Map(eventData, new EventTableServiceEntity
                        {
                            PartitionKey = partitionKey,
                            RowKey = formattedVersion,
                            CreationDate = creationDate,
                        }));

                // Add a duplicate of this event to the Unpublished "queue"
                context.AddObject(
                    this.tableName,
                    Mapper.Map(eventData, new EventTableServiceEntity
                        {
                            PartitionKey = partitionKey,
                            RowKey = UnpublishedRowKeyPrefix + formattedVersion,
                            CreationDate = creationDate,
                        }));
            }

            try
            {
                this.eventStoreRetryPolicy.ExecuteAction(() => context.SaveChanges(SaveChangesOptions.Batch));
            }
            catch (DataServiceRequestException ex)
            {
                var inner = ex.InnerException as DataServiceClientException;
                if (inner != null && inner.StatusCode == (int)HttpStatusCode.Conflict)
                {
                    throw new ConcurrencyException();
                }

                throw;
            }
        }

        /// <summary>
        /// Gets the pending events for publishing asynchronously using delegate continuations.
        /// </summary>
        /// <param name="partitionKey">The partition key to get events from.</param>
        /// <param name="successCallback">The callback that will be called if the data is successfully retrieved. 
        /// The first argument of the callback is the list of pending events.
        /// The second argument is true if there are more records that were not retrieved.</param>
        /// <param name="exceptionCallback">The callback used if there is an exception that does not allow to continue.</param>
        public void GetPendingAsync(string partitionKey, Action<IEnumerable<IEventRecord>, bool> successCallback, Action<Exception> exceptionCallback)
        {
            var query = this.GetEntitiesQuery(partitionKey, UnpublishedRowKeyPrefix, UnpublishedRowKeyPrefixUpperLimit);
            this.pendingEventsQueueRetryPolicy
                .ExecuteAction(
                    ac => query.BeginExecuteSegmented(ac, null),
                    ar => query.EndExecuteSegmented(ar),
                    rs =>
                    {
                        var all = rs.Results.ToList();
                        successCallback(rs.Results, rs.HasMoreResults);
                    },
                    exceptionCallback);
        }

        /// <summary>
        /// Deletes the specified pending event from the queue.
        /// </summary>
        /// <param name="partitionKey">The partition key of the event.</param>
        /// <param name="rowKey">The partition key of the event.</param>
        /// <param name="successCallback">The callback that will be called if the data is successfully retrieved.
        /// The argument specifies if the row was deleted. If false, it means that the row did not exist.
        /// </param>
        /// <param name="exceptionCallback">The callback used if there is an exception that does not allow to continue.</param>
        public void DeletePendingAsync(string partitionKey, string rowKey, Action<bool> successCallback, Action<Exception> exceptionCallback)
        {
            var context = this.tableClient.GetDataServiceContext();
            var item = new EventTableServiceEntity { PartitionKey = partitionKey, RowKey = rowKey };
            context.AttachTo(this.tableName, item, "*");
            context.DeleteObject(item);

            this.pendingEventsQueueRetryPolicy.ExecuteAction(
                ac => context.BeginSaveChanges(ac, null),
                ar => 
                    {
                        try
                        {
                            context.EndSaveChanges(ar);
                            return true;
                        }
                        catch (DataServiceRequestException ex)
                        {
                            // ignore if entity was already deleted.
                            var inner = ex.InnerException as DataServiceClientException;
                            if (inner == null || inner.StatusCode != (int)HttpStatusCode.NotFound)
                            {
                                throw;
                            }

                            return false;
                        }
                    },
                successCallback,
                exceptionCallback);
        }

        /// <summary>
        /// Gets the list of all partitions that have pending unpublished events.
        /// </summary>
        /// <returns>The list of all partitions.</returns>
        public IEnumerable<string> GetPartitionsWithPendingEvents()
        {
            var context = this.tableClient.GetDataServiceContext();
            var query = context
                .CreateQuery<EventTableServiceEntity>(this.tableName)
                .Where(
                    x =>
                    x.RowKey.CompareTo(UnpublishedRowKeyPrefix) >= 0 &&
                    x.RowKey.CompareTo(UnpublishedRowKeyPrefixUpperLimit) <= 0)
                .Select(x => new { x.PartitionKey })
                .AsTableServiceQuery();

            var result = new BlockingCollection<string>();
            var tokenSource = new CancellationTokenSource();

            this.pendingEventsQueueRetryPolicy.ExecuteAction(
                ac => query.BeginExecuteSegmented(ac, null),
                ar => query.EndExecuteSegmented(ar),
                rs =>
                {
                    foreach (var key in rs.Results.Select(x => x.PartitionKey).Distinct())
                    {
                        result.Add(key);
                    }

                    while (rs.HasMoreResults)
                    {
                        try
                        {
                            rs = this.pendingEventsQueueRetryPolicy.ExecuteAction(() => rs.GetNext());
                            foreach (var key in rs.Results.Select(x => x.PartitionKey).Distinct())
                            {
                                result.Add(key);
                            }
                        }
                        catch
                        {
                            // Cancel is to force an exception being thrown in the consuming enumeration thread
                            // TODO: is there a better way to get the correct exception message instead of an OperationCancelledException in the consuming thread?
                            tokenSource.Cancel();
                            throw;
                        }
                    }
                    result.CompleteAdding();
                },
                ex =>
                {
                    tokenSource.Cancel();
                    throw ex;
                });

            return result.GetConsumingEnumerable(tokenSource.Token);
        }

        private CloudTableQuery<EventTableServiceEntity> GetEntitiesQuery(string partitionKey, string minRowKey, string maxRowKey)
        {
            var context = this.tableClient.GetDataServiceContext();
            var query = context
                .CreateQuery<EventTableServiceEntity>(this.tableName)
                .Where(
                    x =>
                    x.PartitionKey == partitionKey && x.RowKey.CompareTo(minRowKey) >= 0 && x.RowKey.CompareTo(maxRowKey) <= 0);

            return query.AsTableServiceQuery();
        }
    }
}
