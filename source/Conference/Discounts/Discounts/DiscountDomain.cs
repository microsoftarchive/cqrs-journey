using System;
using System.Collections.Generic;
using AutoMapper;
using Discounts.Commands;
using Discounts.Events;
using Discounts.Infrastructure;
using Discounts.ValueObjects;
using Infrastructure.Sql.EventSourcing;

namespace Discounts {
    public class DiscountDomain : IStore, IConsume {
        private Dictionary<Guid, List<DiscountEvent>> _conferences;
        public DiscountDomain() {
            Mapper.CreateMap<GlobalDiscountAddedEvent, GlobalDiscount>();
            _conferences = new Dictionary<Guid, List<DiscountEvent>>();
        }

        public void Store(DiscountEvent discountEvent) {
            if (discountEvent is ConfCreated) _conferences[discountEvent.ConfID] = new List<DiscountEvent>();
            else _conferences[discountEvent.ConfID].Add(discountEvent);
        }

        public IEnumerable<Event> Consume(DiscountCommand discountCommand) {
            var discount = new Discount(_conferences[discountCommand.ConfID]);
            if (discountCommand is ApplyDiscountCommand) {
                var discountEvents = discount.Consume((ApplyDiscountCommand) discountCommand);   
                foreach (var discountEvent in discountEvents) {
                    AddContext(discountCommand, discountEvent);
                    Store(discountEvent);
                    yield return discountEvent;
                }
            }
        }

        private static Guid AddContext(DiscountCommand discountCommand, DiscountEvent discountEvent) {
            return discountEvent.ConfID = discountCommand.ConfID;
        }
    }
}