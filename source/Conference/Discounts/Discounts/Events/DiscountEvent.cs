using System;
using Infrastructure.EventSourcing;

namespace Discounts.Events {
    public class DiscountEvent : IVersionedEvent {
        public Guid ID { get; set; }
        public Guid SourceId { get; private set; }
        public int Version { get; private set; }
    }
}