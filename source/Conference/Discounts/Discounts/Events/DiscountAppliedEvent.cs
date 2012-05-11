using System;

namespace Discounts.Events {
    public class DiscountAppliedEvent : DiscountEvent {
        public decimal DiscountAmount;
        public Guid DiscountID;
        public Guid Order;
    }
}