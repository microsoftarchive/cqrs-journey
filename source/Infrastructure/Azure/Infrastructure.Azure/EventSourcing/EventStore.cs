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
    using System.Collections.Generic;
    using System.Data.Services.Client;
    using System.Linq;
    using System.Net;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;

    public class EventStore : IEventStore, IPendingEventsQueue
    {
        private readonly CloudStorageAccount account;
        private readonly string tableName;
        private readonly CloudTableClient tableClient;
        private const string UnpublishedRowKeyPrefix = "Unpublished_";
        private const string UnpublishedRowKeyPrefixUpperLimit = "Unpublished`";
        private const string RowKeyVersionUpperLimit = "9999999999";

        public EventStore(CloudStorageAccount account, string tableName)
        {
            this.account = account;
            this.tableName = tableName;
            this.tableClient = account.CreateCloudTableClient();
            
            // TODO: error handling
            tableClient.CreateTableIfNotExist(tableName);
        }

        public IEnumerable<EventData> Load(string partitionKey, int version)
        {
            var minRowKey = version.ToString("D10");
            var all = this.GetEntities(partitionKey, minRowKey, RowKeyVersionUpperLimit);
            return all.Select(x => new EventData
                                       {
                                           Version = int.Parse(x.RowKey),
                                           EventType = x.EventType,
                                           Payload = x.Payload
                                       });
        }

        public void Save(string partitionKey, IEnumerable<EventData> events)
        {
            var context = this.tableClient.GetDataServiceContext();
            foreach (var eventData in events)
            {
                var formattedVersion = eventData.Version.ToString("D10");
                context.AddObject(
                    this.tableName,
                    new EventTableServiceEntity
                        {
                            PartitionKey = partitionKey,
                            RowKey = formattedVersion,
                            EventType = eventData.EventType,
                            Payload = eventData.Payload
                        });

                // Add a duplicate of this event to the Unpublished "queue"
                context.AddObject(
                    this.tableName,
                    new EventTableServiceEntity
                    {
                        PartitionKey = partitionKey,
                        RowKey = UnpublishedRowKeyPrefix + formattedVersion,
                        EventType = eventData.EventType,
                        Payload = eventData.Payload
                    });

            }

            // TODO: error handling and retrying
            try
            {
                context.SaveChanges(SaveChangesOptions.Batch);
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
            return this.GetEntities(partitionKey, UnpublishedRowKeyPrefix, UnpublishedRowKeyPrefixUpperLimit);
        }

        public void DeletePending(string partitionKey, string rowKey)
        {
            var context = this.tableClient.GetDataServiceContext();
            var item = new EventTableServiceEntity { PartitionKey = partitionKey, RowKey = rowKey };
            context.AttachTo(this.tableName, item, "*");
            context.DeleteObject(item);
            context.SaveChanges();
        }

        private IEnumerable<EventTableServiceEntity> GetEntities(string partitionKey, string minRowKey, string maxRowKey)
        {
            var context = this.tableClient.GetDataServiceContext();
            var query = context
                .CreateQuery<EventTableServiceEntity>(this.tableName)
                .Where(
                    x =>
                    x.PartitionKey == partitionKey && x.RowKey.CompareTo(minRowKey) >= 0 && x.RowKey.CompareTo(maxRowKey) <= 0);

            // TODO: error handling, continuation tokens, etc
            var all = query.AsTableServiceQuery().Execute();
            return all;
        }
    }
}
