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

namespace Infrastructure.Azure.EventLog
{
    using System;
    using System.Collections.Generic;
    using System.Data.Services.Client;
    using System.Linq;
    using System.Net;
    using Infrastructure.EventLog;
    using Infrastructure.Messaging;
    using Infrastructure.Serialization;
    using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.AzureStorage;
    using Microsoft.Practices.TransientFaultHandling;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;

    public class AzureEventLog : IEventLog
    {
        private readonly CloudStorageAccount account;
        private readonly string tableName;
        private readonly CloudTableClient tableClient;
        private Microsoft.Practices.TransientFaultHandling.RetryPolicy retryPolicy;
        private IMetadataProvider metadataProvider;
        private ITextSerializer serializer;

        public AzureEventLog(CloudStorageAccount account, string tableName, ITextSerializer serializer, IMetadataProvider metadataProvider)
        {
            if (account == null) throw new ArgumentNullException("account");
            if (tableName == null) throw new ArgumentNullException("tableName");
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("tableName");
            if (metadataProvider == null) throw new ArgumentNullException("metadataProvider");
            if (serializer == null) throw new ArgumentNullException("serializer");

            this.account = account;
            this.tableName = tableName;
            this.tableClient = account.CreateCloudTableClient();
            this.tableClient.RetryPolicy = RetryPolicies.NoRetry();
            this.metadataProvider = metadataProvider;
            this.serializer = serializer;

            // TODO: This could be injected.
            var retryStrategy = new ExponentialBackoff(10, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(1));

            this.retryPolicy = new RetryPolicy<ConflictDetectionStrategy>(retryStrategy);

            this.retryPolicy.ExecuteAction(() => tableClient.CreateTableIfNotExist(tableName));
        }

        public void Save(IEvent @event)
        {
            var partitionKey = DateTime.UtcNow.ToString("yyyMM");
            var rowKey = DateTime.UtcNow.Ticks.ToString("D20");
            var rnd = new Random((int)DateTime.UtcNow.Ticks);
            var metadata = this.metadataProvider.GetMetadata(@event);
            var payload = this.serializer.Serialize(@event);

            this.retryPolicy.ExecuteAction(() =>
            {
                var context = this.tableClient.GetDataServiceContext();

                context.AddObject(
                    this.tableName,
                    new EventLogEntity
                    {
                        PartitionKey = partitionKey,
                        RowKey = rowKey + "_" + rnd.Next(1, 999999).ToString("D6"),
                        SourceId = @event.SourceId.ToString(),
                        AssemblyName = metadata[StandardMetadata.AssemblyName],
                        FullName = metadata[StandardMetadata.FullName],
                        Namespace = metadata[StandardMetadata.Namespace],
                        TypeName = metadata[StandardMetadata.TypeName],
                        Payload = payload,
                    });

                context.SaveChanges();
            });
        }

        public IEnumerable<IEvent> Query(QueryCriteria criteria)
        {
            var context = this.tableClient.GetDataServiceContext();
            var query = (IQueryable<EventLogEntity>)context.CreateQuery<EventLogEntity>(this.tableName);
            var where = criteria.ToExpression();
            if (where != null)
                query = query.Where(where);

            return query
                .AsTableServiceQuery()
                .Execute()
                .Select(e => this.serializer.Deserialize<IEvent>(e.Payload));
        }

        private class ConflictDetectionStrategy : ITransientErrorDetectionStrategy
        {
            private ITransientErrorDetectionStrategy storageStrategy = new StorageTransientErrorDetectionStrategy();

            public bool IsTransient(Exception ex)
            {
                return this.storageStrategy.IsTransient(ex) || IsDataServiceConflict(ex);
            }

            private bool IsDataServiceConflict(Exception ex)
            {
                var requestException = ex as DataServiceRequestException;
                if (requestException == null)
                    return false;

                var clientException = requestException.InnerException as DataServiceClientException;
                if (clientException == null)
                    return false;

                return clientException.StatusCode == (int)HttpStatusCode.Conflict;
            }
        }
    }
}
