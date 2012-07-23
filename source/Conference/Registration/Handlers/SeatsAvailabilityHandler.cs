// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
// http://go.microsoft.com/fwlink/p/?LinkID=258575
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

namespace Registration.Handlers
{
    using Infrastructure.EventSourcing;
    using Infrastructure.Messaging.Handling;
    using Registration.Commands;

    /// <summary>
    /// Handles commands issued to the seats availability aggregate.
    /// </summary>
    public class SeatsAvailabilityHandler :
        ICommandHandler<MakeSeatReservation>,
        ICommandHandler<CancelSeatReservation>,
        ICommandHandler<CommitSeatReservation>,
        ICommandHandler<AddSeats>,
        ICommandHandler<RemoveSeats>
    {
        private readonly IEventSourcedRepository<SeatsAvailability> repository;

        public SeatsAvailabilityHandler(IEventSourcedRepository<SeatsAvailability> repository)
        {
            this.repository = repository;
        }

        public void Handle(MakeSeatReservation command)
        {
            var availability = this.repository.Get(command.ConferenceId);
            availability.MakeReservation(command.ReservationId, command.Seats);
            this.repository.Save(availability, command.Id.ToString());
        }

        public void Handle(CancelSeatReservation command)
        {
            var availability = this.repository.Get(command.ConferenceId);
            availability.CancelReservation(command.ReservationId);
            this.repository.Save(availability, command.Id.ToString());
        }

        public void Handle(CommitSeatReservation command)
        {
            var availability = this.repository.Get(command.ConferenceId);
            availability.CommitReservation(command.ReservationId);
            this.repository.Save(availability, command.Id.ToString());
        }

        // Commands created from events from the conference BC

        public void Handle(AddSeats command)
        {
            var availability = this.repository.Find(command.ConferenceId);
            if (availability == null)
                availability = new SeatsAvailability(command.ConferenceId);

            availability.AddSeats(command.SeatType, command.Quantity);
            this.repository.Save(availability, command.Id.ToString());
        }

        public void Handle(RemoveSeats command)
        {
            var availability = this.repository.Find(command.ConferenceId);
            if (availability == null)
                availability = new SeatsAvailability(command.ConferenceId);

            availability.RemoveSeats(command.SeatType, command.Quantity);
            this.repository.Save(availability, command.Id.ToString());
        }
    }
}
