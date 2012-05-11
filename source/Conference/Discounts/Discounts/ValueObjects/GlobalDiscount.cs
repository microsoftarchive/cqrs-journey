using System;
using System.Collections.Generic;

namespace Discounts.ValueObjects {
    public class GlobalDiscount {
        public int Percentage;
        public IList<Guid> UsedBy = new List<Guid>();
    }
}