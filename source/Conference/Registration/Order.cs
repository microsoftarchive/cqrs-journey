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
    using System.Collections.ObjectModel;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using Common;
    using Common.Utils;
    using Registration.Events;

    public class Order : EventSourcedAggregateRoot
    {
        private static readonly TimeSpan ReservationAutoExpiration = TimeSpan.FromMinutes(15);

        public Order()
        {
        }

        public Order(Guid id, Guid conferenceId, IEnumerable<OrderItem> items)
        {
            this.Update(new OrderPlaced
                                {
                                    OrderId = id,
                                    ConferenceId = conferenceId,
                                    AccessCode = HandleGenerator.Generate(6),
                                    ReservationAutoExpiration = DateTime.UtcNow.Add(ReservationAutoExpiration),
                                    Seats = ConvertItems(items)
                                });
        }

        private Guid id;
        private List<SeatQuantity> seats;
        private bool isConfirmed;

        public override Guid Id
        {
            get { return this.id; }
        }

        public void Apply(OrderPlaced e)
        {
            this.id = e.OrderId;
            this.seats = e.Seats.ToList();
        }

        public void UpdateSeats(IEnumerable<OrderItem> seats)
        {
            this.Update(
                new OrderUpdated
                {
                    OrderId = this.id,
                    Seats = ConvertItems(seats)
                });
        }

        public void Apply(OrderUpdated e)
        {
            this.seats = e.Seats.ToList();
        }

        public void MarkAsReserved(DateTime expirationDate, IEnumerable<SeatQuantity> seats)
        {
            if (this.isConfirmed)
                throw new InvalidOperationException("Cannot modify a confirmed order.");

            // Is there an order item which didn't get an exact reservation?
            if (this.seats.Any(item => !seats.Any(seat => seat.SeatType == item.SeatType && seat.Quantity == item.Quantity)))
            {
                this.Update(new OrderPartiallyReserved
                {
                    OrderId = this.id,
                    //ConferenceId = this.ConferenceId,
                    Seats = seats.ToList(),
                    //ReservationExpiration = expirationDate,
                });
            }
            else
            {
                this.Update(new OrderReservationCompleted
                {
                    OrderId = this.id,
                    //ConferenceId = this.ConferenceId,
                    Seats = seats.ToList(),
                    //ReservationExpiration = expirationDate,
                });
            }
        }

        public void Apply(OrderPartiallyReserved e)
        {
            this.seats = e.Seats.ToList();
        }

        public void Apply(OrderReservationCompleted e)
        {
            this.seats = e.Seats.ToList();
        }

        public void Expire()
        {
            if (this.isConfirmed)
                throw new InvalidOperationException();

            this.Update(new OrderExpired(this.id));
        }

        public void Apply(OrderExpired e)
        {
        }

        public void ConfirmPayment()
        {
            this.Update(new OrderPaymentConfirmed(this.id));
        }

        public void Apply(OrderPaymentConfirmed e)
        {
            this.isConfirmed = true;
        }

        public void AssignRegistrant(string firstName, string lastName, string email)
        {
            this.Update(new OrderRegistrantAssigned
            {
                OrderId = this.id,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
            });
        }

        public void Apply(OrderRegistrantAssigned e)
        {
        }

        private static List<SeatQuantity> ConvertItems(IEnumerable<OrderItem> items)
        {
            return items.Select(x => new SeatQuantity { SeatType = x.SeatType, Quantity = x.Quantity }).ToList();
        }
    }
}
