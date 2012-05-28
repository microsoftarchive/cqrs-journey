using Discounts.Events;

namespace Conference.Specflow.Support {
    public interface IStore {
        void Store(DiscountEvent @event);
    }
}