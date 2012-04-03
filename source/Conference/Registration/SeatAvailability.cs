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
    }
}
