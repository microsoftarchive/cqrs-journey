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

using System.Data.Entity;
using System.IO;
using Infrastructure.Blob;
using Infrastructure.Serialization;

namespace Infrastructure.Sql.Blob
{
    /// <summary>
    /// Simple local blob storage simulator for easy local debugging. 
    /// Assumes the blobs are persisted as text through an <see cref="ITextSerializer"/>.
    /// </summary>
    public class BlobStorageDbContext : DbContext, IBlobStorage
    {
        public const string SchemaName = "BlobStorage";
        private ITextSerializer serializer;

        public BlobStorageDbContext(string nameOrConnectionString, ITextSerializer serializer)
            : base(nameOrConnectionString)
        {
            this.serializer = serializer;
        }

        public object Find(string id)
        {
            var blob = this.Set<BlobEntity>().Find(id);
            if (blob == null)
                return null;

            return this.serializer.Deserialize(new StringReader(blob.Data));
        }

        public void Save(string id, object blob)
        {
            var existing = this.Set<BlobEntity>().Find(id);
            string data = "";
            using (var writer = new StringWriter())
            {
                this.serializer.Serialize(writer, blob);
                data = writer.ToString();
            }

            if (existing != null)
            {
                existing.Data = data;
            }
            else
            {
                this.Set<BlobEntity>().Add(new BlobEntity(id, data));
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
