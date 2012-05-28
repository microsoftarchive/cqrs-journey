using System.Collections.Generic;
using Discounts.Commands;
using Infrastructure.EventSourcing;

namespace Discounts.Infrastructure {
    public interface IConsume {
        IEnumerable<IVersionedEvent> Consume(DiscountCommand discountCommand);
    }
}