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

namespace Registration.Events
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Common;

    public class OrderPlaced : IDomainEvent
    {
        private readonly Guid sourceId;
        private readonly int version;
        private readonly Guid conferenceId;
        private readonly IEnumerable<SeatQuantity> seats;
        private readonly DateTime reservationAutoExpiration;
        private readonly string accessCode;

        public OrderPlaced(Guid sourceId, int version, Guid conferenceId, IEnumerable<SeatQuantity> seats, DateTime reservationAutoExpiration, string accessCode)
        {
            this.sourceId = sourceId;
            this.version = version;
            this.conferenceId = conferenceId;
            this.reservationAutoExpiration = reservationAutoExpiration;
            this.accessCode = accessCode;
            this.seats = seats.ToArray();
        }

        public Guid SourceId { get { return this.sourceId; } }

        public int Version { get { return this.version; } }

        public Guid ConferenceId { get { return this.conferenceId; } }

        public IEnumerable<SeatQuantity> Seats { get { return this.seats; } }

        /// <summary>
        /// The expected expiration time if the reservation is not explicitly confirmed later.
        /// </summary>
        public DateTime ReservationAutoExpiration { get { return this.reservationAutoExpiration; } }

        public string AccessCode { get { return this.accessCode; } }
    }
}
