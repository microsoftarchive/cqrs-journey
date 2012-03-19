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
        {
            this.Id = id;
            this.PendingReservations = new ObservableCollection<Reservation>();
        }

        // ORM requirement
        protected SeatsAvailability()
        {
            this.PendingReservations = new ObservableCollection<Reservation>();
        }

        public virtual Guid Id { get; private set; }

        public virtual int RemainingSeats { get; private set; }

        public virtual ObservableCollection<Reservation> PendingReservations { get; private set; }

        public IEnumerable<IEvent> Events
        {
            get { return this.events; }
        }

        public void AddSeats(int additionalSeats)
        {
            this.RemainingSeats += additionalSeats;
        }

        public void MakeReservation(Guid reservationId, int numberOfSeats)
        {
            if (numberOfSeats > this.RemainingSeats)
            {
                this.events.Add(new ReservationRejected { ReservationId = reservationId, ConferenceId = this.Id });
            }
            else
            {
                this.PendingReservations.Add(new Reservation(reservationId, numberOfSeats));
                this.RemainingSeats -= numberOfSeats;
                this.events.Add(new ReservationAccepted { ReservationId = reservationId, ConferenceId = this.Id });
            }
        }

        public void CommitReservation(Guid reservationId)
        {
            var reservation = this.PendingReservations.FirstOrDefault(r => r.Id == reservationId);
            if (reservation == null) throw new KeyNotFoundException();

            this.PendingReservations.Remove(reservation);
        }

        public void CancelReservation(Guid reservationId)
        {
            var reservation = this.PendingReservations.FirstOrDefault(r => r.Id == reservationId);
            if (reservation == null) throw new KeyNotFoundException();

            this.PendingReservations.Remove(reservation);
            this.RemainingSeats += reservation.Seats;
        }
    }
}
