using System;

namespace Discounts.Events {
    public class GlobalDiscountAddedEvent : DiscountEvent {
        public Guid DiscountID;
        public int Percentage;
    }
}