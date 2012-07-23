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

namespace Registration.IntegrationTests.DraftOrderViewModelGeneratorFixture
{
    using System;
    using System.Linq;
    using Events;
    using Infrastructure.Serialization;
    using Registration.Handlers;
    using Registration.ReadModel;
    using Registration.ReadModel.Implementation;
    using Xunit;

    public class given_a_read_model_generator : given_a_read_model_database
    {
        protected DraftOrderViewModelGenerator sut;
        protected IOrderDao dao;

        public given_a_read_model_generator()
        {
            var blobStorage = new MemoryBlobStorage();
            this.sut = new DraftOrderViewModelGenerator(() => new ConferenceRegistrationDbContext(dbName));
            this.dao = new OrderDao(() => new ConferenceRegistrationDbContext(dbName), blobStorage, new JsonTextSerializer());
        }
    }

    public class given_a_placed_order : given_a_read_model_generator
    {
        protected OrderPlaced orderPlacedEvent;

        public given_a_placed_order()
        {
            System.Diagnostics.Trace.Listeners.Clear();

            this.orderPlacedEvent = new OrderPlaced
            {
                SourceId = Guid.NewGuid(),
                ConferenceId = Guid.NewGuid(),
                AccessCode = "asdf",
                Seats = new[] { new SeatQuantity(Guid.NewGuid(), 5) },
                Version = 1
            };

            sut.Handle(orderPlacedEvent);
        }

        [Fact]
        public void then_read_model_created()
        {
            var dto = dao.FindDraftOrder(orderPlacedEvent.SourceId);

            Assert.NotNull(dto);
            Assert.Equal("asdf", dto.AccessCode);
            Assert.Equal(orderPlacedEvent.ConferenceId, dto.ConferenceId);
            Assert.Equal(orderPlacedEvent.SourceId, dto.OrderId);
            Assert.Equal(1, dto.Lines.Count);
            Assert.Equal(1, dto.OrderVersion);
        }

        [Fact]
        public void then_order_is_pending_reservation()
        {
            var dto = dao.FindDraftOrder(orderPlacedEvent.SourceId);

            Assert.Equal(DraftOrder.States.PendingReservation, dto.State);
        }

        [Fact]
        public void then_order_does_not_contain_expiration_yet()
        {
            var dto = dao.FindDraftOrder(orderPlacedEvent.SourceId);

            Assert.Null(dto.ReservationExpirationDate);
        }

        [Fact]
        public void then_one_order_line_per_seat_type_is_created()
        {
            var dto = dao.FindDraftOrder(orderPlacedEvent.SourceId);

            Assert.Equal(1, dto.Lines.Count);
            Assert.Equal(orderPlacedEvent.Seats.First().SeatType, dto.Lines.First().SeatType);
        }

        [Fact]
        public void then_order_line_seats_are_requested_but_not_reserved_yet()
        {
            var dto = dao.FindDraftOrder(orderPlacedEvent.SourceId);

            Assert.Equal(orderPlacedEvent.Seats.First().Quantity, dto.Lines.First().RequestedSeats);
            Assert.Equal(0, dto.Lines.First().ReservedSeats);
        }

        [Fact]
        public void when_registrant_information_assigned_then_email_is_persisted()
        {
            sut.Handle(new OrderRegistrantAssigned
            {
                Email = "a@b.com",
                FirstName = "A",
                LastName = "Z",
                SourceId = orderPlacedEvent.SourceId,
                Version = 5
            });

            var dto = dao.FindDraftOrder(orderPlacedEvent.SourceId);

            Assert.Equal("a@b.com", dto.RegistrantEmail);
        }

        [Fact]
        public void when_registrant_information_assigned_then_can_locate_order()
        {
            sut.Handle(new OrderRegistrantAssigned
            {
                Email = "a@b.com",
                FirstName = "A",
                LastName = "Z",
                SourceId = orderPlacedEvent.SourceId,
                Version = 5
            });

            var actual = dao.LocateOrder("a@b.com", orderPlacedEvent.AccessCode);

            Assert.NotNull(actual);
            Assert.Equal(orderPlacedEvent.SourceId, actual.Value);
        }

