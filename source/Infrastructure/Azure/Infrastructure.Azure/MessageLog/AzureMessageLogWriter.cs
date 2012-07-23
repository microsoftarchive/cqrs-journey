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

namespace Infrastructure.Azure.MessageLog
{
    using System;
    using System.Data.Services.Client;
    using System.Net;
    using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.AzureStorage;
    using Microsoft.Practices.TransientFaultHandling;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;

    public class AzureMessageLogWriter : IAzureMessageLogWriter
    {
        private readonly CloudStorageAccount account;
        private readonly string tableName;
        private readonly CloudTableClient tableClient;
        private Microsoft.Practices.TransientFaultHandling.RetryPolicy retryPolicy;

        public AzureMessageLogWriter(CloudStorageAccount account, string tableName)
        {
            if (account == null) throw new ArgumentNullException("account");
            if (tableName == null) throw new ArgumentNullException("tableName");
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("tableName");

            this.account = account;
            this.tableName = tableName;
            this.tableClient = account.CreateCloudTableClient();
            this.tableClient.RetryPolicy = RetryPolicies.NoRetry();

            var retryStrategy = new ExponentialBackoff(10, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(1));
            this.retryPolicy = new RetryPolicy<StorageTransientErrorDetectionStrategy>(retryStrategy);

            this.retryPolicy.ExecuteAction(() => tableClient.CreateTableIfNotExist(tableName));
        }

        public void Save(MessageLogEntity entity)
        {
            this.retryPolicy.ExecuteAction(() =>
            {
                var context = this.tableClient.GetDataServiceContext();

                context.AddObject(this.tableName, entity);

                try
                {
                    context.SaveChanges();
                }
                catch (DataServiceRequestException dsre)
                {
                    var clientException = dsre.InnerException as DataServiceClientException;
                    // If we get a conflict, we ignore it as we've already saved the message, 
                    // making this log idempotent.
                    if (clientException == null || clientException.StatusCode != (int)HttpStatusCode.Conflict)
                        throw;
                }
            });
        }
    }
}
