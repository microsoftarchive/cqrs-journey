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

namespace Registration.Handlers
{
    using System;
    using System.Data.Entity;
    using System.Linq;
    using Infrastructure.Messaging.Handling;
    using Registration.Events;
    using Registration.ReadModel;
    using Registration.ReadModel.Implementation;

    public class PricedOrderViewModelGenerator : IEventHandler<OrderTotalsCalculated>, IEventHandler<OrderExpired>
    {
        private readonly Func<ConferenceRegistrationDbContext> contextFactory;
        private IConferenceDao conferenceDao;

        public PricedOrderViewModelGenerator(IConferenceDao conferenceDao, Func<ConferenceRegistrationDbContext> contextFactory)
        {
            this.conferenceDao = conferenceDao;
            this.contextFactory = contextFactory;
        }

        public void Handle(OrderTotalsCalculated @event)
        {
            var seatTypeIds = @event.Lines.OfType<SeatOrderLine>().Select(x => x.SeatType).ToArray();
            using (var context = this.contextFactory.Invoke())
            {
                var dto = context.Query<PricedOrder>().Include(x => x.Lines).FirstOrDefault(x => x.OrderId == @event.SourceId);
                if (dto == null)
                {
                    dto = new PricedOrder { OrderId = @event.SourceId };
                    context.Set<PricedOrder>().Add(dto);
                }
                else
                {
                    var linesSet = context.Set<PricedOrderLine>();
                    foreach (var line in dto.Lines.ToList())
                    {
                        linesSet.Remove(line);
                    }
                }

                var seatTypeDescriptions = this.conferenceDao.GetSeatTypeNames(seatTypeIds);

                foreach (var orderLine in @event.Lines)
                {
                    var line = new PricedOrderLine
                    {
                        LineTotal = orderLine.LineTotal
                    };

                    var seatOrderLine = orderLine as SeatOrderLine;
                    if (seatOrderLine != null)
                    {
                        line.Description = seatTypeDescriptions.Where(x => x.Id == seatOrderLine.SeatType).Select(x => x.Name).FirstOrDefault();
                        line.UnitPrice = seatOrderLine.UnitPrice;
                        line.Quantity = seatOrderLine.Quantity;
                    }

                    dto.Lines.Add(line);
                }

                dto.Total = @event.Total;

                context.SaveChanges();
            }
        }
        public void Handle(OrderExpired @event)
        {
            // No need to keep this totalled order alive if it is expired.
            using (var context = this.contextFactory.Invoke())
            {
                var dto = context.Find<PricedOrder>(@event.SourceId);
                if (dto != null)
                {
                    context.Set<PricedOrder>().Remove(dto);
                    context.SaveChanges();
                }
            }
        }
    }
}
