// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// Copyright (c) Microsoft Corporation and contributors http://cqrsjourney.github.com/contributors/members
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

    public class SeatReservationManager
    {
        public SeatReservationManager(Guid id)
        {
            this.Id = id;
            this.PendingReservations = new Dictionary<Guid, int>();
        }

        internal Guid Id { get; set; }

        internal int RemainingSeats { get; set; }

        internal Dictionary<Guid, int> PendingReservations { get; set; }

        public void AddSeats(int additionalSeats)
        {
            this.RemainingSeats += additionalSeats;
        }

        public void MakeReservation(Guid reservationId, int numberOfSeats)
        {
            if (numberOfSeats > this.RemainingSeats)
            {
                throw new ArgumentOutOfRangeException("numberOfSeats");
            }

            this.PendingReservations.Add(reservationId, numberOfSeats);
            this.RemainingSeats -= numberOfSeats;
        }

        public void CommitReservation(Guid reservationId)
        {
            
        }

        public void ExpireReservation(Guid reservationId)
        {
            var numberOfSeats = this.PendingReservations[reservationId];
            this.PendingReservations.Remove(reservationId);
            this.RemainingSeats += numberOfSeats;
        }
    }
}
