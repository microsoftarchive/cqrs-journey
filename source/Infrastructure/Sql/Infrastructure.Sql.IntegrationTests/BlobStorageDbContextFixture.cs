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

namespace Infrastructure.Sql.IntegrationTests
{
    using System;
    using Infrastructure.Serialization;
    using Infrastructure.Sql.BlobStorage;
    using Xunit;
    using System.Text;

    public class BlobStorageDbContextFixture : IDisposable
    {
        private string dbName = "BlobStorageDbContextFixture_" + Guid.NewGuid().ToString();

        public BlobStorageDbContextFixture()
        {
            using (var context = new BlobStorageDbContext(dbName))
            {
                if (context.Database.Exists())
                {
                    context.Database.Delete();
                }

                context.Database.Create();
            }
        }

        public void Dispose()
        {
            using (var context = new BlobStorageDbContext(dbName))
            {
                if (context.Database.Exists())
                {
                    context.Database.Delete();
                }
            }
        }


        [Fact]
        public void when_saving_blob_then_can_read_it()
        {
            using (var storage = new BlobStorageDbContext(this.dbName))
            {
                storage.Save("test", "text/plain", Encoding.UTF8.GetBytes("Hello"));
            }

            using (var storage = new BlobStorageDbContext(this.dbName))
            {
                var data = Encoding.UTF8.GetString(storage.Find("test"));

                Assert.Equal("Hello", data);
            }
        }

        [Fact]
        public void when_updating_existing_blob_then_can_read_changes()
        {
            using (var storage = new BlobStorageDbContext(this.dbName))
            {
                storage.Save("test", "text/plain", Encoding.UTF8.GetBytes("Hello"));
            }

            using (var storage = new BlobStorageDbContext(this.dbName))
            {
                storage.Save("test", "text/plain", Encoding.UTF8.GetBytes("World"));
            }

            using (var storage = new BlobStorageDbContext(this.dbName))
            {
                var data = Encoding.UTF8.GetString(storage.Find("test"));

                Assert.Equal("World", data);
            }
        }
    }
}
