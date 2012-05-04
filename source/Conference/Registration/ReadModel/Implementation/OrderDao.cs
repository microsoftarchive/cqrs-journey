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
    using System.Data.Entity;
    using System.IO;
    using System.Linq;
    using Infrastructure.Blob;
    using Infrastructure.Serialization;

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

        public DraftOrder GetDraftOrder(Guid orderId)
        {
            using (var context = this.contextFactory.Invoke())
            {
                return context.Query<DraftOrder>().Include(x => x.Lines).FirstOrDefault(dto => dto.OrderId == orderId);
            }
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

        public PricedOrder GetPricedOrder(Guid orderId)
        {
            using (var context = this.contextFactory.Invoke())
            {
                return context.Query<PricedOrder>().Include(x => x.Lines).FirstOrDefault(dto => dto.OrderId == orderId);
            }
        }

        public OrderSeats FindOrderSeats(Guid orderId)
        {
            Guid? assignmentsId = null;
            using (var context = this.contextFactory.Invoke())
            {
                // Grab the correlation ID from the order.
                assignmentsId = (from order in context.Query<PricedOrder>()
                                 where order.OrderId == orderId
                                 select order.AssignmentsId)
                                .FirstOrDefault();
            }

            if (assignmentsId == null)
                return null;

            var blob = this.blobStorage.Find("SeatAssignments-" + assignmentsId.Value);
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