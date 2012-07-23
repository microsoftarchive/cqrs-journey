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

namespace Registration.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Diagnostics;
    using System.Linq;
    using AutoMapper;
    using Infrastructure.Messaging.Handling;
    using Registration.Events;
    using Registration.ReadModel;
    using Registration.ReadModel.Implementation;

    public class DraftOrderViewModelGenerator :
        IEventHandler<OrderPlaced>, IEventHandler<OrderUpdated>,
        IEventHandler<OrderPartiallyReserved>, IEventHandler<OrderReservationCompleted>,
        IEventHandler<OrderRegistrantAssigned>,
        IEventHandler<OrderConfirmed>, IEventHandler<OrderPaymentConfirmed>
    {
        private readonly Func<ConferenceRegistrationDbContext> contextFactory;

        static DraftOrderViewModelGenerator()
        {
            // Mapping old version of the OrderPaymentConfirmed event to the new version.
            // Currently it is being done explicitly by the consumer, but this one in particular could be done
            // at the deserialization level, as it is just a rename, not a functionality change.
            Mapper.CreateMap<OrderPaymentConfirmed, OrderConfirmed>();
        }

        public DraftOrderViewModelGenerator(Func<ConferenceRegistrationDbContext> contextFactory)
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
                if (WasNotAlreadyHandled(dto, @event.Version))
                {
                    dto.RegistrantEmail = @event.Email;
                    dto.OrderVersion = @event.Version;
                    context.Save(dto);
                }
            }
        }

        public void Handle(OrderUpdated @event)
        {
            using (var context = this.contextFactory.Invoke())
            {
                var dto = context.Set<DraftOrder>().Include(o => o.Lines).First(o => o.OrderId == @event.SourceId);
                if (WasNotAlreadyHandled(dto, @event.Version))
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

        public void Handle(OrderPaymentConfirmed @event)
        {
            this.Handle(Mapper.Map<OrderConfirmed>(@event));
        }

        public void Handle(OrderConfirmed @event)
        {
            using (var context = this.contextFactory.Invoke())
            {
                var dto = context.Find<DraftOrder>(@event.SourceId);
                if (WasNotAlreadyHandled(dto, @event.Version))
                {
                    dto.State = DraftOrder.States.Confirmed;
                    dto.OrderVersion = @event.Version;
                    context.Save(dto);
                }
            }
        }

        private void UpdateReserved(Guid orderId, DateTime reservationExpiration, DraftOrder.States state, int orderVersion, IEnumerable<SeatQuantity> seats)
        {
            using (var context = this.contextFactory.Invoke())
            {
                var dto = context.Set<DraftOrder>().Include(x => x.Lines).First(x => x.OrderId == orderId);
                if (WasNotAlreadyHandled(dto, orderVersion))
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
            }
        }

        private static bool WasNotAlreadyHandled(DraftOrder draftOrder, int eventVersion)
        {
            // This assumes that events will be handled in order, but we might get the same message more than once.
            if (eventVersion > draftOrder.OrderVersion)
            {
                return true;
            }
            else if (eventVersion == draftOrder.OrderVersion)
            {
                Trace.TraceWarning(
                    "Ignoring duplicate draft order update message with version {1} for order id {0}",
                    draftOrder.OrderId,
                    eventVersion);
                return false;
            }
            else
            {
                Trace.TraceWarning(
                    @"An older order update message was received with with version {1} for order id {0}, last known version {2}.
This read model generator has an expectation that the EventBus will deliver messages for the same source in order.",
                    draftOrder.OrderId,
                    eventVersion,
                    draftOrder.OrderVersion);
                return false;
            }
        }
    }
}
