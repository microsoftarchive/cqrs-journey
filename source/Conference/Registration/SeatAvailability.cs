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
    using System.Collections.ObjectModel;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;

    /// <summary>
    /// Tracks the availability and in-flight reservations for a 
    /// specific type of seat within a conference.
    /// </summary>
    public class SeatAvailability
    {
        public SeatAvailability(Guid seatType, int quantity)
            : this()
        {
            this.SeatType = seatType;
            this.RemainingSeats = quantity;
        }

        // ORM requirement
        protected SeatAvailability()
        {
            this.PendingReservations = new ObservableCollection<Reservation>();
        }

        [Key]
        public Guid SeatType { get; private set; }
        public virtual int RemainingSeats { get; set; }
        public virtual Collection<Reservation> PendingReservations { get; private set; }

        internal int Reserve(Guid reservationId, int quantity)
        {
            var existing = this.PendingReservations.FirstOrDefault(x => x.Id == reservationId);
            if (existing == null)
            {
                if (quantity > this.RemainingSeats)
                {
                    quantity = this.RemainingSeats;
                }

                if (quantity > 0)
                {
                    this.PendingReservations.Add(new Reservation(reservationId, quantity));
                    this.RemainingSeats -= quantity;
                }
            }
            else
            {
                var relativeQuantity = quantity - existing.Quantity;
                if (relativeQuantity > this.RemainingSeats)
                {
                    relativeQuantity = this.RemainingSeats;
                    quantity  = existing.Quantity + relativeQuantity;
                }

                existing.Quantity = quantity;
                // We might be substracting a negative here, i.e. 
                // we request 3, had 5 existing, we're substracting -2
                // that is, adding the 2 we dropped.
                this.RemainingSeats -= relativeQuantity;
                if (quantity == 0)
                {
                    this.PendingReservations.Remove(existing);
                }
            }

            return quantity;
        }

        internal void CommitReservation(Guid reservationId)
        {
            var pending = this.PendingReservations.FirstOrDefault(x => x.Id == reservationId);
            if (pending != null)
            {
                this.PendingReservations.Remove(pending);
            }
        }

        internal void CancelReservation(Guid reservationId)
        {
            var pending = this.PendingReservations.FirstOrDefault(x => x.Id == reservationId);
            if (pending != null)
            {
                this.RemainingSeats += pending.Quantity;
                this.PendingReservations.Remove(pending);
            }
        }

        internal void AddSeats(int quantity)
        {
            this.RemainingSeats += quantity;
        }
    }
}
