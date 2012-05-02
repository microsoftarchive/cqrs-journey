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
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Linq;
    using Infrastructure.Messaging.Handling;
    using Registration.Events;
    using Registration.ReadModel;
    using Registration.ReadModel.Implementation;

    public class TotalledOrderViewModelGenerator : IEventHandler<OrderTotalsCalculated>, IEventHandler<OrderExpired>
    {
        private readonly Func<ConferenceRegistrationDbContext> contextFactory;

        public TotalledOrderViewModelGenerator(Func<ConferenceRegistrationDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public void Handle(OrderTotalsCalculated @event)
        {
            var seatTypeIds = @event.Lines.OfType<SeatOrderLine>().Select(x => x.SeatType).ToArray();
            using (var context = this.contextFactory.Invoke())
            {
                var dto = context.Query<TotalledOrder>().Include(x => x.Lines).FirstOrDefault(x => x.OrderId == @event.SourceId);
                if (dto == null)
                {
                    dto = new TotalledOrder { OrderId = @event.SourceId };
                }
                else
                {
                    dto.Lines.Clear();
                }

                if (seatTypeIds.Length > 0)
                {
                    // if there are no seat type IDs, there is no need for the following IN query.
                    var seatTypeDescriptions = context.Query<ConferenceSeatTypeDTO>().Where(x => seatTypeIds.Contains(x.Id)).Select(x => new { x.Id, x.Description }).ToList();

                    foreach (var orderLine in @event.Lines)
                    {
                        var line = new TotalledOrderLine
                                       {
                                           LineTotal = orderLine.LineTotal
                                       };

                        var seatOrderLine = orderLine as SeatOrderLine;
                        if (seatOrderLine != null)
                        {
                            line.Description = seatTypeDescriptions.Where(x => x.Id == seatOrderLine.SeatType).Select(x => x.Description).FirstOrDefault();
                            line.UnitPrice = seatOrderLine.UnitPrice;
                            line.Quantity = seatOrderLine.Quantity;
                        }
                        
                        dto.Lines.Add(line);
                    }
                }
                else
                {
                    dto.Lines.AddRange(@event.Lines.Select(x => new TotalledOrderLine { LineTotal = x.LineTotal }));
                }

                dto.Total = @event.Total;

                context.Save(dto);
            }
        }
        public void Handle(OrderExpired @event)
        {
            // No need to keep this totalled order alive if it is expired.
            using(var context = this.contextFactory.Invoke())
            {
                var dto = context.Find<TotalledOrder>(@event.SourceId);
                if (dto != null)
                {
                    context.Set<TotalledOrder>().Remove(dto);
                    context.SaveChanges();
                }
            }
        }
    }
}
