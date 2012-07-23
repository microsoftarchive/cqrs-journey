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

using System;
using System.Linq;
using Infrastructure.Messaging;
using Payments.Contracts.Events;
using Registration.Commands;
using Registration.Events;
using Xunit;

namespace Registration.Tests.RegistrationProcessManagerFixture.given_uninitialized_process
{
    public class Context
    {
        protected RegistrationProcessManager sut;

        public Context()
        {
            this.sut = new RegistrationProcessManager();
        }
    }

    public class when_order_is_placed : Context
    {
        private OrderPlaced orderPlaced;

        public when_order_is_placed()
        {
            this.orderPlaced = new OrderPlaced
                                   {
                                       SourceId = Guid.NewGuid(),
                                       ConferenceId = Guid.NewGuid(),
                                       Seats = new[] { new SeatQuantity(Guid.NewGuid(), 2) },
                                       ReservationAutoExpiration = DateTime.UtcNow.Add(TimeSpan.FromMinutes(22))
                                   };
            sut.Handle(orderPlaced);
        }

        [Fact]
        public void then_sends_two_commands()
        {
            Assert.Equal(2, sut.Commands.Count());
        }

        [Fact]
        public void then_reservation_is_requested_for_specific_conference()
        {
            var reservation = sut.Commands.Select(x => x.Body).OfType<MakeSeatReservation>().Single();

            Assert.Equal(orderPlaced.ConferenceId, reservation.ConferenceId);
            Assert.Equal(2, reservation.Seats[0].Quantity);
        }

        [Fact]
        public void then_saves_reservation_command_id_for_later_use()
        {
            var reservation = sut.Commands.Select(x => x.Body).OfType<MakeSeatReservation>().Single();

            Assert.Equal(reservation.Id, this.sut.SeatReservationCommandId);
        }

        [Fact]
        public void then_posts_delayed_expiration_command()
        {
            var expirationCommandEnvelope = this.sut.Commands.Where(e => e.Body is ExpireRegistrationProcess).Single();

            Assert.True(expirationCommandEnvelope.Delay > TimeSpan.FromMinutes(32));
            Assert.Equal(((ExpireRegistrationProcess)expirationCommandEnvelope.Body).ProcessId, this.sut.Id);
            Assert.Equal(expirationCommandEnvelope.Body.Id, this.sut.ExpirationCommandId);
        }

        [Fact]
        public void then_reservation_expiration_time_is_stored_for_later_use()
        {
            Assert.True(sut.ReservationAutoExpiration.HasValue);
            Assert.Equal(orderPlaced.ReservationAutoExpiration, sut.ReservationAutoExpiration.Value);
        }

        [Fact]
        public void then_transitions_to_awaiting_reservation_confirmation_state()
        {
            Assert.Equal(RegistrationProcessManager.ProcessState.AwaitingReservationConfirmation, sut.State);
        }
    }

    public class when_order_is_placed_but_already_expired : Context
    {
        private OrderPlaced orderPlaced;

        public when_order_is_placed_but_already_expired()
        {
            this.orderPlaced = new OrderPlaced
            {
                SourceId = Guid.NewGuid(),
                ConferenceId = Guid.NewGuid(),
                Seats = new[] { new SeatQuantity(Guid.NewGuid(), 2) },
                ReservationAutoExpiration = DateTime.UtcNow.Add(TimeSpan.FromMinutes(-1))
            };
            sut.Handle(orderPlaced);
        }

        [Fact]
        public void then_order_is_rejected()
        {
            var command = sut.Commands.Select(x => x.Body).Cast<RejectOrder>().Single();

            Assert.Equal(orderPlaced.SourceId, command.OrderId);
        }

        [Fact]
        public void then_process_manager_is_completed()
        {
            Assert.True(sut.Completed);
        }
    }
}

namespace Registration.Tests.RegistrationProcessFixture.given_process_awaiting_for_reservation_confirmation
{
    public class Context
    {
        protected RegistrationProcessManager sut;
        protected Guid orderId;
        protected Guid conferenceId;

