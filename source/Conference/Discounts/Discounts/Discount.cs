using System;
using System.Collections.Generic;
using AutoMapper;
using Discounts.Commands;
using Discounts.Events;
using Discounts.Exceptions;
using Discounts.ValueObjects;
using Infrastructure.Sql.EventSourcing;

namespace Discounts {
    public class Discount {

        private readonly Dictionary<Guid, GlobalDiscount> _globalDiscounts;

        public Discount(List<DiscountEvent> events) {
            _globalDiscounts = new Dictionary<Guid, GlobalDiscount>();
            events.ForEach(Hydrate);
        }

        public IEnumerable<DiscountEvent> Consume(ApplyDiscountCommand applyDiscountCommand) {
            return ApplyDiscount(applyDiscountCommand.DiscountID, applyDiscountCommand.Total, applyDiscountCommand.Order);
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
        private IEnumerable<DiscountEvent> ApplyDiscount(Guid discountID, decimal total, Guid user) {
            var discount = _globalDiscounts[discountID];
            var usedBy = discount.UsedBy;
            if (usedBy.Contains(user)) throw new DiscountAlreadyAppliedException();
            return new[] { ((DiscountEvent) new DiscountAppliedEvent {
                DiscountAmount = total * discount.Percentage / 100, DiscountID = discountID }) };
        }
        private void Hydrate(Event @event) {
            if (@event is GlobalDiscountAddedEvent) ApplyStateChange((GlobalDiscountAddedEvent) @event);
            else if (@event is DiscountAppliedEvent) ApplyStateChange((DiscountAppliedEvent) @event);
            else throw new EventIsNotHandledException();
        }
        private void ApplyStateChange(GlobalDiscountAddedEvent globalDiscountAddedEvent) {
            _globalDiscounts[globalDiscountAddedEvent.DiscountID] = Mapper.Map<GlobalDiscountAddedEvent, GlobalDiscount>(globalDiscountAddedEvent);
        }
        private void ApplyStateChange(DiscountAppliedEvent globalDiscountAdded) {
            var usedBy = _globalDiscounts[globalDiscountAdded.DiscountID].UsedBy;
            usedBy.Add(globalDiscountAdded.Order);
        }
    }
}