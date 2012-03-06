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

namespace Registration.Handlers
{
    using System;
    using Common;
    using Registration.Commands;

    /// <summary>
    /// Handles commands issued to the seats availability aggregate.
    /// </summary>
    public class ConferenceSeatsAvailabilityHandler :
        ICommandHandler<MakeSeatReservation>,
        ICommandHandler<ExpireSeatReservation>,
        ICommandHandler<CommitSeatReservation>
    {
        private Func<IRepository> repositoryFactory;

        public ConferenceSeatsAvailabilityHandler(Func<IRepository> repositoryFactory)
        {
            this.repositoryFactory = repositoryFactory;
        }

        public void Handle(MakeSeatReservation command)
        {
            var repo = this.repositoryFactory();
            using (repo as IDisposable)
            {
                var availability = repo.Find<ConferenceSeatsAvailability>(command.ConferenceId);
                if (availability != null)
                {
                    availability.MakeReservation(command.ReservationId, command.NumberOfSeats);
                    repo.Save(availability);
                }
                // TODO: what if there's no aggregate? how do we tell the saga?
            }
        }

        public void Handle(ExpireSeatReservation command)
        {
            var repo = this.repositoryFactory();
            using (repo as IDisposable)
            {
                var availability = repo.Find<ConferenceSeatsAvailability>(command.ConferenceId);
                if (availability != null)
                {
                    availability.ExpireReservation(command.ReservationId);
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
                var availability = repo.Find<ConferenceSeatsAvailability>(command.ConferenceId);
                if (availability != null)
                {
                    availability.ExpireReservation(command.ReservationId);
                    repo.Save(availability);
                }
                // TODO: what if there's no aggregate? how do we tell the saga?
            }
        }
    }
}
