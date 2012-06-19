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
            this.pendingEventsQueueRetryPolicy.Retrying += (s, e) => Trace.TraceWarning(
                "An error occurred in attempt number {1} to access table storage (PendingEventsQueue): {0}",
                e.LastException.Message,
                e.CurrentRetryCount);
            this.eventStoreRetryPolicy = new RetryPolicy<StorageTransientErrorDetectionStrategy>(blockingRetryStrategy);
            this.eventStoreRetryPolicy.Retrying += (s, e) => Trace.TraceWarning(
                "An error occurred in attempt number {1} to access table storage (EventStore): {0}",
                e.LastException.Message,
                e.CurrentRetryCount);

            this.eventStoreRetryPolicy.ExecuteAction(() => tableClient.CreateTableIfNotExist(tableName));
        }

        public IEnumerable<EventData> Load(string partitionKey, int version)
        {
            var minRowKey = version.ToString("D10");
            var query = this.GetEntitiesQuery(partitionKey, minRowKey, RowKeyVersionUpperLimit);
            // TODO: continuation tokens, etc
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

        public IEnumerable<IEventRecord> GetPending(string partitionKey)
        {
            var query = this.GetEntitiesQuery(partitionKey, UnpublishedRowKeyPrefix, UnpublishedRowKeyPrefixUpperLimit);
            // TODO: continuation tokens, etc
            return this.pendingEventsQueueRetryPolicy.ExecuteAction(() => query.Execute());
        }

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

        private CloudTableQuery<EventTableServiceEntity> GetEntitiesQuery(string partitionKey, string minRowKey, string maxRowKey)
        {
            var context = this.tableClient.GetDataServiceContext();
            var query = context
                .CreateQuery<EventTableServiceEntity>(this.tableName)
                .Where(
                    x =>
                    x.PartitionKey == partitionKey && x.RowKey.CompareTo(minRowKey) >= 0 && x.RowKey.CompareTo(maxRowKey) <= 0);

            // TODO: continuation tokens, etc
            return query.AsTableServiceQuery();
        }

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
    }
}
