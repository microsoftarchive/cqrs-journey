using Discounts.Events;

namespace Discounts.Infrastructure {
    public interface IStore {
        void Store(DiscountEvent @event);
    }
}