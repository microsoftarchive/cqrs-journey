// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
// http://cqrsjourney.github.com/contributors/members
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

namespace Registration
{
    using System.Collections.Generic;
    using System.Linq;

    public interface IPricingService
    {
        OrderTotal CalculateTotal(ICollection<SeatQuantity> seatItems);
    }

    public class PricingService : IPricingService
    {
        public OrderTotal CalculateTotal(ICollection<SeatQuantity> seatItems)
        {
            // stub implementation.
            return new OrderTotal
                       {
                           Total = 0,
                           Lines = seatItems.Select(x => new SeatOrderLine { SeatType = x.SeatType, UnitPrice = 0, LineTotal = 0 }).ToArray()
                       };
        }
    }
}