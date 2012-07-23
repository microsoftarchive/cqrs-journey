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

namespace Registration.ReadModel.Implementation
{
    using System;
    using System.Data.Entity;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Infrastructure.BlobStorage;
    using Infrastructure.Serialization;
    using Registration.Handlers;

    public class OrderDao : IOrderDao
    {
        private readonly Func<ConferenceRegistrationDbContext> contextFactory;
        private IBlobStorage blobStorage;
        private ITextSerializer serializer;

        public OrderDao(Func<ConferenceRegistrationDbContext> contextFactory, IBlobStorage blobStorage, ITextSerializer serializer)
        {
            this.contextFactory = contextFactory;
            this.blobStorage = blobStorage;
            this.serializer = serializer;
        }

        public Guid? LocateOrder(string email, string accessCode)
        {
            using (var context = this.contextFactory.Invoke())
            {
                var orderProjection = context
                    .Query<DraftOrder>()
                    .Where(o => o.RegistrantEmail == email && o.AccessCode == accessCode)
                    .Select(o => new { o.OrderId })
                    .FirstOrDefault();

                if (orderProjection != null)
                {
                    return orderProjection.OrderId;
                }

                return null;
            }
        }

        public DraftOrder FindDraftOrder(Guid orderId)
        {
            using (var context = this.contextFactory.Invoke())
            {
                return context.Query<DraftOrder>().Include(x => x.Lines).FirstOrDefault(dto => dto.OrderId == orderId);
            }
        }

        public PricedOrder FindPricedOrder(Guid orderId)
        {
            using (var context = this.contextFactory.Invoke())
            {
                return context.Query<PricedOrder>().Include(x => x.Lines).FirstOrDefault(dto => dto.OrderId == orderId);
            }
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