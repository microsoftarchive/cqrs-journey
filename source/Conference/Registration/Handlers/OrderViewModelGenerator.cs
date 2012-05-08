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
    using System.Diagnostics;
    using System.Linq;
    using Infrastructure.Messaging.Handling;
    using Registration.Events;
    using Registration.ReadModel;
    using Registration.ReadModel.Implementation;

    public class OrderViewModelGenerator :
        IEventHandler<OrderPlaced>, IEventHandler<OrderUpdated>,
        IEventHandler<OrderPartiallyReserved>, IEventHandler<OrderReservationCompleted>,
        IEventHandler<OrderRegistrantAssigned>,
        IEventHandler<OrderConfirmed>,
        IEventHandler<OrderTotalsCalculated>
    {
        private readonly Func<ConferenceRegistrationDbContext> contextFactory;

        public OrderViewModelGenerator(Func<ConferenceRegistrationDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public void Handle(OrderPlaced @event)
        {
            using (var context = this.contextFactory.Invoke())
            {
                var dto = new DraftOrder(@event.SourceId, @event.ConferenceId, DraftOrder.States.PendingReservation, @event.Version)
                {
                    AccessCode = @event.AccessCode,
                };
                dto.Lines.AddRange(@event.Seats.Select(seat => new DraftOrderItem(seat.SeatType, seat.Quantity)));

                context.Save(dto);
            }
        }

        public void Handle(OrderRegistrantAssigned @event)
        {
            using (var context = this.contextFactory.Invoke())
            {
                var dto = context.Find<DraftOrder>(@event.SourceId);
                if (dto != null)
                {
                    dto.RegistrantEmail = @event.Email;

                    dto.OrderVersion = @event.Version;

                    context.Save(dto);
                }
                else
                {
                    Trace.TraceError("Failed to locate Order read model for assigned registrant for order with id {0}.", @event.SourceId);
                }
            }
        }

        public void Handle(OrderUpdated @event)
        {
            using (var context = this.contextFactory.Invoke())
            {
                var dto = context.Set<DraftOrder>().Include(o => o.Lines).FirstOrDefault(o => o.OrderId == @event.SourceId);
                if (dto != null)
                {
                    var linesSet = context.Set<DraftOrderItem>();
                    foreach (var line in dto.Lines.ToArray())
                    {
                        linesSet.Remove(line);
                    }

                    dto.Lines.AddRange(@event.Seats.Select(seat => new DraftOrderItem(seat.SeatType, seat.Quantity)));

                    dto.State = DraftOrder.States.PendingReservation;
                    dto.OrderVersion = @event.Version;

                    context.Save(dto);
                }
                else
                {
                    Trace.TraceError("Failed to locate Order read model for updated order with id {0}.", @event.SourceId);
                }
            }
        }

        public void Handle(OrderPartiallyReserved @event)
        {
            this.UpdateReserved(@event.SourceId, @event.ReservationExpiration, DraftOrder.States.PartiallyReserved, @event.Version, @event.Seats);
        }

        public void Handle(OrderReservationCompleted @event)
        {
            this.UpdateReserved(@event.SourceId, @event.ReservationExpiration, DraftOrder.States.ReservationCompleted, @event.Version, @event.Seats);
        }

        public void Handle(OrderConfirmed @event)
        {
            using (var context = this.contextFactory.Invoke())
            {
                var dto = context.Find<DraftOrder>(@event.SourceId);
                if (dto != null)
                {
                    dto.State = DraftOrder.States.Confirmed;
                    if (dto.OrderVersion < @event.Version)
                    {
                        dto.OrderVersion = @event.Version;
                    }
                    context.Save(dto);
                }
                else
                {
                    Trace.TraceError("Failed to locate Order read model for confirmed payment for order with id {0}.", @event.SourceId);
                }
            }
        }

        public void Handle(OrderTotalsCalculated @event)
        {
            using (var context = this.contextFactory.Invoke())
            {
                var dto = context.Find<DraftOrder>(@event.SourceId);
                if (dto != null)
                {
                    if (dto.OrderVersion < @event.Version)
                    {
                        dto.OrderVersion = @event.Version;
                        context.Save(dto);
                    }
                }
                else
                {
                    Trace.TraceError("Failed to locate Order read model for calculated totals for order with id {0}.", @event.SourceId);
                }
            }
        }

        private void UpdateReserved(Guid orderId, DateTime reservationExpiration, DraftOrder.States state, int orderVersion, IEnumerable<SeatQuantity> seats)
        {
            using (var context = this.contextFactory.Invoke())
            {
                var dto = context.Set<DraftOrder>().Include(x => x.Lines).First(x => x.OrderId == orderId);
                if (dto != null)
                {
                    foreach (var seat in seats)
                    {
                        var item = dto.Lines.Single(x => x.SeatType == seat.SeatType);
                        item.ReservedSeats = seat.Quantity;
                    }

                    dto.State = state;
                    dto.ReservationExpirationDate = reservationExpiration;

                    dto.OrderVersion = orderVersion;

                    context.Save(dto);
                }
                else
                {
                    Trace.TraceError("Failed to locate Order read model for updated reservation for order with id {0}.", orderId);
                }
            }
        }
    }
}
