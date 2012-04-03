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
                Seats = { new SeatQuantity { SeatType = Guid.NewGuid(), Quantity = 2 } }
            };
            sut.Handle(orderPlaced);
        }

        [Fact]
        public void then_locks_seats()
        {
            Assert.Equal(1, sut.Commands.Count());
            Assert.IsAssignableFrom<MakeSeatReservation>(sut.Commands.Select(x => x.Body).Single());
        }

        [Fact]
        public void then_reservation_is_requested_for_specific_conference()
        {
            var reservation = (MakeSeatReservation)sut.Commands.Select(x => x.Body).Single();

            Assert.Equal(orderPlaced.ConferenceId, reservation.ConferenceId);
            Assert.Equal(2, reservation.Seats[0].Quantity);
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
        protected Guid orderId;
        protected Guid conferenceId;

        public given_saga_awaiting_for_reservation_confirmation()
        {
            this.sut = new RegistrationProcessSaga();
            this.orderId = Guid.NewGuid();
            this.conferenceId = Guid.NewGuid();

            this.sut.Handle(new OrderPlaced
            {
                OrderId = this.orderId,
                ConferenceId = this.conferenceId,
                Seats = { new SeatQuantity { SeatType = Guid.NewGuid(), Quantity = 2 } }
            });
        }
    }

    public class when_reservation_confirmation_is_received : given_saga_awaiting_for_reservation_confirmation
    {
        private Guid reservationId;

        public when_reservation_confirmation_is_received()
        {
            var makeReservationCommand = sut.Commands.Select(e => e.Body).OfType<MakeSeatReservation>().Single();
            this.reservationId = makeReservationCommand.ReservationId;

            var seatsReserved = new SeatsReserved
            {
                ReservationId = makeReservationCommand.ReservationId,
                // Seats ?
            };
            sut.Handle(seatsReserved);
        }

        [Fact]
        public void then_updates_order_status()
        {
            var command = sut.Commands.Select(x => x.Body).OfType<MarkSeatsAsReserved>().Single();

            Assert.Equal(this.orderId, command.OrderId);
        }

        [Fact]
        public void then_enqueues_expiration_message()
        {
            var message = sut.Commands.Single(x => x.Body is ExpireOrder);

            Assert.Equal(TimeSpan.FromMinutes(15), message.Delay);
            Assert.Equal(this.orderId, ((ExpireOrder)message.Body).OrderId);
        }

        [Fact]
        public void then_transitions_state()
        {
            Assert.Equal(RegistrationProcessSaga.SagaState.AwaitingPayment, sut.State);
        }
    }

    public class given_saga_awaiting_payment
    {
        protected RegistrationProcessSaga sut;
        protected Guid orderId;
        protected Guid conferenceId;
        protected Guid reservationId;

        public given_saga_awaiting_payment()
        {
            this.sut = new RegistrationProcessSaga();
            this.orderId = Guid.NewGuid();
            this.conferenceId = Guid.NewGuid();

            var seatType = Guid.NewGuid();

            this.sut.Handle(new OrderPlaced
            {
                OrderId = this.orderId,
                ConferenceId = this.conferenceId,
                Seats = { new SeatQuantity { SeatType = seatType, Quantity = 2 } }
            });

            var makeReservationCommand = sut.Commands.Select(e => e.Body).OfType<MakeSeatReservation>().Single();
            this.reservationId = makeReservationCommand.ReservationId;

            this.sut.Handle(new SeatsReserved
            {
                ReservationId = makeReservationCommand.ReservationId,
                Seats = { new SeatQuantity { SeatType = seatType, Quantity = 2 }, }
            });
        }
    }

    public class when_reservation_is_expired : given_saga_awaiting_payment
    {
        public when_reservation_is_expired()
        {
            sut.Handle(new ExpireOrder
            {
                OrderId = this.orderId,
            });
        }

        [Fact]
        public void then_cancels_seat_reservation()
        {
            var command = sut.Commands.Select(x => x.Body).OfType<CancelSeatReservation>().Single();

            Assert.Equal(this.reservationId, command.ReservationId);
            Assert.Equal(this.conferenceId, command.ConferenceId);
        }

        [Fact]
        public void then_updates_order_status()
        {
            var command = sut.Commands.Select(x => x.Body).OfType<RejectOrder>().Single();

            Assert.Equal(this.orderId, command.OrderId);
        }

        [Fact]
        public void then_transitions_state()
        {
            Assert.Equal(RegistrationProcessSaga.SagaState.Completed, sut.State);
        }
    }
}
