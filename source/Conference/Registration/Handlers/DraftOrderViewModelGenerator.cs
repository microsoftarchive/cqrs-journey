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
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using AutoMapper;
    using Infrastructure.BlobStorage;
    using Infrastructure.Messaging.Handling;
    using Infrastructure.Serialization;
    using Registration.Events;
    using Registration.ReadModel;

    public class DraftOrderViewModelGenerator :
        IEventHandler<OrderPlaced>, IEventHandler<OrderUpdated>,
        IEventHandler<OrderPartiallyReserved>, IEventHandler<OrderReservationCompleted>,
        IEventHandler<OrderRegistrantAssigned>,
        IEventHandler<OrderConfirmed>, IEventHandler<OrderPaymentConfirmed>,
        IEventHandler<OrderTotalsCalculated>
    {
        private readonly IBlobStorage blobStorage;
        private readonly ITextSerializer serializer;

        static DraftOrderViewModelGenerator()
        {
            // Mapping old version of the OrderPaymentConfirmed event to the new version.
            // Currently it is being done explicitly by the consumer, but this one in particular could be done
            // at the deserialization level, as it is just a rename, not a functionality change.
            Mapper.CreateMap<OrderPaymentConfirmed, OrderConfirmed>();
        }

        public DraftOrderViewModelGenerator(IBlobStorage blobStorage, ITextSerializer serializer)
        {
            this.blobStorage = blobStorage;
            this.serializer = serializer;
        }

        public static string GetDraftOrderBlobId(Guid sourceId)
        {
            return "DraftOrder/" + sourceId.ToString();
        }

        public void Handle(OrderPlaced @event)
        {
            var dto = new DraftOrder(@event.SourceId, @event.ConferenceId, DraftOrder.States.PendingReservation, @event.Version)
            {
                AccessCode = @event.AccessCode,
            };
            dto.Lines.AddRange(@event.Seats.Select(seat => new DraftOrderItem(seat.SeatType, seat.Quantity)));

            this.Save(dto);
        }

        public void Handle(OrderRegistrantAssigned @event)
        {
            var dto = this.Find(@event.SourceId);
            if (WasNotAlreadyHandled(dto, @event.Version))
            {
                dto.RegistrantEmail = @event.Email;
                dto.OrderVersion = @event.Version;

                this.Save(dto);
            }
        }

        public void Handle(OrderUpdated @event)
        {
            var dto = this.Find(@event.SourceId);
            if (WasNotAlreadyHandled(dto, @event.Version))
            {
                dto.Lines.Clear();

                dto.Lines.AddRange(@event.Seats.Select(seat => new DraftOrderItem(seat.SeatType, seat.Quantity)));

                dto.State = DraftOrder.States.PendingReservation;
                dto.OrderVersion = @event.Version;

                this.Save(dto);
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
            var dto = this.Find(@event.SourceId);
            if (WasNotAlreadyHandled(dto, @event.Version))
            {
                dto.State = DraftOrder.States.Confirmed;
                dto.OrderVersion = @event.Version;

                this.Save(dto);
            }
        }

        // TODO: why is this needed?
        public void Handle(OrderTotalsCalculated @event)
        {
            var dto = this.Find(@event.SourceId);
            if (WasNotAlreadyHandled(dto, @event.Version))
            {
                dto.OrderVersion = @event.Version;

                this.Save(dto);
            }
        }

        private void UpdateReserved(Guid orderId, DateTime reservationExpiration, DraftOrder.States state, int orderVersion, IEnumerable<SeatQuantity> seats)
        {
            var dto = this.Find(orderId);
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

                this.Save(dto);
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
                throw new InvalidOperationException(
                    string.Format(
                        @"An older order update message was received with with version {1} for order id {0}, last known version {2}.
This read model generator has an expectation that the EventBus will deliver messages for the same source in order.",
                        draftOrder.OrderId,
                        eventVersion,
                        draftOrder.OrderVersion));
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "By design")]
        private DraftOrder Find(Guid id)
        {
            var dto = this.blobStorage.Find(GetDraftOrderBlobId(id));
            if (dto == null)
            {
                return null;
            }

            using (var stream = new MemoryStream(dto))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                return (DraftOrder)this.serializer.Deserialize(reader);
            }
        }

        private void Save(DraftOrder dto)
        {
            using (var writer = new StringWriter())
            {
                this.serializer.Serialize(writer, dto);
                this.blobStorage.Save(GetDraftOrderBlobId(dto.OrderId), "text/plain", Encoding.UTF8.GetBytes(writer.ToString()));
            }
        }
    }
}
