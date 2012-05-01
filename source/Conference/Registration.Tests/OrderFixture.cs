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

namespace Registration.Tests.OrderFixture
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Infrastructure.EventSourcing;
    using Registration.Commands;
    using Registration.Events;
    using Registration.Handlers;
    using Xunit;

    public class given_no_order
    {
        private static readonly Guid OrderId = Guid.NewGuid();
        private static readonly Guid ConferenceId = Guid.NewGuid();
        private static readonly Guid SeatTypeId = Guid.NewGuid();
        private EventSourcingTestHelper<Order> sut;

        public given_no_order()
        {
            this.sut = new EventSourcingTestHelper<Order>();
            this.sut.Setup(new OrderCommandHandler(sut.Repository));
        }

        [Fact]
        public void when_creating_order_then_is_placed()
        {
            PlaceOrder();

            Assert.Single(sut.Events);
            Assert.Equal(OrderId, ((OrderPlaced)sut.Events.Single()).SourceId);
        }

        [Fact]
        public void when_placing_order_then_has_full_details()
        {
            PlaceOrder();

            var @event = (OrderPlaced)sut.Events.Single();
            Assert.Equal(OrderId, @event.SourceId);
            Assert.Equal(ConferenceId, @event.ConferenceId);
            Assert.Equal(1, @event.Seats.Count());
            Assert.Equal(5, @event.Seats.ElementAt(0).Quantity);
        }

        [Fact]
        public void when_placing_order_then_raises_integration_event_with_access_code()
        {
            //TODO: does this need to be part of the write model?
            PlaceOrder();

            var @event = (OrderPlaced)sut.Events.Single();
            Assert.NotEmpty(@event.AccessCode);
        }

        [Fact]
        public void when_placing_order_then_raises_integration_event_with_expected_expiration_time_in_15_minutes()
        {
            PlaceOrder();

            var @event = (OrderPlaced)sut.Events.Single();
            var relativeExpiration = @event.ReservationAutoExpiration.Subtract(DateTime.UtcNow);
            Assert.True(relativeExpiration.Minutes <= 16);
            Assert.True(relativeExpiration.Minutes >= 14);
        }

        private void PlaceOrder()
        {
            var seats = new[] { new SeatQuantity(SeatTypeId, 5) };
            this.sut.When(new RegisterToConference { ConferenceId = ConferenceId, OrderId = OrderId, Seats = seats });
        }
    }

    public class given_placed_order
    {
        private static readonly Guid OrderId = Guid.NewGuid();
        private static readonly Guid ConferenceId = Guid.NewGuid();
        private static readonly Guid SeatTypeId = Guid.NewGuid();

        private EventSourcingTestHelper<Order> sut;

        public given_placed_order()
        {
            this.sut = new EventSourcingTestHelper<Order>();
            this.sut.Setup(new OrderCommandHandler(sut.Repository));

            this.sut.Given(
                    new OrderPlaced 
                    { 
                        SourceId = OrderId,
                        ConferenceId = ConferenceId,
                        Seats = new[] { new SeatQuantity(SeatTypeId, 5) },
                        ReservationAutoExpiration = DateTime.UtcNow
                    });
        }

        [Fact]
        public void when_updating_seats_then_updates_order_with_new_seats()
        {
            this.sut.When(new RegisterToConference{ ConferenceId = ConferenceId, OrderId = OrderId, Seats = new[] { new SeatQuantity(SeatTypeId, 20)  }});

            var @event = sut.ThenHasSingle<OrderUpdated>();
            Assert.Equal(OrderId, @event.SourceId);
            Assert.Equal(1, @event.Seats.Count());
            Assert.Equal(20, @event.Seats.ElementAt(0).Quantity);
        }

        [Fact]
        public void when_marking_a_subset_of_seats_as_reserved_then_order_is_partially_reserved()
        {
            var expiration = DateTime.UtcNow.AddMinutes(15);
            this.sut.When(new MarkSeatsAsReserved { OrderId = OrderId, Expiration = expiration, Seats = new List<SeatQuantity> { new SeatQuantity(SeatTypeId, 3) } });

            var @event = sut.ThenHasSingle<OrderPartiallyReserved>();
            Assert.Equal(OrderId, @event.SourceId);
            Assert.Equal(1, @event.Seats.Count());
            Assert.Equal(3, @event.Seats.ElementAt(0).Quantity);
            Assert.Equal(expiration, @event.ReservationExpiration);
        }

        [Fact]
        public void when_marking_all_seats_as_reserved_then_order_is_reserved()
        {
            var expiration = DateTime.UtcNow.AddMinutes(15);
            this.sut.When(new MarkSeatsAsReserved { OrderId = OrderId, Expiration = expiration, Seats = new List<SeatQuantity> { new SeatQuantity(SeatTypeId, 5) } });

            var @event = sut.ThenHasSingle<OrderReservationCompleted>();
            Assert.Equal(OrderId, @event.SourceId);
            Assert.Equal(1, @event.Seats.Count());
            Assert.Equal(5, @event.Seats.ElementAt(0).Quantity);
            Assert.Equal(expiration, @event.ReservationExpiration);
        }

        [Fact]
        public void when_expiring_order_then_notifies()
        {
            this.sut.When(new RejectOrder { OrderId = OrderId });

            var @event = sut.ThenHasSingle<OrderExpired>();
            Assert.Equal(OrderId, @event.SourceId);
        }

        [Fact]
        public void when_assigning_registrant_information_then_raises_integration_event()
        {
            this.sut.When(new AssignRegistrantDetails { OrderId = OrderId, FirstName = "foo", LastName = "bar", Email = "foo@bar.com" });

            var @event = sut.ThenHasSingle<OrderRegistrantAssigned>();
            Assert.Equal(OrderId, @event.SourceId);
            Assert.Equal("foo", @event.FirstName);
            Assert.Equal("bar", @event.LastName);
            Assert.Equal("foo@bar.com", @event.Email);
        }

        //[Fact]
        //public void when_confirming_payment_throws()
        //{
        //    Assert.Throws<InvalidOperationException>(() => this.sut.ConfirmPayment());
        //}
    }

    public class given_fully_reserved_order
    {
        private static readonly Guid OrderId = Guid.NewGuid();
        private static readonly Guid ConferenceId = Guid.NewGuid();
        private static readonly Guid SeatTypeId = Guid.NewGuid();

        private EventSourcingTestHelper<Order> sut;

        public given_fully_reserved_order()
        {
            this.sut = new EventSourcingTestHelper<Order>();
            this.sut.Setup(new OrderCommandHandler(sut.Repository));

            this.sut.Given(
                new OrderPlaced
                    {
                        SourceId = OrderId,
                        ConferenceId = ConferenceId,
                        Seats = new[] { new SeatQuantity(SeatTypeId, 5) },
                        ReservationAutoExpiration = DateTime.UtcNow
                    },
                new OrderReservationCompleted
                    {
                        SourceId = OrderId,
                        ReservationExpiration = DateTime.UtcNow.AddMinutes(5),
                        Seats = new[] { new SeatQuantity(SeatTypeId, 5) }
                    });
        }

        [Fact]
        public void when_expiring_order_then_notifies()
        {
            this.sut.When(new RejectOrder { OrderId = OrderId });

            var @event = sut.ThenHasSingle<OrderExpired>();
            Assert.Equal(OrderId, @event.SourceId);
        }

        [Fact]
        public void when_confirming_payment_then_notifies()
        {
            this.sut.When(new ConfirmOrderPayment { OrderId = OrderId });

            var @event = sut.ThenHasSingle<OrderPaymentConfirmed>();
            Assert.Equal(OrderId, @event.SourceId);
        }
    }
}
