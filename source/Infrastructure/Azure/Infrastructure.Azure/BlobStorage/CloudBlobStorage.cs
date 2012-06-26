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

namespace Infrastructure.Azure.BlobStorage
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using Infrastructure.BlobStorage;
    using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.AzureStorage;
    using Microsoft.Practices.TransientFaultHandling;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;

    public class CloudBlobStorage : IBlobStorage
    {
        private readonly CloudStorageAccount account;
        private readonly string rootContainerName;
        private readonly CloudBlobClient blobClient;
        private readonly RetryPolicy<StorageTransientErrorDetectionStrategy> readRetryPolicy;
        private readonly RetryPolicy<StorageTransientErrorDetectionStrategy> writeRetryPolicy;

        public CloudBlobStorage(CloudStorageAccount account, string rootContainerName)
        {
            this.account = account;
            this.rootContainerName = rootContainerName;

            this.blobClient = account.CreateCloudBlobClient();
            this.blobClient.RetryPolicy = RetryPolicies.NoRetry();

            this.readRetryPolicy = new RetryPolicy<StorageTransientErrorDetectionStrategy>(new Incremental(2, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1)));
            this.readRetryPolicy.Retrying += (s, e) => Trace.TraceWarning("An error occurred in attempt number {1} to read from blob storage: {0}", e.LastException.Message, e.CurrentRetryCount);
            this.writeRetryPolicy = new RetryPolicy<StorageTransientErrorDetectionStrategy>(new ExponentialBackoff(3, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(3)));
            this.writeRetryPolicy.Retrying += (s, e) => Trace.TraceWarning("An error occurred in attempt number {1} to write to blob storage: {0}", e.LastException.Message, e.CurrentRetryCount);

            var containerReference = this.blobClient.GetContainerReference(this.rootContainerName);
            this.writeRetryPolicy.ExecuteAction(() => containerReference.CreateIfNotExist());
        }

        public byte[] Find(string id)
        {
            var containerReference = this.blobClient.GetContainerReference(this.rootContainerName);
            var blobReference = containerReference.GetBlobReference(id);

            return this.readRetryPolicy.ExecuteAction(() =>
                {
                    try
                    {
                        using (var stream = blobReference.OpenRead())
                        using (var resultStream = new MemoryStream())
                        {
                            stream.CopyTo(resultStream);
                            return resultStream.ToArray();
                        }
                    }
                    catch (StorageClientException e)
                    {
                        if (e.ErrorCode == StorageErrorCode.ResourceNotFound)
                        {
                            return null;
                        }

                        throw;
                    }
                });
        }

        public void Save(string id, string contentType, byte[] blob)
        {
            var client = this.account.CreateCloudBlobClient();
            var containerReference = client.GetContainerReference(this.rootContainerName);

            var blobReference = containerReference.GetBlobReference(id);

            this.writeRetryPolicy.ExecuteAction(() =>
                {
                    using (var stream = blobReference.OpenWrite())
                    {
                        stream.Write(blob, 0, blob.Length);
                    }

                    blobReference.Properties.ContentType = contentType;
                });
        }

        public void Delete(string id)
        {
            var client = this.account.CreateCloudBlobClient();
            var containerReference = client.GetContainerReference(this.rootContainerName);
            var blobReference = containerReference.GetBlobReference(id);

            this.writeRetryPolicy.ExecuteAction(() =>
                {
                    try
                    {
                        blobReference.DeleteIfExists();
                    }
                    catch (StorageClientException e)
                    {
                        if (e.ErrorCode != StorageErrorCode.ResourceNotFound)
                        {
                            throw;
                        }
                    }
                });
        }
    }
}
