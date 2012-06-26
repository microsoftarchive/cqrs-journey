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
    using System.IO;
    using System.Text;
    using Infrastructure.BlobStorage;
    using Infrastructure.Serialization;
    using Registration.Handlers;

    public class OrderDao : IOrderDao
    {
        private IBlobStorage blobStorage;
        private ITextSerializer serializer;

        public OrderDao(IBlobStorage blobStorage, ITextSerializer serializer)
        {
            this.blobStorage = blobStorage;
            this.serializer = serializer;
        }

        public Guid? LocateOrder(string email, string accessCode)
        {
            var blob = this.FindBlob<OrderLocator>(DraftOrderViewModelGenerator.GetOrderLocatorBlobId(accessCode, email));
            return blob != null ? blob.OrderId : (Guid?)null;
        }

        public DraftOrder FindDraftOrder(Guid orderId)
        {
            return FindBlob<DraftOrder>(DraftOrderViewModelGenerator.GetDraftOrderBlobId(orderId));
        }

        public PricedOrder FindPricedOrder(Guid orderId)
        {
            return FindBlob<PricedOrder>(PricedOrderViewModelGenerator.GetPricedOrderBlobId(orderId));
        }

        public OrderSeats FindOrderSeats(Guid assignmentsId)
        {
            return FindBlob<OrderSeats>(SeatAssignmentsViewModelGenerator.GetSeatAssignmentsBlobId(assignmentsId));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "By design")]
        private T FindBlob<T>(string id)
            where T : class
        {
            var dto = this.blobStorage.Find(id);
            if (dto == null)
            {
                return null;
            }

            using (var stream = new MemoryStream(dto))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                return (T)this.serializer.Deserialize(reader);
            }
        }
    }
}