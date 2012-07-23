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

namespace Infrastructure.Azure.IntegrationTests.Storage.BlobStorageFixture
{
    using System;
    using System.Text;
    using Infrastructure.Azure.BlobStorage;
    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.StorageClient;
    using Xunit;

    public class given_blob_storage : IDisposable
    {
        protected readonly CloudBlobStorage sut;
        protected readonly CloudStorageAccount account;
        protected readonly string rootContainerName;

        public given_blob_storage()
        {
            var settings = InfrastructureSettings.Read("Settings.xml").BlobStorage;
            this.account = CloudStorageAccount.Parse(settings.ConnectionString);
            this.rootContainerName = Guid.NewGuid().ToString();
            this.sut = new CloudBlobStorage(account, this.rootContainerName);
        }

        public void Dispose()
        {
            var client = this.account.CreateCloudBlobClient();
            var containerReference = client.GetContainerReference(this.rootContainerName);

            try
            {
                containerReference.Delete();
            }
            catch (StorageClientException)
            {
            }
        }
    }

    public class when_retrieving_from_non_existing_container : given_blob_storage
    {
        private byte[] bytes;

        public when_retrieving_from_non_existing_container()
        {
            this.bytes = this.sut.Find(Guid.NewGuid().ToString());
        }

        [Fact]
        public void then_returns_null()
        {
            Assert.Null(this.bytes);
        }
    }

    public class given_blob_storage_with_existing_root_container : IDisposable
    {
        protected readonly CloudBlobStorage sut;
        protected readonly CloudStorageAccount account;
        protected readonly string rootContainerName;

        public given_blob_storage_with_existing_root_container()
        {
            var settings = InfrastructureSettings.Read("Settings.xml").BlobStorage;
            this.account = CloudStorageAccount.Parse(settings.ConnectionString);
            this.rootContainerName = Guid.NewGuid().ToString();

            var client = this.account.CreateCloudBlobClient();
            var containerReference = client.GetContainerReference(this.rootContainerName);

            containerReference.Create();

            this.sut = new CloudBlobStorage(account, this.rootContainerName);
        }

        public void Dispose()
        {
            var client = this.account.CreateCloudBlobClient();
            var containerReference = client.GetContainerReference(this.rootContainerName);

            try
            {
                containerReference.Delete();
            }
            catch (StorageClientException)
            {
            }
        }
    }

    public class when_retrieving_non_existing_blob : given_blob_storage_with_existing_root_container
    {
        private byte[] bytes;

        public when_retrieving_non_existing_blob()
        {
            this.bytes = this.sut.Find(Guid.NewGuid().ToString());
        }

        [Fact]
        public void then_returns_null()
        {
            Assert.Null(this.bytes);
        }

        [Fact]
        public void then_can_delete_blob()
        {
            this.sut.Delete(Guid.NewGuid().ToString());
        }
    }

    public class when_saving_blob : given_blob_storage
    {
        private readonly byte[] bytes;
        private readonly string id;

        public when_saving_blob()
        {
            this.id = Guid.NewGuid().ToString();
            this.bytes = Guid.NewGuid().ToByteArray();

            this.sut.Save(this.id, "text/plain", this.bytes);
        }

        [Fact]
        public void then_writes_blob()
        {
            var client = this.account.CreateCloudBlobClient();
            var blobReference = client.GetBlobReference(this.rootContainerName + '/' + this.id);

            blobReference.FetchAttributes();
        }

        [Fact]
        public void then_can_find_blob()
        {
            var retrievedBytes = this.sut.Find(this.id);

            Assert.Equal(this.bytes, retrievedBytes);
        }

        [Fact]
        public void then_can_delete_blob()
        {
            this.sut.Delete(this.id);

            var retrievedBytes = this.sut.Find(this.id);

            Assert.Null(retrievedBytes);
        }

        [Fact]
        public void then_can_delete_multiple_times()
        {
            this.sut.Delete(this.id);
            this.sut.Delete(this.id);

            var retrievedBytes = this.sut.Find(this.id);

            Assert.Null(retrievedBytes);
        }

        [Fact]
        public void then_can_overwrite_blob()
        {
            var newBytes = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + Guid.NewGuid().ToString());

            this.sut.Save(this.id, "text/plain", newBytes);

            var retrievedBytes = this.sut.Find(this.id);

            Assert.Equal(newBytes, retrievedBytes);
        }
    }

    public class when_saving_blob_with_compound_id : given_blob_storage
    {
        private readonly byte[] bytes;
        private readonly string id;

        public when_saving_blob_with_compound_id()
        {
            this.id = Guid.NewGuid().ToString() + '/' + Guid.NewGuid().ToString();
            this.bytes = Guid.NewGuid().ToByteArray();

            this.sut.Save(this.id, "text/plain", this.bytes);
        }

        [Fact]
        public void then_writes_blob()
        {
            var client = this.account.CreateCloudBlobClient();
            var blobReference = client.GetBlobReference(this.rootContainerName + '/' + this.id);

            blobReference.FetchAttributes();
        }

        [Fact]
        public void then_can_find_blob()
        {
            var retrievedBytes = this.sut.Find(this.id);

            Assert.Equal(this.bytes, retrievedBytes);
        }
    }
}
