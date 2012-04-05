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
    using Registration.Events;

    public class Order : IAggregateRoot, IEventPublisher
    {
        public enum States
        {
            Created = 0,
            PartiallyReserved = 1,
            ReservationCompleted = 2,
            Rejected = 3,
            Confirmed = 4,
        }

        private List<IEvent> events = new List<IEvent>();

        protected Order()
        {
        }

        public Order(Guid id, Guid conferenceId, IEnumerable<OrderItem> items)
        {
            this.Id = id;
            this.ConferenceId = conferenceId;
            this.Registrant = new Registrant();
            this.AccessCode = HandleGenerator.Generate(5);
            this.Items = new ObservableCollection<OrderItem>(items);

            this.events.Add(
                new OrderPlaced
                {
                    OrderId = this.Id,
                    ConferenceId = this.ConferenceId,
                    AccessCode = this.AccessCode,
                    Seats = this.Items.Select(x => new SeatQuantity { SeatType = x.SeatType, Quantity = x.Quantity }).ToList()
                });
        }

        public IEnumerable<IEvent> Events
        {
            get { return this.events; }
        }

        public Guid Id { get; private set; }

        public Guid ConferenceId { get; private set; }

        public virtual ObservableCollection<OrderItem> Items { get; private set; }

        public Registrant Registrant { get; private set; }

        /// <summary>
        /// Access code, combined with the registrant email can 
        /// be used to find the order and provide low chances of 
        /// collision as it's also scoped to the conference.
        /// </summary>
        public string AccessCode { get; set; }

        public int StateValue { get; private set; }
        [NotMapped]
        public States State
        {
            get { return (States)this.StateValue; }
            internal set { this.StateValue = (int)value; }
        }

        public DateTime? ReservationExpirationDate { get; private set; }

        public void UpdateSeats(IEnumerable<OrderItem> seats)
        {
            this.Items.Clear();
            this.Items.AddRange(seats);

            this.events.Add(
                new OrderUpdated
                {
                    OrderId = this.Id,
                    Seats = this.Items.Select(x => new SeatQuantity { SeatType = x.SeatType, Quantity = x.Quantity }).ToArray()
                });
        }

        public void MarkAsReserved(DateTime expirationDate, IEnumerable<SeatQuantity> seats)
        {
            if (this.State == States.Confirmed || this.State == States.Rejected)
                throw new InvalidOperationException("Cannot modify confirmed or cancelled order.");

            // Is there an order item which didn't get an exact reservation?
            if (this.Items.Any(item => !seats.Any(seat => seat.SeatType == item.SeatType && seat.Quantity == item.Quantity)))
            {
                this.State = States.PartiallyReserved;
                this.events.Add(new OrderPartiallyReserved
                {
                    OrderId = this.Id,
                    ConferenceId = this.ConferenceId,
                    Seats = seats.ToList(),
                    ReservationExpiration = expirationDate,
                });
            }
            else
            {
                this.State = States.ReservationCompleted;
                this.events.Add(new OrderReservationCompleted
                {
                    OrderId = this.Id,
                    ConferenceId = this.ConferenceId,
                    Seats = seats.ToList(),
                    ReservationExpiration = expirationDate,
                });
            }

            this.ReservationExpirationDate = expirationDate;
            this.Items.Clear();
            this.Items.AddRange(seats.Select(seat => new OrderItem(seat.SeatType, seat.Quantity)));
        }

        public void Reject()
        {
            // TODO: when do we "reject" order? Is it "Cancel" or something else?
            //if (this.State != States.AwaitingReservation && this.State != States.Booked)
            //    throw new InvalidOperationException();

            this.State = States.Rejected;
            //this.BookingExpirationDate = null;
        }

        public void AssignRegistrant(string firstName, string lastName, string email)
        {
            this.Registrant = new Registrant
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
            };

            this.events.Add(new OrderRegistrantAssigned
            {
                OrderId = this.Id,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
            });
        }
    }
}
