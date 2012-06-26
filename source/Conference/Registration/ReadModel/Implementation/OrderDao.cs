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
            // TODO add support for locating orders
            return null;

            //using (var context = this.contextFactory.Invoke())
            //{
            //    var orderProjection = context
            //        .Query<DraftOrder>()
            //        .Where(o => o.RegistrantEmail == email && o.AccessCode == accessCode)
            //        .Select(o => new { o.OrderId })
            //        .FirstOrDefault();

            //    if (orderProjection != null)
            //    {
            //        return orderProjection.OrderId;
            //    }

            //    return null;
            //}
        }

        public DraftOrder FindDraftOrder(Guid orderId)
        {
            var blob = this.blobStorage.Find(DraftOrderViewModelGenerator.GetDraftOrderBlobId(orderId));
            if (blob == null)
            {
                return null;
            }

            using (var stream = new MemoryStream(blob))
            using (var reader = new StreamReader(stream))
            {
                return (DraftOrder)this.serializer.Deserialize(reader);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "By design")]
        public PricedOrder FindPricedOrder(Guid orderId)
        {
            var blob = this.blobStorage.Find(PricedOrderViewModelGenerator.GetPricedOrderBlobId(orderId));
            if (blob == null)
            {
                return null;
            }

            using (var stream = new MemoryStream(blob))
            using (var reader = new StreamReader(stream))
            {
                return (PricedOrder)this.serializer.Deserialize(reader);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "By design")]
        public OrderSeats FindOrderSeats(Guid assignmentsId)
        {
            var blob = this.blobStorage.Find("SeatAssignments-" + assignmentsId);
            if (blob == null)
                return null;

            using (var stream = new MemoryStream(blob))
            using (var reader = new StreamReader(stream))
            {
                return (OrderSeats)this.serializer.Deserialize(reader);
            }
        }
    }
}