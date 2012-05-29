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

namespace Infrastructure.Sql.BlobStorage
{
    using System.Data.Entity;
    using System.IO;

    public class BlobStorageDbContext : DbContext
    {
        public const string SchemaName = "BlobStorage";

        public BlobStorageDbContext(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
        }

        public byte[] Find(string id)
        {
            var blob = this.Set<BlobEntity>().Find(id);
            if (blob == null)
                return null;

            return blob.Blob;
        }

        public void Save(string id, string contentType, byte[] blob)
        {
            var existing = this.Set<BlobEntity>().Find(id);
            string blobString = "";
            if (contentType == "text/plain")
            {
                using (var stream = new MemoryStream(blob))
                using (var reader = new StreamReader(stream))
                {
                    blobString = reader.ReadToEnd();
                }
            }

            if (existing != null)
            {
                existing.Blob = blob;
                existing.BlobString = blobString;
            }
            else
            {
                this.Set<BlobEntity>().Add(new BlobEntity(id, contentType, blob, blobString));
            }

            this.SaveChanges();
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BlobEntity>().ToTable("Blobs", SchemaName);
        }
    }
}
