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

    public class OrderPartiallyReserved : IDomainEvent
    {
        private readonly Guid sourceId;
        private readonly int version;
        private readonly DateTime reservationExpiration;
        private readonly IEnumerable<SeatQuantity> seats;

        public OrderPartiallyReserved(Guid sourceId, int version, DateTime reservationExpiration, IEnumerable<SeatQuantity> seats)
        {
            this.sourceId = sourceId;
            this.version = version;
            this.reservationExpiration = reservationExpiration;
            this.seats = seats.ToArray();
        }

        public Guid SourceId { get { return this.sourceId; } }

        public int Version { get { return this.version; } }

        public DateTime ReservationExpiration { get { return this.reservationExpiration; } }

        public IEnumerable<SeatQuantity> Seats { get { return this.seats; } }
    }
}