        public Context()
        {
            this.sut = new RegistrationProcessManager();
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

    public class when_reservation_confirmation_is_received : Context
    {
        private Guid reservationId;

        public when_reservation_confirmation_is_received()
        {
            var makeReservationCommand = sut.Commands.Select(e => e.Body).OfType<MakeSeatReservation>().Single();
            this.reservationId = makeReservationCommand.ReservationId;

            var seatsReserved = new SeatsReserved { SourceId = this.conferenceId, ReservationId = makeReservationCommand.ReservationId, ReservationDetails = new SeatQuantity[0] };
            sut.Handle(new Envelope<SeatsReserved>(seatsReserved) { CorrelationId = makeReservationCommand.Id.ToString() });
        }

        [Fact]
        public void then_updates_order_status()
        {
            var command = sut.Commands.Select(x => x.Body).OfType<MarkSeatsAsReserved>().Single();

            Assert.Equal(this.orderId, command.OrderId);
        }

        [Fact]
        public void then_transitions_state()
        {
            Assert.Equal(RegistrationProcessManager.ProcessState.ReservationConfirmationReceived, sut.State);
        }
    }

    public class when_reservation_confirmation_is_received_for_non_current_correlation_id : Context
    {
        private int initialCommandCount;

        public when_reservation_confirmation_is_received_for_non_current_correlation_id()
        {
            var makeReservationCommand = sut.Commands.Select(e => e.Body).OfType<MakeSeatReservation>().Single();

            var seatsReserved = new SeatsReserved { SourceId = this.conferenceId, ReservationId = makeReservationCommand.ReservationId, ReservationDetails = new SeatQuantity[0] };
            this.initialCommandCount = this.sut.Commands.Count();
            sut.Handle(new Envelope<SeatsReserved>(seatsReserved) { CorrelationId = Guid.NewGuid().ToString() });
        }

        [Fact]
        public void then_does_not_update_order_status()
        {
            Assert.Equal(this.initialCommandCount, this.sut.Commands.Count());
        }

        [Fact]
        public void then_does_not_transition_state()
        {
            Assert.Equal(RegistrationProcessManager.ProcessState.AwaitingReservationConfirmation, sut.State);
        }
    }

    public class when_order_update_is_received : Context
    {
        private Guid reservationId;
        private OrderUpdated orderUpdated;

        public when_order_update_is_received()
        {
            var makeReservationCommand = sut.Commands.Select(e => e.Body).OfType<MakeSeatReservation>().Single();
            this.reservationId = makeReservationCommand.ReservationId;

            this.orderUpdated = new OrderUpdated
            {
                SourceId = Guid.NewGuid(),
                Seats = new[] { new SeatQuantity(Guid.NewGuid(), 3) }
            };
            sut.Handle(orderUpdated);
        }

        [Fact]
        public void then_sends_new_reservation_command()
        {
            Assert.Equal(2, sut.Commands.Select(x => x.Body).OfType<MakeSeatReservation>().Count());
        }

        [Fact]
        public void then_reservation_is_requested_for_specific_conference()
        {
            var newReservation = sut.Commands.Select(x => x.Body).OfType<MakeSeatReservation>().ElementAt(1);

            Assert.Equal(this.conferenceId, newReservation.ConferenceId);
            Assert.Equal(3, newReservation.Seats[0].Quantity);
        }

        [Fact]
        public void then_saves_reservation_command_id_for_later_use()
        {
            var reservation = sut.Commands.Select(x => x.Body).OfType<MakeSeatReservation>().ElementAt(1);

            Assert.Equal(reservation.Id, this.sut.SeatReservationCommandId);
        }

        [Fact]
        public void then_transitions_to_awaiting_reservation_confirmation_state()
        {
            Assert.Equal(RegistrationProcessManager.ProcessState.AwaitingReservationConfirmation, sut.State);
        }
    }
}

namespace Registration.Tests.RegistrationProcessFixture.given_process_with_reservation_confirmation_received
{
    public class Context
    {
        protected RegistrationProcessManager sut;
        protected Guid orderId;
        protected Guid conferenceId;
        protected Guid reservationId;

        public Context()
        {
            this.sut = new RegistrationProcessManager();
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
                new Envelope<SeatsReserved>(
                    new SeatsReserved
                    {
                        SourceId = this.conferenceId,
                        ReservationId = makeReservationCommand.ReservationId,
                        ReservationDetails = new[] { new SeatQuantity(seatType, 2) }
                    })
                    {
                        CorrelationId = makeReservationCommand.Id.ToString()
                    });
        }
    }

    public class when_reservation_is_expired : Context
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

    public class when_order_update_is_received : Context
    {
        private OrderUpdated orderUpdated;

        public when_order_update_is_received()
        {
            var makeReservationCommand = sut.Commands.Select(e => e.Body).OfType<MakeSeatReservation>().Single();
            this.reservationId = makeReservationCommand.ReservationId;

            this.orderUpdated = new OrderUpdated
            {
                SourceId = this.orderId,
                Seats = new[] { new SeatQuantity(Guid.NewGuid(), 3) }
            };
            sut.Handle(orderUpdated);
        }

        [Fact]
        public void then_sends_new_reservation_command()
        {
            Assert.Equal(2, sut.Commands.Select(x => x.Body).OfType<MakeSeatReservation>().Count());
        }

