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
    using Common;

    public class SeatsReservationCommitted : IEvent
    {
        private readonly Guid conferenceId;
        private readonly Guid reservationId;

        public SeatsReservationCommitted(Guid conferenceId, Guid reservationId)
        {
            this.conferenceId = conferenceId;
            this.reservationId = reservationId;
        }

        public Guid ConferenceId { get { return this.conferenceId; } }

        public Guid ReservationId { get { return this.reservationId; } }
    }
}