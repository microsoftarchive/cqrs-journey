using System;
using System.Collections.Generic;
using AutoMapper;
using Discounts.Commands;
using Discounts.Events;
using Discounts.Exceptions;
using Discounts.Infrastructure;
using Discounts.ValueObjects;
using Infrastructure.Sql.EventSourcing;

namespace Discounts {
    public class DiscountDomain : IStore, IConsume {
        private readonly Dictionary<Guid, List<DiscountEvent>> _conferenceEvents;
        public DiscountDomain() {
            Mapper.CreateMap<GlobalDiscountAddedEvent, PercentageDiscount>();
            _conferenceEvents = new Dictionary<Guid, List<DiscountEvent>>();
        }

        public void Store(DiscountEvent discountEvent) {
            if (discountEvent is ConfCreated) _conferenceEvents[discountEvent.ConfID] = new List<DiscountEvent>();
            else _conferenceEvents[discountEvent.ConfID].Add(discountEvent);
        }

        public IEnumerable<Event> Consume(DiscountCommand discountCommand) {
            var discount = new Discount(_conferenceEvents[discountCommand.ConfID]);
            if (discountCommand is ApplyDiscountCommand) {
                var discountEvents = discount.Consume((ApplyDiscountCommand) discountCommand);   
                foreach (var discountEvent in discountEvents) {
                    AddContext(discountCommand, discountEvent);
                    Store(discountEvent);
                    yield return discountEvent;
                }
            }
            else if (discountCommand is AddDiscountCommand) {
                var discountEvents = discount.Consume((AddDiscountCommand) discountCommand);   
                foreach (var discountEvent in discountEvents) {
                    AddContext(discountCommand, discountEvent);
                    Store(discountEvent);
                    yield return discountEvent;
                }
            }
            else throw new CommandNotHandledException();
        }

        private static void AddContext(DiscountCommand discountCommand, DiscountEvent discountEvent) {
            discountEvent.ConfID = discountCommand.ConfID;
        }
    }
}