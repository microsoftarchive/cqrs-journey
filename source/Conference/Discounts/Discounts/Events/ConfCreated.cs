using System;

namespace Discounts.Events {
    public class ConfCreated : DiscountEvent {
        public Guid ConfID { get; set; }
    }
}