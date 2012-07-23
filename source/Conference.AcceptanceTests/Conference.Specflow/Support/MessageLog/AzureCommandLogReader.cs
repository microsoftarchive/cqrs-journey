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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Infrastructure;
using Infrastructure.Azure.MessageLog;
using Infrastructure.MessageLog;
using Infrastructure.Messaging;
using Infrastructure.Serialization;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace Conference.Specflow.Support.MessageLog
{
    public class AzureCommandLogReader : AzureEventLogReader, ICommandLogReader
    {
        private readonly string tableName;
        private readonly ITextSerializer serializer;
        private readonly CloudTableClient tableClient;

        public AzureCommandLogReader(CloudStorageAccount account, string tableName, ITextSerializer serializer) : 
            base(account, tableName, serializer)
        {
            this.tableName = tableName;
            this.serializer = serializer;
            this.tableClient = account.CreateCloudTableClient();
        }

        public new IEnumerable<ICommand> Query(QueryCriteria criteria)
        {
            var context = this.tableClient.GetDataServiceContext();
            var query = (IQueryable<MessageLogEntity>)context.CreateQuery<MessageLogEntity>(this.tableName)
                .Where(x => x.Kind == StandardMetadata.CommandKind);

            var where = criteria.ToExpression();
            if (where != null)
                query = query.Where(where);

            return query
                .AsTableServiceQuery()
                .Execute()
                .Select(e => this.serializer.Deserialize<ICommand>(e.Payload));
        }
    }
}
