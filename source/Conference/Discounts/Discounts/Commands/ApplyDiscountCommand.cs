using System;

namespace Discounts.Commands {
    public class ApplyDiscountCommand : DiscountCommand {
        public Guid DiscountID;
        public decimal Total;
        public Guid Order;
    }
}