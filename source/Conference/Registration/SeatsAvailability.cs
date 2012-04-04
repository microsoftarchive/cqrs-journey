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
                availability.AddSeats(quantity);
            }
        }

        public void MakeReservation(Guid reservationId, IEnumerable<SeatQuantity> seats)
        {
            if (seats.Any(x => !this.Seats.Any(availability => x.SeatType == availability.SeatType)))
            {
                    throw new ArgumentOutOfRangeException("seats");
            }

            var reserved = new SeatsReserved { ReservationId = reservationId };
            foreach (var availability in this.Seats)
            {
                var seat = seats.FirstOrDefault(x => x.SeatType == availability.SeatType);
                if (seat != null)
                {
                    var actualReserved = availability.Reserve(reservationId, seat.Quantity);
                    reserved.Seats.Add(new SeatQuantity(seat.SeatType, actualReserved));
                }
                else
                {
                    availability.Reserve(reservationId, 0);
                }
            }

            this.events.Add(reserved);
        }

        public void CommitReservation(Guid reservationId)
        {
            foreach (var availability in this.Seats)
            {
                availability.CommitReservation(reservationId);
            }
        }

        public void CancelReservation(Guid reservationId)
        {
            foreach (var availability in this.Seats)
            {
                availability.CancelReservation(reservationId);
            }
        }
    }
}
