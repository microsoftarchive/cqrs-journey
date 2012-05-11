using System;

namespace Discounts.Events {
    public class DiscountAddedEvent : DiscountEvent {
        public Guid DiscountID;
        public string Code;
        public int Percentage;
    }
}