using System;

namespace Discounts.Commands {
    public class AddDiscountCommand : DiscountCommand {
        public Guid DiscountID;
        public string Code;
        public int Percentage;
    }
}