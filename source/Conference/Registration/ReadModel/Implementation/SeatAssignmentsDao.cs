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

namespace Registration.ReadModel.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Infrastructure.Blob;
    using Infrastructure.Serialization;
    using System.IO;

    public class SeatAssignmentsDao : ISeatAssignmentsDao
    {
        private IBlobStorage storage;
        private ITextSerializer serializer;

        public SeatAssignmentsDao(IBlobStorage storage, ITextSerializer serializer)
        {
            this.storage = storage;
            this.serializer = serializer;
        }

        public SeatAssignmentsDTO Find(Guid orderId)
        {
            var blob = this.storage.Find("SeatAssignments-" + orderId);
            if (blob == null)
                return null;

            using (var stream = new MemoryStream(blob))
            using (var reader = new StreamReader(stream))
            {
                return (SeatAssignmentsDTO)this.serializer.Deserialize(reader);
            }
        }
    }
}
