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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common;
    using Common.Utils;
    using Registration.Events;

    public class Order : EventSourcedAggregateRoot
    {
        private static readonly TimeSpan ReservationAutoExpiration = TimeSpan.FromMinutes(15);

        private Guid id;
        private List<SeatQuantity> seats;
        private bool isConfirmed;

        protected Order()
        {
            base.Handles<OrderPlaced>(this.OnOrderPlaced);
            base.Handles<OrderUpdated>(this.OnOrderUpdated);
            base.Handles<OrderPartiallyReserved>(this.OnOrderPartiallyReserved);
            base.Handles<OrderReservationCompleted>(this.OnOrderReservationCompleted);
            base.Handles<OrderExpired>(this.OnOrderExpired);
            base.Handles<OrderPaymentConfirmed>(this.OnOrderPaymentConfirmed);
            base.Handles<OrderRegistrantAssigned>(this.OnOrderRegistrantAssigned);
        }

        public Order(IEnumerable<IDomainEvent> history) : this()
        {
            this.Rehydrate(history);
        }

        public Order(Guid id, Guid conferenceId, IEnumerable<OrderItem> items)  : this()
        {
            this.Update(new OrderPlaced(id, 0, conferenceId, ConvertItems(items), DateTime.UtcNow.Add(ReservationAutoExpiration), HandleGenerator.Generate(6)));
        }

        public override Guid Id
        {
            get { return this.id; }
        }

        public void UpdateSeats(IEnumerable<OrderItem> seats)
        {
            this.Update(new OrderUpdated(this.id, this.Version + 1, ConvertItems(seats)));
        }

        public void MarkAsReserved(DateTime expirationDate, IEnumerable<SeatQuantity> reservedSeats)
        {
            if (this.isConfirmed)
                throw new InvalidOperationException("Cannot modify a confirmed order.");

            var reserved = reservedSeats.ToList();

            // Is there an order item which didn't get an exact reservation?
            if (this.seats.Any(item => !reserved.Any(seat => seat.SeatType == item.SeatType && seat.Quantity == item.Quantity)))
            {
                this.Update(new OrderPartiallyReserved(this.id, this.Version + 1, expirationDate, reserved));
            }
            else
            {
                this.Update(new OrderReservationCompleted(this.id, this.Version + 1, expirationDate, reserved));
            }
        }

        public void Expire()
        {
            if (this.isConfirmed)
                throw new InvalidOperationException();

            this.Update(new OrderExpired(this.id, this.Version + 1));
        }

        public void ConfirmPayment()
        {
            this.Update(new OrderPaymentConfirmed(this.id, this.Version + 1));
        }

        public void AssignRegistrant(string firstName, string lastName, string email)
        {
            this.Update(new OrderRegistrantAssigned
            {
                SourceId = this.id,
                Version = this.Version + 1,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
            });
        }

        private void OnOrderPlaced(OrderPlaced e)
        {

            this.id = e.SourceId;
            this.seats = e.Seats.ToList();
        }

        private void OnOrderUpdated(OrderUpdated e)
        {
            this.seats = e.Seats.ToList();
        }
        
        private void OnOrderPartiallyReserved(OrderPartiallyReserved e)
        {
            this.seats = e.Seats.ToList();
        }

        private void OnOrderReservationCompleted(OrderReservationCompleted e)
        {
            this.seats = e.Seats.ToList();
        }

        private void OnOrderExpired(OrderExpired e)
        {
        }

        private void OnOrderPaymentConfirmed(OrderPaymentConfirmed e)
        {
            this.isConfirmed = true;
        }

        private void OnOrderRegistrantAssigned(OrderRegistrantAssigned e)
        {
        }

        private static List<SeatQuantity> ConvertItems(IEnumerable<OrderItem> items)
        {
            return items.Select(x => new SeatQuantity(x.SeatType, x.Quantity)).ToList();
        }
    }
}
