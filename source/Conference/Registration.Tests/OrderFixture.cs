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

namespace Registration.Tests.OrderFixture
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Moq;
    using Registration.Commands;
    using Registration.Events;
    using Registration.Handlers;
    using Xunit;

    public class given_no_order
    {
        private static readonly Guid OrderId = Guid.NewGuid();
        private static readonly Guid ConferenceId = Guid.NewGuid();
        private static readonly Guid SeatTypeId = Guid.NewGuid();
        private static readonly OrderTotal OrderTotal = new OrderTotal { Total = 33, Lines = new[] { new OrderLine() } };
        private EventSourcingTestHelper<Order> sut;
        private readonly Mock<IPricingService> pricingService;

        public given_no_order()
        {
            this.pricingService = new Mock<IPricingService>();
            this.pricingService.Setup(x => x.CalculateTotal(ConferenceId, It.IsAny<ICollection<SeatQuantity>>())).Returns(OrderTotal);
            this.sut = new EventSourcingTestHelper<Order>();
            this.sut.Setup(new OrderCommandHandler(this.sut.Repository, pricingService.Object));
        }

        [Fact]
        public void when_creating_order_then_is_placed_with_specified_id()
        {
            this.sut.When(new RegisterToConference { ConferenceId = ConferenceId, OrderId = OrderId, Seats = new[] { new SeatQuantity(SeatTypeId, 5) } });

            Assert.Equal(OrderId, sut.ThenHasOne<OrderPlaced>().SourceId);
        }

        [Fact]
        public void when_placing_order_then_has_full_details()
        {
            this.sut.When(new RegisterToConference { ConferenceId = ConferenceId, OrderId = OrderId, Seats = new[] { new SeatQuantity(SeatTypeId, 5) } });

            var @event = sut.ThenHasOne<OrderPlaced>();
            Assert.Equal(OrderId, @event.SourceId);
            Assert.Equal(ConferenceId, @event.ConferenceId);
            Assert.Equal(1, @event.Seats.Count());
            Assert.Equal(5, @event.Seats.ElementAt(0).Quantity);
        }

        [Fact]
        public void when_placing_order_then_has_access_code()
        {
            //TODO: does this need to be part of the write model?
            this.sut.When(new RegisterToConference { ConferenceId = ConferenceId, OrderId = OrderId, Seats = new[] { new SeatQuantity(SeatTypeId, 5) } });

            var @event = sut.ThenHasOne<OrderPlaced>();
            Assert.NotEmpty(@event.AccessCode);
        }

        [Fact]
        public void when_placing_order_then_defines_expected_expiration_time_in_15_minutes()
        {
            this.sut.When(new RegisterToConference { ConferenceId = ConferenceId, OrderId = OrderId, Seats = new[] { new SeatQuantity(SeatTypeId, 5) } });

            var @event = sut.ThenHasOne<OrderPlaced>();
            var relativeExpiration = @event.ReservationAutoExpiration.Subtract(DateTime.UtcNow);
            Assert.True(relativeExpiration.Minutes <= 16);
            Assert.True(relativeExpiration.Minutes >= 14);
        }

        [Fact]
        public void when_creating_order_then_calculates_totals()
        {
            this.sut.When(new RegisterToConference { ConferenceId = ConferenceId, OrderId = OrderId, Seats = new[] { new SeatQuantity(SeatTypeId, 5) } });

            var totals = sut.ThenHasOne<OrderTotalsCalculated>();
            Assert.Equal(OrderTotal.Total, totals.Total);
            Assert.Equal(OrderTotal.Lines.Count, totals.Lines.Length);
            Assert.Equal(OrderTotal.Lines.First().LineTotal, totals.Lines[0].LineTotal);
        }
    }

    public class given_placed_order
    {
        private static readonly Guid OrderId = Guid.NewGuid();
        private static readonly Guid ConferenceId = Guid.NewGuid();
        private static readonly Guid SeatTypeId = Guid.NewGuid();
        private static readonly OrderTotal OrderTotal = new OrderTotal { Total = 33, Lines = new [] { new OrderLine() } };
        private EventSourcingTestHelper<Order> sut;
        private readonly Mock<IPricingService> pricingService;

        public given_placed_order()
        {
            this.pricingService = new Mock<IPricingService>();
            this.pricingService.Setup(x => x.CalculateTotal(ConferenceId, It.IsAny<ICollection<SeatQuantity>>())).Returns(OrderTotal);
            this.sut = new EventSourcingTestHelper<Order>();
            this.sut.Setup(new OrderCommandHandler(sut.Repository, pricingService.Object));

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
            this.sut.When(new RegisterToConference { ConferenceId = ConferenceId, OrderId = OrderId, Seats = new[] { new SeatQuantity(SeatTypeId, 20) }});

            var @event = sut.ThenHasOne<OrderUpdated>();
            Assert.Equal(OrderId, @event.SourceId);
            Assert.Equal(1, @event.Seats.Count());
            Assert.Equal(20, @event.Seats.ElementAt(0).Quantity);
        }

        [Fact]
        public void when_marking_a_subset_of_seats_as_reserved_then_order_is_partially_reserved()
        {
            var expiration = DateTime.UtcNow.AddMinutes(15);
            this.sut.When(new MarkSeatsAsReserved { OrderId = OrderId, Expiration = expiration, Seats = new List<SeatQuantity> { new SeatQuantity(SeatTypeId, 3) } });

            var @event = sut.ThenHasOne<OrderPartiallyReserved>();
            Assert.Equal(OrderId, @event.SourceId);
            Assert.Equal(1, @event.Seats.Count());
            Assert.Equal(3, @event.Seats.ElementAt(0).Quantity);
            Assert.Equal(expiration, @event.ReservationExpiration);
        }

        [Fact]
        public void when_marking_a_subset_of_seats_as_reserved_then_totals_are_calculated()
        {
            var expiration = DateTime.UtcNow.AddMinutes(15);
            this.sut.When(new MarkSeatsAsReserved { OrderId = OrderId, Expiration = expiration, Seats = new List<SeatQuantity> { new SeatQuantity(SeatTypeId, 3) } });

            var @event = sut.ThenHasOne<OrderTotalsCalculated>();
            Assert.Equal(OrderId, @event.SourceId);
            Assert.Equal(33, @event.Total);
            Assert.Equal(1, @event.Lines.Count());
            Assert.Same(OrderTotal.Lines.Single(), @event.Lines.Single());

            pricingService.Verify(s => s.CalculateTotal(ConferenceId, It.Is<ICollection<SeatQuantity>>(x => x.Single().SeatType == SeatTypeId && x.Single().Quantity == 3)));
        }

        [Fact]
        public void when_marking_all_seats_as_reserved_then_order_is_reserved()
        {
            var expiration = DateTime.UtcNow.AddMinutes(15);
            this.sut.When(new MarkSeatsAsReserved { OrderId = OrderId, Expiration = expiration, Seats = new List<SeatQuantity> { new SeatQuantity(SeatTypeId, 5) } });

            var @event = sut.ThenHasOne<OrderReservationCompleted>();
            Assert.Equal(OrderId, @event.SourceId);
            Assert.Equal(1, @event.Seats.Count());
            Assert.Equal(5, @event.Seats.ElementAt(0).Quantity);
            Assert.Equal(expiration, @event.ReservationExpiration);
        }

        [Fact]
        public void when_marking_all_as_reserved_then_totals_are_not_recalculated()
        {
            var expiration = DateTime.UtcNow.AddMinutes(15);
            this.sut.When(new MarkSeatsAsReserved { OrderId = OrderId, Expiration = expiration, Seats = new List<SeatQuantity> { new SeatQuantity(SeatTypeId, 5) } });

            Assert.Equal(0, sut.Events.OfType<OrderTotalsCalculated>().Count());
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
        private static readonly OrderTotal OrderTotal = new OrderTotal { Total = 33, Lines = new[] { new OrderLine() } };
        private EventSourcingTestHelper<Order> sut;
        private readonly Mock<IPricingService> pricingService;

        public given_fully_reserved_order()
        {
            this.pricingService = new Mock<IPricingService>();
            this.pricingService.Setup(x => x.CalculateTotal(ConferenceId, It.IsAny<ICollection<SeatQuantity>>())).Returns(OrderTotal);
            this.sut = new EventSourcingTestHelper<Order>();
            this.sut.Setup(new OrderCommandHandler(this.sut.Repository, pricingService.Object));

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
        public void when_confirming_order_then_notifies()
        {
            this.sut.When(new ConfirmOrder { OrderId = OrderId });

            var @event = sut.ThenHasSingle<OrderConfirmed>();
            Assert.Equal(OrderId, @event.SourceId);
        }

        [Fact]
        public void when_updating_an_order_then_updates_seats()
        {
            this.sut.When(new RegisterToConference { OrderId = OrderId, Seats = new[] { new SeatQuantity(SeatTypeId, 4) } });

            var updated = sut.ThenHasOne<OrderUpdated>();
            Assert.Equal(OrderId, updated.SourceId);
            Assert.Equal(SeatTypeId, updated.Seats.First().SeatType);
            Assert.Equal(4, updated.Seats.First().Quantity);
        }

        [Fact]
        public void when_updating_an_order_then_recalculates()
        {
            this.sut.When(new RegisterToConference { OrderId = OrderId, Seats = new[] { new SeatQuantity(SeatTypeId, 4) } });

            var @event = sut.ThenHasOne<OrderTotalsCalculated>();
            Assert.Equal(OrderId, @event.SourceId);
            Assert.Equal(33, @event.Total);
            Assert.Equal(1, @event.Lines.Count());
            Assert.Same(OrderTotal.Lines.Single(), @event.Lines.Single());
        }

        [Fact]
        public void when_rejecting_confirmed_order_then_throws()
        {
            this.sut.Given(new OrderConfirmed { SourceId = OrderId });

            Assert.Throws<InvalidOperationException>(() => this.sut.When(new RejectOrder { OrderId = OrderId }));
        }

        [Fact]
        public void when_rejecting_a_payment_confirmed_order_then_throws()
        {
            this.sut.Given(new OrderPaymentConfirmed { SourceId = OrderId });

            Assert.Throws<InvalidOperationException>(() => this.sut.When(new RejectOrder { OrderId = OrderId }));
        }
    }
}
