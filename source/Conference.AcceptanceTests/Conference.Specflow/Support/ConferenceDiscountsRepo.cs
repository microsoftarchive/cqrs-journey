using System;
using System.Collections.Generic;
using Discounts;
using Discounts.Events;
using Infrastructure.EventSourcing;

namespace Conference.Specflow.Support {
    public class ConferenceDiscountsRepo : IEventSourcedRepository<ConferenceDiscounts>, IStore {
        private Dictionary<Guid, List<DiscountEvent>> _events;

        public ConferenceDiscountsRepo() {
            _events = new Dictionary<Guid, List<DiscountEvent>>();
        }

        public ConferenceDiscounts Find(Guid id) {
            throw new NotImplementedException();
        }

        public ConferenceDiscounts Get(Guid id) {
            return new ConferenceDiscounts(id, _events[id]);
        }

        public void Save(ConferenceDiscounts eventSourced) {
            _events[eventSourced.Id] = (List<DiscountEvent>) eventSourced.Events;
        }
        public void Store(DiscountEvent discountEvent) {
            if (discountEvent is ConfCreated) CreateEmptyConfDiscountsFor(discountEvent.ID);
            else AddEventToConference(discountEvent);
        }

        private void AddEventToConference(DiscountEvent discountEvent) {
            _events[discountEvent.ID].Add(discountEvent);
        }

        private void CreateEmptyConfDiscountsFor(Guid confDiscountsID) {
            _events[confDiscountsID] = new List<DiscountEvent>();
        }

    }
}