        [Fact]
        public void when_order_is_updated_then_removes_original_lines()
        {
            sut.Handle(new OrderUpdated
            {
                SourceId = orderPlacedEvent.SourceId,
                Seats = new[] { new SeatQuantity(Guid.NewGuid(), 2) },
                Version = 2
            });

            var dto = dao.FindDraftOrder(orderPlacedEvent.SourceId);

            Assert.False(dto.Lines.Any(line => line.SeatType == orderPlacedEvent.Seats.First().SeatType));
        }

        [Fact]
        public void when_order_is_updated_then_adds_new_lines()
        {
            var newSeat = Guid.NewGuid();
            sut.Handle(new OrderUpdated
            {
                SourceId = orderPlacedEvent.SourceId,
                Seats = new[] { new SeatQuantity(newSeat, 2) },
                Version = 4,
            });

            var dto = dao.FindDraftOrder(orderPlacedEvent.SourceId);

            Assert.Equal(2, dto.Lines.First().RequestedSeats);
            Assert.Equal(newSeat, dto.Lines.First().SeatType);
            Assert.Equal(4, dto.OrderVersion);
        }

        [Fact]
        public void when_order_updated_event_is_received_twice_then_no_ops()
        {
            var newSeat = Guid.NewGuid();
            sut.Handle(new OrderUpdated
            {
                SourceId = orderPlacedEvent.SourceId,
                Seats = new[] { new SeatQuantity(newSeat, 2) },
                Version = 4,
            });

            var seatType = dao.FindDraftOrder(orderPlacedEvent.SourceId).Lines.First().SeatType;

            sut.Handle(new OrderUpdated
            {
                SourceId = orderPlacedEvent.SourceId,
                Seats = new[] { new SeatQuantity(newSeat, 2) },
                Version = 4,
            });

            Assert.Equal(seatType, dao.FindDraftOrder(orderPlacedEvent.SourceId).Lines.First().SeatType);
        }

        [Fact]
        public void when_order_is_updated_then_state_is_pending_reservation()
        {
            sut.Handle(new OrderUpdated
            {
                SourceId = orderPlacedEvent.SourceId,
                Seats = new[] { new SeatQuantity(Guid.NewGuid(), 2) },
                Version = 4,
            });

            var dto = dao.FindDraftOrder(orderPlacedEvent.SourceId);

            Assert.Equal(DraftOrder.States.PendingReservation, dto.State);
        }

        [Fact]
        public void when_order_is_updated_then_removes_original_lines_from_originating_order()
        {
            var secondOrder = Guid.NewGuid();
            sut.Handle(new OrderPlaced
            {
                SourceId = secondOrder,
                ConferenceId = Guid.NewGuid(),
                AccessCode = "asdf",
                Seats = new[] { new SeatQuantity(Guid.NewGuid(), 5) },
                Version = 1,
            });

            sut.Handle(new OrderUpdated
            {
                SourceId = orderPlacedEvent.SourceId,
                Seats = new[] { new SeatQuantity(Guid.NewGuid(), 2) },
                Version = 4,
            });

            var dto = dao.FindDraftOrder(secondOrder);

            Assert.Equal(1, dto.Lines.Count);
        }

        [Fact]
        public void when_order_partially_reserved_then_sets_order_expiration()
        {
            sut.Handle(new OrderPartiallyReserved
            {
                SourceId = orderPlacedEvent.SourceId,
                ReservationExpiration = DateTime.UtcNow.AddMinutes(15),
                // We got two rather than the requested 5.
                Seats = new[] { new SeatQuantity(orderPlacedEvent.Seats.First().SeatType, 2) },
                Version = 3,
            });

            var dto = dao.FindDraftOrder(orderPlacedEvent.SourceId);

            Assert.NotNull(dto.ReservationExpirationDate);
        }

        [Fact]
        public void when_order_partially_reserved_then_updates_reserved_seats()
        {
            sut.Handle(new OrderPartiallyReserved
            {
                SourceId = orderPlacedEvent.SourceId,
                ReservationExpiration = DateTime.UtcNow.AddMinutes(15),
                // We got two rather than the requested 5.
                Seats = new[] { new SeatQuantity(orderPlacedEvent.Seats.First().SeatType, 2) },
                Version = 3,
            });

            var dto = dao.FindDraftOrder(orderPlacedEvent.SourceId);

            Assert.Equal(2, dto.Lines.First().ReservedSeats);
        }

