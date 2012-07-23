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

namespace Registration.IntegrationTests
{
    using System.Collections.Generic;
    using Infrastructure.BlobStorage;

    public class MemoryBlobStorage : IBlobStorage
    {
        private Dictionary<string, byte[]> blobs = new Dictionary<string, byte[]>();

        public byte[] Find(string id)
        {
            byte[] blob = null;
            this.blobs.TryGetValue(id, out blob);
            return blob;
        }

        public void Save(string id, string contentType, byte[] blob)
        {
            this.blobs[id] = blob;
        }

        public void Delete(string id)
        {
            this.blobs.Remove(id);
        }
    }
}
