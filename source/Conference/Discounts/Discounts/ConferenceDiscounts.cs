using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Discounts.Commands;
using Discounts.Events;
using Discounts.Exceptions;
using Discounts.ValueObjects;
using Infrastructure.EventSourcing;

namespace Discounts { 
    public class ConferenceDiscounts : IEventSourced {
        private readonly Dictionary<Guid, PercentageDiscount> _discounts = new Dictionary<Guid, PercentageDiscount>();
        public ConferenceDiscounts(Guid id, IEnumerable<DiscountEvent> discountEvents) {
            Mapper.CreateMap<GlobalDiscountAddedEvent, PercentageDiscount>();
            Id = id;
            Events = discountEvents;
            Events.ToList().ForEach(Hydrate);
        }

        public IEnumerable<DiscountEvent> Consume(ApplyDiscountCommand applyDiscountCommand) { return ApplyDiscount(applyDiscountCommand.DiscountID, applyDiscountCommand.Total, applyDiscountCommand.Order); }
        public IEnumerable<DiscountEvent> Consume(AddDiscountCommand addDiscountCommand) { return AddDiscount(addDiscountCommand.DiscountID, addDiscountCommand.Code, addDiscountCommand.Percentage); }

        private IEnumerable<DiscountEvent> AddDiscount(Guid discountID, string code, int percentage) {
            _discounts[discountID] = new PercentageDiscount {Percentage = percentage};
            return new[] { ((DiscountEvent) new DiscountAddedEvent {
                DiscountID = discountID, Code = code, Percentage = percentage })};
        }

        private IEnumerable<DiscountEvent> ApplyDiscount(Guid discountID, decimal total, Guid user) {
            var discount = _discounts[discountID];
            var usedBy = discount.UsedBy;
            if (usedBy.Contains(user)) throw new DiscountAlreadyAppliedException();
            return new[] { ((DiscountEvent) new DiscountAppliedEvent {
                DiscountAmount = total * discount.Percentage / 100, DiscountID = discountID }) };
        }

        private void Hydrate(IVersionedEvent @event) {
            if (@event is GlobalDiscountAddedEvent) ApplyStateChange((GlobalDiscountAddedEvent) @event);
            else if (@event is DiscountAppliedEvent) ApplyStateChange((DiscountAppliedEvent) @event);
            else throw new EventIsNotHandledException();
        }

        private void ApplyStateChange(GlobalDiscountAddedEvent globalDiscountAddedEvent) {
            _discounts[globalDiscountAddedEvent.DiscountID] = Mapper.Map<GlobalDiscountAddedEvent, PercentageDiscount>(globalDiscountAddedEvent);
        }

        private void ApplyStateChange(DiscountAppliedEvent globalDiscountAdded) {
            var usedBy = _discounts[globalDiscountAdded.DiscountID].UsedBy;
            usedBy.Add(globalDiscountAdded.Order);
        }

        public Guid Id { get; private set; }
        public int Version { get { return Events.Count(); } }
        public IEnumerable<IVersionedEvent> Events { get; private set; }
    }
}
        //todo: add limit and refactor

//        private IEnumerable<LimitedDiscountEvent> ApplyDiscount(Guid discountID, decimal total, Guid user) {

//            var discount = _globalDiscounts[discountID];

//            var limit = discount.Limit;

//            if (discount.UsedBy.Count >= limit) throw new ExceededNUbmerOfRedemptionsForDiscount();

//            var usedBy = discount.UsedBy;

//            if (usedBy.Contains(user)) throw new DiscountAlreadyAppliedException();

//            return new[] { ((DiscountEvent) new DiscountAppliedEvent {

//                DiscountAmount = total * discount.Percentage / 100, DiscountID = discountID

//                }) };

//            

//        } 
