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
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;

    public class EventStore : IEventStore
    {
        private readonly CloudStorageAccount account;
        private readonly string tableName;
        private readonly CloudTableClient tableClient;
        private const string MaxVersion = "9999999999";

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
            var context = this.tableClient.GetDataServiceContext();
            var formattedVersion = version.ToString("D10");
            var query = context
                .CreateQuery<EventTableServiceEntity>(this.tableName)
                .Where(x => x.PartitionKey == partitionKey && x.RowKey.CompareTo(formattedVersion) >= 0 && x.RowKey.CompareTo(MaxVersion) <= 0);

            // TODO: error handling, continuation tokens, etc
            var all = query.AsTableServiceQuery().Execute();
            return all.Select(x => new EventData
                                       {
                                           // TODO: skip if version is not a number
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
                context.AddObject(
                    this.tableName,
                    new EventTableServiceEntity
                        {
                            PartitionKey = partitionKey,
                            RowKey = eventData.Version.ToString("D10"),
                            EventType = eventData.EventType,
                            Payload = eventData.Payload
                        });
            }

            // TODO: update record saying that there is a pending event to publish to the service bus

            // TODO: error handling and retrying
            try
            {
                context.SaveChanges(SaveChangesOptions.Batch);
            }
            catch (DataServiceRequestException ex)
            {
                var inner = ex.InnerException as DataServiceClientException;
                if (inner != null && inner.StatusCode == 409)
                {
                    throw new ConcurrencyException();
                }

                throw;
            }
        }
    }
}
