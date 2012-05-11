using System.Collections.Generic;
using Discounts.Commands;
using Infrastructure.Sql.EventSourcing;

namespace Discounts.Infrastructure {
    public interface IConsume {
        IEnumerable<Event> Consume(DiscountCommand discountCommand);
    }
}