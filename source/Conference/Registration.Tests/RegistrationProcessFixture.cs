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

namespace Registration.Tests.RegistrationProcessFixture
{
    using System;
    using System.Linq;
    using Payments.Contracts.Events;
    using Registration.Commands;
    using Registration.Events;
    using Xunit;

    public class given_uninitialized_process
    {
        protected RegistrationProcess sut;

        public given_uninitialized_process()
        {
            this.sut = new RegistrationProcess();
        }
    }

    public class when_order_is_placed : given_uninitialized_process
    {
        private OrderPlaced orderPlaced;

        public when_order_is_placed()
        {
            this.orderPlaced = new OrderPlaced
                                   {
                                       SourceId = Guid.NewGuid(),
                                       ConferenceId = Guid.NewGuid(),
                                       Seats = new[] {new SeatQuantity(Guid.NewGuid(), 2)},
                                       ReservationAutoExpiration = DateTime.UtcNow.Add(TimeSpan.FromMinutes(22))
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
        public void then_reservation_expiration_time_is_stored_for_later_user()
        {
            Assert.Equal(RegistrationProcess.ProcessState.AwaitingReservationConfirmation, sut.State);
        }

        [Fact]
        public void then_transitions_to_awaiting_reservation_confirmation_state()
        {
            Assert.True(sut.ReservationAutoExpiration.HasValue);
            Assert.Equal(orderPlaced.ReservationAutoExpiration, sut.ReservationAutoExpiration.Value);
        }
    }

    public class given_process_awaiting_for_reservation_confirmation
    {
        protected RegistrationProcess sut;
        protected Guid orderId;
        protected Guid conferenceId;

        public given_process_awaiting_for_reservation_confirmation()
        {
            this.sut = new RegistrationProcess();
            this.orderId = Guid.NewGuid();
            this.conferenceId = Guid.NewGuid();

            this.sut.Handle(
                new OrderPlaced
                {
                    SourceId = this.orderId,
                    ConferenceId = this.conferenceId,
                    Seats = new[] { new SeatQuantity(Guid.NewGuid(), 2) },
                    ReservationAutoExpiration = DateTime.UtcNow.Add(TimeSpan.FromMinutes(22))
                });
        }
    }

    public class when_reservation_confirmation_is_received : given_process_awaiting_for_reservation_confirmation
    {
        private Guid reservationId;

        public when_reservation_confirmation_is_received()
        {
            var makeReservationCommand = sut.Commands.Select(e => e.Body).OfType<MakeSeatReservation>().Single();
            this.reservationId = makeReservationCommand.ReservationId;

            var seatsReserved = new SeatsReserved { SourceId = this.conferenceId, ReservationId = makeReservationCommand.ReservationId, ReservationDetails = new SeatQuantity[0] };
            sut.Handle(seatsReserved);
        }

        [Fact]
        public void then_updates_order_status()
        {
            var command = sut.Commands.Select(x => x.Body).OfType<MarkSeatsAsReserved>().Single();

            Assert.Equal(this.orderId, command.OrderId);
        }

        [Fact]
        public void then_enqueues_expiration_message_using_expected_value_from_order_plus_buffer()
        {
            var message = sut.Commands.Single(x => x.Body is ExpireRegistrationProcess);

            Assert.True(message.Delay > TimeSpan.FromMinutes(22));
            Assert.True(message.Delay < TimeSpan.FromMinutes(30));
            Assert.Equal(sut.Id, ((ExpireRegistrationProcess)message.Body).ProcessId);
        }

        [Fact]
        public void then_transitions_state()
        {
            Assert.Equal(RegistrationProcess.ProcessState.AwaitingPayment, sut.State);
        }
    }

    public class given_process_awaiting_payment
    {
        protected RegistrationProcess sut;
        protected Guid orderId;
        protected Guid conferenceId;
        protected Guid reservationId;

        public given_process_awaiting_payment()
        {
            this.sut = new RegistrationProcess();
            this.orderId = Guid.NewGuid();
            this.conferenceId = Guid.NewGuid();

            var seatType = Guid.NewGuid();

            this.sut.Handle(
                new OrderPlaced
                {
                    SourceId = this.orderId,
                    ConferenceId = this.conferenceId,
                    Seats = new[] { new SeatQuantity(Guid.NewGuid(), 2) },
                    ReservationAutoExpiration = DateTime.UtcNow.Add(TimeSpan.FromMinutes(22))
                });

            var makeReservationCommand = sut.Commands.Select(e => e.Body).OfType<MakeSeatReservation>().Single();
            this.reservationId = makeReservationCommand.ReservationId;

            this.sut.Handle(
                new SeatsReserved
                    {
                        SourceId = this.conferenceId,
                        ReservationId = makeReservationCommand.ReservationId,
                        ReservationDetails = new[] {new SeatQuantity(seatType, 2)}
                    });
        }
    }

    public class when_reservation_is_paid : given_process_awaiting_payment
    {
        public when_reservation_is_paid()
        {
            sut.Handle(new PaymentCompleted
            {
                PaymentSourceId = this.orderId,
            });
        }

        [Fact]
        public void then_commits_seat_reservations()
        {
            var command = sut.Commands.Select(x => x.Body).OfType<CommitSeatReservation>().Single();

            Assert.Equal(this.reservationId, command.ReservationId);
            Assert.Equal(this.conferenceId, command.ConferenceId);
        }

        [Fact]
        public void then_updates_order_status()
        {
            var command = sut.Commands.Select(x => x.Body).OfType<ConfirmOrderPayment>().Single();

            Assert.Equal(this.orderId, command.OrderId);
        }

        [Fact]
        public void then_transitions_state()
        {
            Assert.Equal(true, sut.Completed);
        }
    }

    public class when_reservation_is_expired : given_process_awaiting_payment
    {
        public when_reservation_is_expired()
        {
            var expirationCommand = sut.Commands.Select(x => x.Body).OfType<ExpireRegistrationProcess>().Single();
            sut.Handle(expirationCommand);
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
            Assert.Equal(true, sut.Completed);
        }
    }
}
