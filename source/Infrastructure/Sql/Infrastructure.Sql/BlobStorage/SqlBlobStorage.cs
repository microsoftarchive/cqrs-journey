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

namespace Infrastructure.Sql.BlobStorage
{
    using Infrastructure.BlobStorage;
    using Infrastructure.Serialization;

    /// <summary>
    /// Simple local blob storage simulator for easy local debugging. 
    /// Assumes the blobs are persisted as text through an <see cref="ITextSerializer"/>.
    /// </summary>
    public class SqlBlobStorage : IBlobStorage
    {
        private string nameOrConnectionString;

        public SqlBlobStorage(string nameOrConnectionString)
        {
            this.nameOrConnectionString = nameOrConnectionString;
        }

        public byte[] Find(string id)
        {
            using (var context = new BlobStorageDbContext(this.nameOrConnectionString))
            {
                return context.Find(id);
            }
        }

        public void Save(string id, string contentType, byte[] blob)
        {
            using (var context = new BlobStorageDbContext(this.nameOrConnectionString))
            {
                context.Save(id, contentType, blob);
            }
        }

        public void Delete(string id)
        {
            using (var context = new BlobStorageDbContext(this.nameOrConnectionString))
            {
                context.Delete(id);
            }
        }
    }
}