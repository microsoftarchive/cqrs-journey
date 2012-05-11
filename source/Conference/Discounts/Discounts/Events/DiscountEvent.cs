using System;
using Infrastructure.Sql.EventSourcing;

namespace Discounts.Events {
    public class DiscountEvent : Event {
        public Guid ConfID;
    }
}