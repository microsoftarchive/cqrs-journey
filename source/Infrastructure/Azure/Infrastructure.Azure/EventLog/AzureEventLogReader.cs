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
    using System.Linq;
    using Infrastructure.EventLog;
    using Infrastructure.Messaging;
    using Infrastructure.Serialization;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;

    public class AzureEventLogReader : IEventLogReader
    {
        private readonly CloudStorageAccount account;
        private readonly string tableName;
        private readonly CloudTableClient tableClient;
        private ITextSerializer serializer;

        public AzureEventLogReader(CloudStorageAccount account, string tableName, ITextSerializer serializer)
        {
            if (account == null) throw new ArgumentNullException("account");
            if (tableName == null) throw new ArgumentNullException("tableName");
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("tableName");
            if (serializer == null) throw new ArgumentNullException("serializer");

            this.account = account;
            this.tableName = tableName;
            this.tableClient = account.CreateCloudTableClient();
            this.serializer = serializer;
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
    }
}