        [Fact]
        public void then_reservation_is_requested_for_specific_conference()
        {
            var newReservation = sut.Commands.Select(x => x.Body).OfType<MakeSeatReservation>().ElementAt(1);

            Assert.Equal(this.conferenceId, newReservation.ConferenceId);
            Assert.Equal(3, newReservation.Seats[0].Quantity);
        }

        [Fact]
        public void then_saves_reservation_command_id_for_later_use()
        {
            var reservation = sut.Commands.Select(x => x.Body).OfType<MakeSeatReservation>().ElementAt(1);

            Assert.Equal(reservation.Id, this.sut.SeatReservationCommandId);
        }

        [Fact]
        public void then_transitions_to_awaiting_reservation_confirmation_state()
        {
            Assert.Equal(RegistrationProcessManager.ProcessState.AwaitingReservationConfirmation, sut.State);
        }
    }

    public class when_payment_confirmation_is_received : Context
    {
        public when_payment_confirmation_is_received()
        {
            sut.Handle(new PaymentCompleted
            {
                PaymentSourceId = this.orderId,
            });
        }

        [Fact]
        public void then_confirms_order()
        {
            var command = sut.Commands.Select(x => x.Body).OfType<ConfirmOrder>().Single();

            Assert.Equal(this.orderId, command.OrderId);
        }

        [Fact]
        public void then_transitions_state()
        {
            Assert.Equal(RegistrationProcessManager.ProcessState.PaymentConfirmationReceived, this.sut.State);
        }
    }

    public class when_order_is_confirmed : Context
    {
        public when_order_is_confirmed()
        {
            sut.Handle(new OrderConfirmed
            {
                SourceId = this.orderId,
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
        public void then_transitions_state()
        {
            Assert.True(this.sut.Completed);
        }
    }

    public class when_reservation_confirmation_is_received_for_current_correlation_id : Context
    {
        private int initialCommandCount;

        public when_reservation_confirmation_is_received_for_current_correlation_id()
        {
            var makeReservationCommand = sut.Commands.Select(e => e.Body).OfType<MakeSeatReservation>().Single();

            var seatsReserved = new SeatsReserved { SourceId = this.conferenceId, ReservationId = makeReservationCommand.ReservationId, ReservationDetails = new SeatQuantity[0] };
            this.initialCommandCount = this.sut.Commands.Count();
            sut.Handle(new Envelope<SeatsReserved>(seatsReserved) { CorrelationId = makeReservationCommand.Id.ToString() });
        }

        [Fact]
        public void then_does_not_send_new_update_to_order()
        {
            Assert.Equal(this.initialCommandCount, this.sut.Commands.Count());
        }

        [Fact]
        public void then_does_not_transition_state()
        {
            Assert.Equal(RegistrationProcessManager.ProcessState.ReservationConfirmationReceived, sut.State);
        }
    }

    public class when_reservation_confirmation_is_received_for_non_current_correlation_id : Context
    {
        private Exception exception;

        public when_reservation_confirmation_is_received_for_non_current_correlation_id()
        {
            var makeReservationCommand = sut.Commands.Select(e => e.Body).OfType<MakeSeatReservation>().Single();

            var seatsReserved = new SeatsReserved { SourceId = this.conferenceId, ReservationId = makeReservationCommand.ReservationId, ReservationDetails = new SeatQuantity[0] };

            try
            {
                sut.Handle(new Envelope<SeatsReserved>(seatsReserved) { CorrelationId = Guid.NewGuid().ToString() });
            }
            catch (InvalidOperationException e)
            {
                this.exception = e;
            }
        }

        [Fact]
        public void then_throws()
        {
            Assert.NotNull(this.exception);
        }
    }
}

namespace Registration.Tests.RegistrationProcessFixture.given_process_with_payment_confirmation_received
{
    public class Context
    {
        protected RegistrationProcessManager sut;
        protected Guid orderId;
        protected Guid conferenceId;
        protected Guid reservationId;

        public Context()
        {
            this.sut = new RegistrationProcessManager();
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
                new Envelope<SeatsReserved>(
                    new SeatsReserved
                    {
                        SourceId = this.conferenceId,
                        ReservationId = makeReservationCommand.ReservationId,
                        ReservationDetails = new[] { new SeatQuantity(seatType, 2) }
                    })
                    {
                        CorrelationId = makeReservationCommand.Id.ToString()
                    });

            this.sut.Handle(
                new PaymentCompleted
                {
                    PaymentSourceId = this.orderId,
                });
        }
    }

    public class when_order_is_confirmed : Context
    {
        public when_order_is_confirmed()
        {
            sut.Handle(new OrderConfirmed
            {
                SourceId = this.orderId,
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
        public void then_transitions_state()
        {
            Assert.True(this.sut.Completed);
        }
    }
}
