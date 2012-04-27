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

namespace SeatAssignment
{
    using System;
    using System.Data;
    using System.Data.Entity;
    using System.Linq;

    public class OrderSeatsDao : DbContext, IOrderSeatsDao
    {
        public OrderSeatsDao()
            : this("SeatAssignments")
        {
        }

        public OrderSeatsDao(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
        }

        public OrderSeats FindOrder(Guid orderId, bool confirmedOnly)
        {
            var seats = this.Assignments.Include(x => x.Seats);
            if (confirmedOnly)
                seats = seats.Where(x => x.IsOrderConfirmed);

            return seats.FirstOrDefault(x => x.OrderId == orderId);
        }

        public Seat FindSeat(Guid seatId)
        {
            return this.Seats.Find(seatId);
        }

        public void SaveOrder(OrderSeats order)
        {
            if (this.Entry(order).State == EntityState.Detached)
                this.Assignments.Add(order);

            this.SaveChanges();
        }

        public void UpdateSeat(Seat seat)
        {
            var saved = this.FindSeat(seat.Id);
            if (saved == null)
                throw new ArgumentException("Seat does not exist.");

            this.Entry(saved).CurrentValues.SetValues(seat);

            this.SaveChanges();
        }

        public virtual DbSet<OrderSeats> Assignments { get; set; }
        public virtual DbSet<Seat> Seats { get; set; }
    }
}
