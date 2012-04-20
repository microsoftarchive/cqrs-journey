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

namespace Registration.Handlers
{
    using Common;
    using Conference;
    using Registration.Commands;

    /// <summary>
    /// Handles commands issued to the seats availability aggregate.
    /// </summary>
    public class SeatsAvailabilityHandler :
        ICommandHandler<MakeSeatReservation>,
        ICommandHandler<CancelSeatReservation>,
        ICommandHandler<CommitSeatReservation>,
        IEventHandler<SeatsAdded>,
        IEventHandler<SeatsRemoved>
    {
        private readonly IEventSourcedRepository<SeatsAvailability> repository;

        public SeatsAvailabilityHandler(IEventSourcedRepository<SeatsAvailability> repository)
        {
            this.repository = repository;
        }

        public void Handle(MakeSeatReservation command)
        {
            var availability = this.repository.Find(command.ConferenceId);
            if (availability != null)
            {
                availability.MakeReservation(command.ReservationId, command.Seats);
                this.repository.Save(availability);
            }
            // TODO: what if there's no aggregate? how do we tell the process?
        }

        public void Handle(CancelSeatReservation command)
        {
            var availability = this.repository.Find(command.ConferenceId);
            if (availability != null)
            {
                availability.CancelReservation(command.ReservationId);
                this.repository.Save(availability);
            }
            // TODO: what if there's no aggregate? how do we tell the process?
        }

        public void Handle(CommitSeatReservation command)
        {
            var availability = this.repository.Find(command.ConferenceId);
            if (availability != null)
            {
                availability.CommitReservation(command.ReservationId);
                this.repository.Save(availability);
            }
            // TODO: what if there's no aggregate? how do we tell the process?
        }

        // Events from the conference BC

        public void Handle(SeatsAdded @event)
        {
            var availability = this.repository.Find(@event.ConferenceId);
            if (availability == null)
                availability = new SeatsAvailability(@event.ConferenceId);

            availability.AddSeats(@event.SourceId, @event.AddedQuantity);
            this.repository.Save(availability);
        }

        public void Handle(SeatsRemoved @event)
        {
            var availability = this.repository.Find(@event.ConferenceId);
            if (availability == null)
                availability = new SeatsAvailability(@event.ConferenceId);

            availability.RemoveSeats(@event.SourceId, @event.RemovedQuantity);
            this.repository.Save(availability);
        }
    }
}
