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
    using System.Linq;
    using Common;
    using Registration.Events;

    /// <summary>
    /// Manages the availability of conference seats.
    /// </summary>
    public class SeatsAvailability : IAggregateRoot, IEventPublisher
    {
        private List<IEvent> events = new List<IEvent>();

        public SeatsAvailability(Guid id)
            : this()
        {
            this.Id = id;
        }

        // ORM requirement
        protected SeatsAvailability()
        {
            this.Seats = new ObservableCollection<SeatAvailability>();
        }

        public Guid Id { get; private set; }

        public virtual Collection<SeatAvailability> Seats { get; private set; }

        public IEnumerable<IEvent> Events
        {
            get { return this.events; }
        }

        public void AddSeats(Guid seatType, int quantity)
        {
            var availability = this.Seats.FirstOrDefault(x => x.SeatType == seatType);
            if (availability == null)
            {
                availability = new SeatAvailability(seatType, quantity);
                this.Seats.Add(availability);
            }
            else
            {
                availability.RemainingSeats += quantity;
            }
        }

        public void MakeReservation(Guid reservationId, IEnumerable<SeatQuantity> seats)
        {
            var reserved = new SeatsReserved { ReservationId = reservationId };
            foreach (var seat in seats)
            {
                var availability = this.Seats.FirstOrDefault(x => x.SeatType == seat.SeatType);
                if (availability == null)
                {
                    throw new ArgumentOutOfRangeException("seats");
                }

                var quantity = 0;
                var existing = availability.PendingReservations.FirstOrDefault(x => x.Id == reservationId);
                if (existing == null)
                {
                    quantity = availability.RemainingSeats >= seat.Quantity ? seat.Quantity : availability.RemainingSeats;

                    if (quantity > 0)
                    {
                        availability.PendingReservations.Add(new Reservation(reservationId, quantity));
                        availability.RemainingSeats -= quantity;
                    }
                }
                else
                {
                    var relativeQuantity = seat.Quantity - existing.Quantity;
                    if (relativeQuantity > availability.RemainingSeats)
                    {
                        relativeQuantity = availability.RemainingSeats;
                    }

                    existing.Quantity += relativeQuantity;
                    quantity = existing.Quantity;
                    // We might be substracting a negative here, i.e. 
                    // we request 3, had 5 existing, we're substracting -2
                    // that is, adding the 2 we dropped.
                    availability.RemainingSeats -= relativeQuantity;
                    if (quantity == 0)
                    {
                        availability.PendingReservations.Remove(existing);
                    }
                }

                reserved.Seats.Add(new SeatQuantity(seat.SeatType, quantity));
            }

            this.events.Add(reserved);
        }

        public void CommitReservation(Guid reservationId)
        {
            foreach (var reservation in this.Seats
                .Select(x => new
                {
                    Collection = x.PendingReservations,
                    Item = x.PendingReservations.FirstOrDefault(r => r.Id == reservationId)
                })
                .Where(x => x.Item != null))
            {
                reservation.Collection.Remove(reservation.Item);
            }
        }

        public void CancelReservation(Guid reservationId)
        {
            foreach (var entry in this.Seats
                .Select(x => new
                {
                    Availability = x,
                    Reservation = x.PendingReservations.FirstOrDefault(r => r.Id == reservationId)
                })
                .Where(x => x.Reservation != null))
            {
                entry.Availability.PendingReservations.Remove(entry.Reservation);
                entry.Availability.RemainingSeats += entry.Reservation.Quantity;
            }
        }
    }
}
