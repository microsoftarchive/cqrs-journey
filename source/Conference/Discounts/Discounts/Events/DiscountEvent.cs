using System;
using Infrastructure.EventSourcing;

namespace Discounts.Events {
    public class DiscountEvent : IVersionedEvent {
        public Guid ConfID;

        public Guid SourceId {
            get {  }
        }

        public int Version {
            get { throw new NotImplementedException(); }
        }
    }
}