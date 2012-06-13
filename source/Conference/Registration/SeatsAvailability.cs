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
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Infrastructure.EventSourcing;
    using Registration.Events;

    /// <summary>
    /// Manages the availability of conference seats.
    /// </summary>
    public class SeatsAvailability : EventSourced, IMementoOriginator
    {
        private readonly Dictionary<Guid, int> remainingSeats = new Dictionary<Guid, int>();
        private readonly Dictionary<Guid, List<SeatQuantity>> pendingReservations = new Dictionary<Guid, List<SeatQuantity>>();

        public SeatsAvailability(Guid id)
            : base(id)
        {
            base.Handles<AvailableSeatsChanged>(this.OnAvailableSeatsChanged);
            base.Handles<SeatsReserved>(this.OnSeatsReserved);
            base.Handles<SeatsReservationCommitted>(this.OnSeatsReservationCommitted);
            base.Handles<SeatsReservationCancelled>(this.OnSeatsReservationCancelled);
        }

        public SeatsAvailability(Guid id, IEnumerable<IVersionedEvent> history)
            : this(id)
        {
            this.LoadFrom(history);
        }

        public SeatsAvailability(Guid id, IMemento memento, IEnumerable<IVersionedEvent> history)
            : this(id)
        {
            var state = (Memento)memento;
            this.Version = state.Version;
            // make a copy of the state values to avoid concurrency problems with reusing references.
            this.remainingSeats.AddRange(state.RemainingSeats);
            this.pendingReservations.AddRange(state.PendingReservations);
            this.LoadFrom(history);
        }

        public void AddSeats(Guid seatType, int quantity)
        {
            base.Update(new AvailableSeatsChanged { Seats = new[] { new SeatQuantity(seatType, quantity) } });
        }

        public void RemoveSeats(Guid seatType, int quantity)
        {
            base.Update(new AvailableSeatsChanged { Seats = new[] { new SeatQuantity(seatType, -quantity) } });
        }

        /// <summary>
        /// Requests a reservation for seats.
        /// </summary>
        /// <param name="reservationId">A token identifying the reservation request.</param>
        /// <param name="wantedSeats">The list of seat requirements.</param>
        /// <remarks>The reservation id is not the id for an aggregate root, just an opaque identifier.</remarks>
        public void MakeReservation(Guid reservationId, IEnumerable<SeatQuantity> wantedSeats)
        {
            var wantedList = wantedSeats.ToList();
            if (wantedList.Any(x => !this.remainingSeats.ContainsKey(x.SeatType)))
            {
                throw new ArgumentOutOfRangeException("wantedSeats");
            }

            var difference = new Dictionary<Guid, SeatDifference>();

            foreach (var w in wantedList)
            {
                var item = GetOrAdd(difference, w.SeatType);
                item.Wanted = w.Quantity;
                item.Remaining = this.remainingSeats[w.SeatType];
            }

            List<SeatQuantity> existing;
            if (this.pendingReservations.TryGetValue(reservationId, out existing))
            {
                foreach (var e in existing)
                {
                    GetOrAdd(difference, e.SeatType).Existing = e.Quantity;
                }
            }

            var reservation = new SeatsReserved
            {
                ReservationId = reservationId,
                ReservationDetails = difference.Select(x => new SeatQuantity(x.Key, x.Value.Actual)).Where(x => x.Quantity != 0).ToList(),
                AvailableSeatsChanged = difference.Select(x => new SeatQuantity(x.Key, -x.Value.DeltaSinceLast)).Where(x => x.Quantity != 0).ToList()
            };

            base.Update(reservation);
        }

        public void CancelReservation(Guid reservationId)
        {
            List<SeatQuantity> reservation;
            if (this.pendingReservations.TryGetValue(reservationId, out reservation))
            {
                base.Update(new SeatsReservationCancelled
                {
                    ReservationId = reservationId,
                    AvailableSeatsChanged = reservation.Select(x => new SeatQuantity(x.SeatType, x.Quantity)).ToList()
                });
            }
        }

        public void CommitReservation(Guid reservationId)
        {
            if (this.pendingReservations.ContainsKey(reservationId))
            {
                base.Update(new SeatsReservationCommitted { ReservationId = reservationId });
            }
        }

        private class SeatDifference
        {
            public int Wanted { get; set; }
            public int Existing { get; set; }
            public int Remaining { get; set; }
            public int Actual
            {
                get { return Math.Min(this.Wanted, Math.Max(this.Remaining, 0) + this.Existing); }
            }
            public int DeltaSinceLast
            {
                get { return this.Actual - this.Existing; }
            }
        }

        private static TValue GetOrAdd<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key) where TValue : new()
        {
            TValue value;
            if (!dictionary.TryGetValue(key, out value))
            {
                value = new TValue();
                dictionary[key] = value;
            }

            return value;
        }

        private void OnAvailableSeatsChanged(AvailableSeatsChanged e)
        {
            foreach (var seat in e.Seats)
            {
                int newValue = seat.Quantity;
                int remaining;
                if (this.remainingSeats.TryGetValue(seat.SeatType, out remaining))
                {
                    newValue += remaining;
                }

                this.remainingSeats[seat.SeatType] = newValue;
            }
        }

        private void OnSeatsReserved(SeatsReserved e)
        {
            var details = e.ReservationDetails.ToList();
            if (details.Count > 0)
            {
                this.pendingReservations[e.ReservationId] = details;
            }
            else
            {
                this.pendingReservations.Remove(e.ReservationId);
            }

            foreach (var seat in e.AvailableSeatsChanged)
            {
                this.remainingSeats[seat.SeatType] = this.remainingSeats[seat.SeatType] + seat.Quantity;
            }
        }

        private void OnSeatsReservationCommitted(SeatsReservationCommitted e)
        {
            this.pendingReservations.Remove(e.ReservationId);
        }

        private void OnSeatsReservationCancelled(SeatsReservationCancelled e)
        {
            this.pendingReservations.Remove(e.ReservationId);

            foreach (var seat in e.AvailableSeatsChanged)
            {
                this.remainingSeats[seat.SeatType] = this.remainingSeats[seat.SeatType] + seat.Quantity;
            }
        }

        public IMemento SaveToMemento()
        {
            return new Memento
            {
                Version = this.Version,
                RemainingSeats = this.remainingSeats.ToArray(),
                PendingReservations = this.pendingReservations.ToArray(),
            };
        }

        public class Memento : IMemento
        {
            public int Version { get; internal set; }
            internal KeyValuePair<Guid, int>[] RemainingSeats { get; set; }
            internal KeyValuePair<Guid, List<SeatQuantity>>[] PendingReservations { get; set; }
        }
    }
}
