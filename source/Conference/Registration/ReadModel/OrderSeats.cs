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

namespace Registration.ReadModel
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;

    /// <summary>
    /// Represents the read model for the set of individual 
    /// seats purchased in an order, which can be assigned 
    /// to attendees.
    /// </summary>
    public class OrderSeats
    {
        public OrderSeats()
        {
            this.Seats = new List<OrderSeat>();
        }

        public OrderSeats(Guid assignmentsId, Guid orderId, IEnumerable<OrderSeat> seats)
        {
            this.AssignmentsId = assignmentsId;
            this.OrderId = orderId;
            this.Seats = seats.ToList();
        }

        /// <summary>
        /// Gets or sets the seat assignments AR identifier.
        /// </summary>
        [Key]
        public Guid AssignmentsId { get; set; }

        /// <summary>
        /// Gets or sets the order id.
        /// </summary>
        public Guid OrderId { get; set; }
        public IList<OrderSeat> Seats { get; set; }
    }
}
