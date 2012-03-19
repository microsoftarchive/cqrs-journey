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
    using System;
    using Common;
    using Registration.Commands;

    /// <summary>
    /// Handles commands issued to the seats availability aggregate.
    /// </summary>
    public class SeatsAvailabilityHandler :
        ICommandHandler<MakeSeatReservation>,
        ICommandHandler<CancelSeatReservation>,
        ICommandHandler<CommitSeatReservation>
    {
        private Func<IRepository> repositoryFactory;

        public SeatsAvailabilityHandler(Func<IRepository> repositoryFactory)
        {
            this.repositoryFactory = repositoryFactory;
        }

        public void Handle(MakeSeatReservation command)
        {
            var repo = this.repositoryFactory();
            using (repo as IDisposable)
            {
                var availability = repo.Find<SeatsAvailability>(command.ConferenceId);
                if (availability != null)
                {
                    availability.MakeReservation(command.ReservationId, command.NumberOfSeats);
                    repo.Save(availability);
                }
                // TODO: what if there's no aggregate? how do we tell the saga?
            }
        }

        public void Handle(CancelSeatReservation command)
        {
            var repo = this.repositoryFactory();
            using (repo as IDisposable)
            {
                var availability = repo.Find<SeatsAvailability>(command.ConferenceId);
                if (availability != null)
                {
                    availability.CancelReservation(command.ReservationId);
                    repo.Save(availability);
                }
                // TODO: what if there's no aggregate? how do we tell the saga?
            }
        }

        public void Handle(CommitSeatReservation command)
        {
            var repo = this.repositoryFactory();
            using (repo as IDisposable)
            {
                var availability = repo.Find<SeatsAvailability>(command.ConferenceId);
                if (availability != null)
                {
                    availability.CommitReservation(command.ReservationId);
                    repo.Save(availability);
                }
                // TODO: what if there's no aggregate? how do we tell the saga?
            }
        }
    }
}
