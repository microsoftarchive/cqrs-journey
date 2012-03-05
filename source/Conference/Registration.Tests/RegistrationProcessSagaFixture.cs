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

namespace Registration.Tests.RegistrationProcessSagaFixture
{
    using System;
    using System.Linq;
    using Registration.Commands;
    using Xunit;

    public class given_uninitialized_saga
    {
        protected RegistrationProcessSaga sut;

        public given_uninitialized_saga()
        {
            this.sut = new RegistrationProcessSaga();
        }
    }

    public class when_registering : given_uninitialized_saga
    {
        public when_registering()
        {
            var registerCommand = new RegisterToConference { ConferenceId = Guid.NewGuid(), NumberOfSeats = 1 };
            sut.Handle(registerCommand);
        }

        [Fact]
        public void then_locks_seats()
        {
            Assert.Equal(1, sut.Commands.Count());
            Assert.IsAssignableFrom<MakeReservation>(sut.Commands.Single());
        }

        [Fact]
        public void then_transitions_to_awaiting_reservation_confirmation_state()
        {
            Assert.Equal(RegistrationProcessSaga.SagaState.AwaitingReservationConfirmation, sut.State);
        }
    }
}
