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
    using System.Data.Entity.Infrastructure;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.Caching;
    using Conference;
    using Infrastructure.Messaging.Handling;
    using Registration.Events;
    using Registration.ReadModel;
    using Registration.ReadModel.Implementation;

    public class PricedOrderViewModelGenerator :
        IEventHandler<OrderPlaced>,
        IEventHandler<OrderTotalsCalculated>,
        IEventHandler<OrderConfirmed>,
        IEventHandler<OrderExpired>,
        IEventHandler<SeatAssignmentsCreated>,
        IEventHandler<SeatCreated>,
        IEventHandler<SeatUpdated>
    {
        private readonly Func<ConferenceRegistrationDbContext> contextFactory;
        private readonly ObjectCache seatDescriptionsCache;

        public PricedOrderViewModelGenerator(Func<ConferenceRegistrationDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
            this.seatDescriptionsCache = MemoryCache.Default;
        }

        public void Handle(OrderPlaced @event)
        {
            using (var context = this.contextFactory.Invoke())
            {
                var dto = new PricedOrder
                {
                    OrderId = @event.SourceId,
                    ReservationExpirationDate = @event.ReservationAutoExpiration,
                    OrderVersion = @event.Version
                };
                context.Set<PricedOrder>().Add(dto);
                try
                {
                    context.SaveChanges();
                }
                catch (DbUpdateException)
                {
                    Trace.TraceWarning(
                        "Ignoring OrderPlaced message with version {1} for order id {0}. This could be caused because the message was already handled and the PricedOrder entity was already created.",
                        dto.OrderId,
                        @event.Version);
                }
            }
        }

        public void Handle(OrderTotalsCalculated @event)
        {
            var seatTypeIds = @event.Lines.OfType<SeatOrderLine>().Select(x => x.SeatType).Distinct().ToArray();
            using (var context = this.contextFactory.Invoke())
            {
                var dto = context.Query<PricedOrder>().Include(x => x.Lines).First(x => x.OrderId == @event.SourceId);
                if (!WasNotAlreadyHandled(dto, @event.Version))
                {
                    // message already handled, skip.
                    return;
                }

                var linesSet = context.Set<PricedOrderLine>();
                foreach (var line in dto.Lines.ToList())
                {
                    linesSet.Remove(line);
                }

                var seatTypeDescriptions = GetSeatTypeDescriptions(seatTypeIds, context);

                for (int i = 0; i < @event.Lines.Length; i++)
                {
                    var orderLine = @event.Lines[i];
                    var line = new PricedOrderLine
                    {
                        LineTotal = orderLine.LineTotal,
                        Position = i,
                    };

                    var seatOrderLine = orderLine as SeatOrderLine;
                    if (seatOrderLine != null)
                    {
                        // should we update the view model to avoid losing the SeatTypeId?
                        line.Description = seatTypeDescriptions.Where(x => x.SeatTypeId == seatOrderLine.SeatType).Select(x => x.Name).FirstOrDefault();
                        line.UnitPrice = seatOrderLine.UnitPrice;
                        line.Quantity = seatOrderLine.Quantity;
                    }

                    dto.Lines.Add(line);
                }

                dto.Total = @event.Total;
                dto.IsFreeOfCharge = @event.IsFreeOfCharge;
                dto.OrderVersion = @event.Version;

                context.SaveChanges();
            }
        }

        public void Handle(OrderConfirmed @event)
        {
            using (var context = this.contextFactory.Invoke())
            {
                var dto = context.Find<PricedOrder>(@event.SourceId);
                if (WasNotAlreadyHandled(dto, @event.Version))
                {
                    dto.ReservationExpirationDate = null;
                    dto.OrderVersion = @event.Version;
                    context.Save(dto);
                }
            }
        }

        public void Handle(OrderExpired @event)
        {
            // No need to keep this priced order alive if it is expired.
            using (var context = this.contextFactory.Invoke())
            {
                var pricedOrder = new PricedOrder { OrderId = @event.SourceId };
                var set = context.Set<PricedOrder>();
                set.Attach(pricedOrder);
                set.Remove(pricedOrder);
                try
                {
                    context.SaveChanges();
                }
                catch (DbUpdateConcurrencyException)
                {
                    Trace.TraceWarning(
                        "Ignoring priced order expiration message with version {1} for order id {0}. This could be caused because the message was already handled and the entity was already deleted.",
                        pricedOrder.OrderId,
                        @event.Version);
                }
            }
        }

        /// <summary>
        /// Saves the seat assignments correlation ID for further lookup.
        /// </summary>
        public void Handle(SeatAssignmentsCreated @event)
        {
            using (var context = this.contextFactory.Invoke())
            {
                var dto = context.Find<PricedOrder>(@event.OrderId);
                dto.AssignmentsId = @event.SourceId;
                // Note: @event.Version does not correspond to order.Version.;
                context.SaveChanges();
            }
        }

        public void Handle(SeatCreated @event)
        {
            using (var context = this.contextFactory.Invoke())
            {
                var dto = context.Find<PricedOrderLineSeatTypeDescription>(@event.SourceId);
                if (dto == null)
                {
                    dto = new PricedOrderLineSeatTypeDescription { SeatTypeId = @event.SourceId };
                    context.Set<PricedOrderLineSeatTypeDescription>().Add(dto);
                }

                dto.Name = @event.Name;
                context.SaveChanges();
            }
        }

        public void Handle(SeatUpdated @event)
        {
            using (var context = this.contextFactory.Invoke())
            {
                var dto = context.Find<PricedOrderLineSeatTypeDescription>(@event.SourceId);
                if (dto == null)
                {
                    dto = new PricedOrderLineSeatTypeDescription { SeatTypeId = @event.SourceId };
                    context.Set<PricedOrderLineSeatTypeDescription>().Add(dto);
                }

                dto.Name = @event.Name;
                context.SaveChanges();
                this.seatDescriptionsCache.Set("SeatDescription_" + dto.SeatTypeId.ToString(), dto, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(5) });
            }
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
                Trace.TraceWarning(
                    @"Ignoring an older order update message was received with with version {1} for order id {0}, last known version {2}.
This read model generator has an expectation that the EventBus will deliver messages for the same source in order. Nevertheless, this warning can be expected in a migration scenario.",
                    pricedOrder.OrderId,
                    eventVersion,
                    pricedOrder.OrderVersion);
                return false;
            }
        } 
        
        private List<PricedOrderLineSeatTypeDescription> GetSeatTypeDescriptions(IEnumerable<Guid> seatTypeIds, ConferenceRegistrationDbContext context)
        {
            var result = new List<PricedOrderLineSeatTypeDescription>();
            var notCached = new List<Guid>();

            PricedOrderLineSeatTypeDescription cached;
            foreach (var seatType in seatTypeIds)
            {
                cached = (PricedOrderLineSeatTypeDescription)this.seatDescriptionsCache.Get("SeatDescription_" + seatType.ToString());
                if (cached == null)
                {
                    notCached.Add(seatType);
                }
                else
                {
                    result.Add(cached);
                }
            }

            if (notCached.Count > 0)
            {
                var notCachedArray = notCached.ToArray();
                var seatTypeDescriptions = context.Query<PricedOrderLineSeatTypeDescription>()
                    .Where(x => notCachedArray.Contains(x.SeatTypeId))
                    .ToList();

                foreach (var seatType in seatTypeDescriptions)
                {
                    // even though we went got a fresh version we don't want to overwrite a fresher version set by the event handler for seat descriptions
                    var desc = (PricedOrderLineSeatTypeDescription)this.seatDescriptionsCache
                        .AddOrGetExisting(
                            "SeatDescription_" + seatType.SeatTypeId.ToString(),
                            seatType,
                            new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(5) })
                        ?? seatType;

                    result.Add(desc);
                }
            }

            return result;
        }
    }
}