        [Fact]
        public void when_order_partially_reserved_then_state_is_partially_reserved()
        {
            sut.Handle(new OrderPartiallyReserved
            {
                SourceId = orderPlacedEvent.SourceId,
                ReservationExpiration = DateTime.UtcNow.AddMinutes(15),
                // We got two rather than the requested 5.
                Seats = new[] { new SeatQuantity(orderPlacedEvent.Seats.First().SeatType, 2) },
                Version = 3,
            });

            var dto = dao.FindDraftOrder(orderPlacedEvent.SourceId);

            Assert.Equal(DraftOrder.States.PartiallyReserved, dto.State);
            Assert.Equal(3, dto.OrderVersion);
        }

        [Fact]
        public void when_order_fully_reserved_then_sets_order_expiration()
        {
            sut.Handle(new OrderReservationCompleted
            {
                SourceId = orderPlacedEvent.SourceId,
                ReservationExpiration = DateTime.UtcNow.AddMinutes(15),
                Seats = new[] { orderPlacedEvent.Seats.First() },
                Version = 3,
            });

            var dto = dao.FindDraftOrder(orderPlacedEvent.SourceId);

            Assert.NotNull(dto.ReservationExpirationDate);
        }

        [Fact]
        public void when_order_fully_reserved_then_updates_reserved_seats()
        {
            sut.Handle(new OrderReservationCompleted
            {
                SourceId = orderPlacedEvent.SourceId,
                ReservationExpiration = DateTime.UtcNow.AddMinutes(15),
                Seats = new[] { orderPlacedEvent.Seats.First() },
                Version = 3,
            });

            var dto = dao.FindDraftOrder(orderPlacedEvent.SourceId);

            Assert.Equal(dto.Lines.First().RequestedSeats, dto.Lines.First().ReservedSeats);
        }

        [Fact]
        public void when_order_fully_reserved_then_state_is_reservation_completed()
        {
            sut.Handle(new OrderReservationCompleted
            {
                SourceId = orderPlacedEvent.SourceId,
                ReservationExpiration = DateTime.UtcNow.AddMinutes(15),
                Seats = new[] { orderPlacedEvent.Seats.First() },
                Version = 3,
            });

            var dto = dao.FindDraftOrder(orderPlacedEvent.SourceId);

            Assert.Equal(DraftOrder.States.ReservationCompleted, dto.State);
            Assert.Equal(3, dto.OrderVersion);
        }

        [Fact]
        public void when_order_confirmed_v1_then_order_state_is_confirmed()
        {
            sut.Handle(new OrderPaymentConfirmed
            {
                SourceId = orderPlacedEvent.SourceId,
                Version = 7,
            });

            var dto = dao.FindDraftOrder(orderPlacedEvent.SourceId);

            Assert.Equal(DraftOrder.States.Confirmed, dto.State);
            Assert.Equal(7, dto.OrderVersion);
        }

        [Fact]
        public void when_order_confirmed_then_order_state_is_confirmed()
        {
            sut.Handle(new OrderConfirmed
            {
                SourceId = orderPlacedEvent.SourceId,
                Version = 7,
            });

            var dto = dao.FindDraftOrder(orderPlacedEvent.SourceId);

            Assert.Equal(DraftOrder.States.Confirmed, dto.State);
        }

        [Fact]
        public void when_order_confirmed_then_updates_order_version()
        {
            sut.Handle(new OrderConfirmed
            {
                SourceId = orderPlacedEvent.SourceId,
                Version = 7,
            });

            var dto = dao.FindDraftOrder(orderPlacedEvent.SourceId);

            Assert.Equal(7, dto.OrderVersion);
        }

        [Fact]
        public void when_order_confirmed_for_older_version_then_no_ops()
        {
            sut.Handle(new OrderUpdated
            {
                SourceId = orderPlacedEvent.SourceId,
                Seats = new[] { new SeatQuantity(Guid.NewGuid(), 2) },
                Version = 4,
            });

            sut.Handle(new OrderConfirmed { SourceId = orderPlacedEvent.SourceId, Version = 1, });

            var dto = dao.FindDraftOrder(orderPlacedEvent.SourceId);
            Assert.Equal(4, dto.OrderVersion);
            Assert.Equal(DraftOrder.States.PendingReservation, dto.State);
        }
    }
}
