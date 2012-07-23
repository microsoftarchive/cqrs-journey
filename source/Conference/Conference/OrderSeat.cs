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

namespace Conference
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public class OrderSeat
    {
        public OrderSeat(Guid orderId, int position, Guid seatInfoId)
            : this()
        {
            this.OrderId = orderId;
            this.Position = position;
            this.SeatInfoId = seatInfoId;
        }

        protected OrderSeat()
        {
            // Complex type properties can never be 
            // null.
            this.Attendee = new Attendee();
        }

        public int Position { get; set; }
        public Guid OrderId { get; set; }
        public Attendee Attendee { get; set; }

        /// <summary>
        /// Typical pattern for foreign key relationship 
        /// in EF. The identifier is all that's needed 
        /// to persist the referring entity.
        /// </summary>
        [ForeignKey("SeatInfo")]
        public Guid SeatInfoId { get; set; }
        public SeatType SeatInfo { get; set; }
    }
}
