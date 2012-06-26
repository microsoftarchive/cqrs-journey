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
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Conference;
    using Infrastructure.BlobStorage;
    using Infrastructure.Messaging.Handling;
    using Infrastructure.Serialization;
    using Registration.Events;
    using Registration.ReadModel;

    public class PricedOrderViewModelGenerator :
        IEventHandler<OrderPlaced>,
        IEventHandler<OrderTotalsCalculated>,
        IEventHandler<OrderConfirmed>,
        IEventHandler<OrderExpired>,
        IEventHandler<SeatAssignmentsCreated>,
        IEventHandler<SeatCreated>,
        IEventHandler<SeatUpdated>
    {
        private readonly IBlobStorage blobStorage;
        private readonly ITextSerializer serializer;

        public PricedOrderViewModelGenerator(IBlobStorage blobStorage, ITextSerializer serializer)
        {
            this.blobStorage = blobStorage;
            this.serializer = serializer;
        }

        public static string GetPricedOrderBlobId(Guid sourceId)
        {
            return "PricedOrder/" + sourceId.ToString();
        }

        public static string GetConferenceSeatsDescriptionBlobId(Guid sourceId)
        {
            return "ConferenceSeats/" + sourceId.ToString();
        }

        public void Handle(OrderPlaced @event)
        {
            var dto = this.Find<PricedOrder>(GetPricedOrderBlobId(@event.SourceId));

            if (dto == null)
            {
                dto = new PricedOrder { OrderId = @event.SourceId, ConferenceId = @event.ConferenceId };
            }
            else if (!WasNotAlreadyHandled(dto, @event.Version))
            {
                return;
            }

            dto.ReservationExpirationDate = @event.ReservationAutoExpiration;
            dto.OrderVersion = @event.Version;

            this.Save(dto, GetPricedOrderBlobId(@event.SourceId));
        }

        public void Handle(OrderTotalsCalculated @event)
        {
            var seatTypeIds = @event.Lines.OfType<SeatOrderLine>().Select(x => x.SeatType).Distinct().ToArray();

            var dto = this.Find<PricedOrder>(GetPricedOrderBlobId(@event.SourceId));
            if (dto == null)
            {
                dto = new PricedOrder { OrderId = @event.SourceId };
            }
            else if (WasNotAlreadyHandled(dto, @event.Version))
            {
                dto.Lines.Clear();
            }
            else
            {
                // message already handled, skip.
                return;
            }

            var conferenceSeatDescriptions =
                this.Find<PricedOrderConferenceSeatTypeDescriptions>(GetConferenceSeatsDescriptionBlobId(dto.ConferenceId)) ?? new PricedOrderConferenceSeatTypeDescriptions();

            foreach (var orderLine in @event.Lines)
            {
                var line = new PricedOrderLine
                {
                    LineTotal = orderLine.LineTotal
                };

                var seatOrderLine = orderLine as SeatOrderLine;
                if (seatOrderLine != null)
                {
                    string description;
                    conferenceSeatDescriptions.SeatDescriptions.TryGetValue(seatOrderLine.SeatType, out description);

                    // should we update the view model to avoid loosing the SeatTypeId?
                    line.Description = description;
                    line.UnitPrice = seatOrderLine.UnitPrice;
                    line.Quantity = seatOrderLine.Quantity;
                }

                dto.Lines.Add(line);
            }

            dto.Total = @event.Total;
            dto.IsFreeOfCharge = @event.IsFreeOfCharge;
            dto.OrderVersion = @event.Version;

            this.Save(dto, GetPricedOrderBlobId(@event.SourceId));
        }

        public void Handle(OrderConfirmed @event)
        {
            var dto = this.Find<PricedOrder>(GetPricedOrderBlobId(@event.SourceId));
            if (WasNotAlreadyHandled(dto, @event.Version))
            {
                dto.ReservationExpirationDate = null;
                dto.OrderVersion = @event.Version;
                this.Save(dto, GetPricedOrderBlobId(@event.SourceId));
            }
        }

        public void Handle(OrderExpired @event)
        {
            this.blobStorage.Delete(GetPricedOrderBlobId(@event.SourceId));
        }

        /// <summary>
        /// Saves the seat assignments correlation ID for further lookup.
        /// </summary>
        public void Handle(SeatAssignmentsCreated @event)
        {
            var dto = this.Find<PricedOrder>(GetPricedOrderBlobId(@event.OrderId));
            dto.AssignmentsId = @event.SourceId;
            // Note: @event.Version does not correspond to order.Version.;
            this.Save(dto, GetPricedOrderBlobId(@event.OrderId));
        }

        public void Handle(SeatCreated @event)
        {
            this.SetSeatName(@event.ConferenceId, @event.SourceId, @event.Name);
        }

        public void Handle(SeatUpdated @event)
        {
            this.SetSeatName(@event.ConferenceId, @event.SourceId, @event.Name);
        }

        private static bool WasNotAlreadyHandled(PricedOrder pricedOrder, int eventVersion)
        {
            // This assumes that events will be handled in order, but we might get the same message more than once.
            if (eventVersion > pricedOrder.OrderVersion)
            {
                return true;
            }
            else if (eventVersion == pricedOrder.OrderVersion)
            {
                Trace.TraceWarning(
                    "Ignoring duplicate priced order update message with version {1} for order id {0}",
                    pricedOrder.OrderId,
                    eventVersion);
                return false;
            }
            else
            {
                throw new InvalidOperationException(
                    string.Format(
                        @"An older order update message was received with with version {1} for order id {0}, last known version {2}.
This read model generator has an expectation that the EventBus will deliver messages for the same source in order.",
                        pricedOrder.OrderId,
                        eventVersion,
                        pricedOrder.OrderVersion));
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "By design")]
        private T Find<T>(string id)
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

        private void Save<T>(T dto, string id)
            where T : class
        {
            using (var writer = new StringWriter())
            {
                this.serializer.Serialize(writer, dto);
                this.blobStorage.Save(id, "text/plain", Encoding.UTF8.GetBytes(writer.ToString()));
            }
        }

        private void SetSeatName(Guid conferenceId, Guid seatId, string name)
        {
            var dto = this.Find<PricedOrderConferenceSeatTypeDescriptions>(GetConferenceSeatsDescriptionBlobId(conferenceId));
            if (dto == null)
            {
                dto = new PricedOrderConferenceSeatTypeDescriptions { ConferenceId = conferenceId };
            }

            dto.SeatDescriptions[seatId] = name;

            this.Save(dto, GetConferenceSeatsDescriptionBlobId(conferenceId));
        }
    }
}
