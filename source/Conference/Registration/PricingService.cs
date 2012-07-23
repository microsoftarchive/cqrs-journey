// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
// http://go.microsoft.com/fwlink/p/?LinkID=258575
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

namespace Registration
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Registration.ReadModel;

    public class PricingService : IPricingService
    {
        private readonly IConferenceDao conferenceDao;

        public PricingService(IConferenceDao conferenceDao)
        {
            if (conferenceDao == null) throw new ArgumentNullException("conferenceDao");

            this.conferenceDao = conferenceDao;
        }

        public OrderTotal CalculateTotal(Guid conferenceId, ICollection<SeatQuantity> seatItems)
        {
            var seatTypes = this.conferenceDao.GetPublishedSeatTypes(conferenceId);
            var lineItems = new List<OrderLine>();
            foreach (var item in seatItems)
            {
                var seatType = seatTypes.FirstOrDefault(x => x.Id == item.SeatType);
                if (seatType == null)
                {
                    throw new InvalidDataException(string.Format(CultureInfo.InvariantCulture, "Invalid seat type ID '{0}' for conference with ID '{1}'", item.SeatType, conferenceId));
                }

                lineItems.Add(new SeatOrderLine { SeatType = item.SeatType, Quantity = item.Quantity, UnitPrice = seatType.Price, LineTotal = Math.Round(seatType.Price * item.Quantity, 2) });
            }

            return new OrderTotal
                       {
                           Total = lineItems.Sum(x => x.LineTotal),
                           Lines = lineItems
                       };
        }
    }
}