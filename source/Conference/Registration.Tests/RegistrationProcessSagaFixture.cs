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
    using Registration.Events;
    using Xunit;

    public class given_uninitialized_saga
    {
        protected RegistrationProcessSaga sut;

        public given_uninitialized_saga()
        {
            this.sut = new RegistrationProcessSaga();
        }
    }

    public class when_order_is_placed : given_uninitialized_saga
    {
        private OrderPlaced orderPlaced;

        public when_order_is_placed()
        {
            this.orderPlaced = new OrderPlaced
            {
                OrderId = Guid.NewGuid(),
                ConferenceId = Guid.NewGuid(),
                Tickets = new[] { new OrderPlaced.Ticket { TicketTypeId = "testSeat", Quantity = 2 } }
            };
            sut.Handle(orderPlaced);
        }

        [Fact]
        public void then_locks_seats()
        {
            Assert.Equal(1, sut.Commands.Count());
            Assert.IsAssignableFrom<MakeReservation>(sut.Commands.Single());
        }

        [Fact]
        public void then_reservation_is_requested_for_specific_conference()
        {
            var reservation = (MakeReservation)sut.Commands.Single();

            Assert.Equal(orderPlaced.ConferenceId, reservation.ConferenceId);
            Assert.Equal(2, reservation.AmountOfSeats);
        }

        [Fact]
        public void then_reservation_is_correlated_with_order_id()
        {
            var reservation = (MakeReservation)sut.Commands.Single();

            Assert.Equal(orderPlaced.OrderId, reservation.Id);
        }

        [Fact]
        public void then_saga_is_correlated_with_order_id()
        {
            var reservation = (MakeReservation)sut.Commands.Single();

            Assert.Equal(orderPlaced.OrderId, sut.Id);
        }

        [Fact]
        public void then_transitions_to_awaiting_reservation_confirmation_state()
        {
            Assert.Equal(RegistrationProcessSaga.SagaState.AwaitingReservationConfirmation, sut.State);
        }
    }

    public class given_saga_awaiting_for_reservation_confirmation
    {
        protected RegistrationProcessSaga sut;

        public given_saga_awaiting_for_reservation_confirmation()
        {
            this.sut = new RegistrationProcessSaga
            {
                Id = Guid.NewGuid(),
                State = RegistrationProcessSaga.SagaState.AwaitingReservationConfirmation,
            };
        }
    }

    public class when_reservation_confirmation_is_received : given_saga_awaiting_for_reservation_confirmation
    {
        public when_reservation_confirmation_is_received()
        {
            var reservationAccepted = new ReservationAccepted
            {
                ReservationId = sut.Id,
                ConferenceId = Guid.NewGuid(),
            };
            sut.Handle(reservationAccepted);
        }

        [Fact]
        public void then_updates_order_status()
        {
            var command = (MarkOrderAsBooked)sut.Commands.Single();

            Assert.Equal(sut.Id, command.OrderId);
        }

        [Fact]
        public void then_transitions_state()
        {
            Assert.Equal(RegistrationProcessSaga.SagaState.AwaitingPayment, sut.State);
        }
    }

    public class when_reservation_is_rejected : given_saga_awaiting_for_reservation_confirmation
    {
        public when_reservation_is_rejected()
        {
            var reservationAccepted = new ReservationRejected
            {
                ReservationId = sut.Id,
                ConferenceId = Guid.NewGuid(),
            };
            sut.Handle(reservationAccepted);
        }

        [Fact]
        public void then_updates_order_status()
        {
            var command = (RejectOrder)sut.Commands.Single();

            Assert.Equal(sut.Id, command.OrderId);
        }

        [Fact]
        public void then_transitions_state()
        {
            Assert.Equal(RegistrationProcessSaga.SagaState.Completed, sut.State);
        }
    }
}
