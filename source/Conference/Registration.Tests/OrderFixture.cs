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
    using System.Linq;
    using Common;
    using Registration.Events;
    using Registration.Tests;
    using Xunit;

    public class given_no_order
    {
        private static readonly Guid OrderId = Guid.NewGuid();
        private static readonly Guid ConferenceId = Guid.NewGuid();
        private static readonly Guid SeatTypeId = Guid.NewGuid();

        private Order sut;

        [Fact]
        public void when_creating_order_then_is_placed()
        {
            PlaceOrder();

            Assert.Single(sut.Events);
            Assert.Equal(OrderId, ((OrderPlaced)sut.Events.Single()).OrderId);
        }

        [Fact]
        public void when_placing_order_then_has_full_details()
        {
            PlaceOrder();

            var @event = (OrderPlaced)sut.Events.Single();
            Assert.Equal(OrderId, @event.OrderId);
            Assert.Equal(ConferenceId, @event.ConferenceId);
            Assert.Equal(1, @event.Seats.Count);
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
            var lines = new[] { new OrderItem(SeatTypeId, 5) };
            this.sut = new Order(OrderId, ConferenceId, lines);
        }
    }

    public class given_placed_order
    {
        private static readonly Guid OrderId = Guid.NewGuid();
        private static readonly Guid ConferenceId = Guid.NewGuid();
        private static readonly Guid SeatTypeId = Guid.NewGuid();

        private Order sut;

        public given_placed_order()
        {
            this.sut = new Order(new[] {
                    new OrderPlaced
                        {
                            OrderId = OrderId,
                            ConferenceId = ConferenceId,
                            Seats = new[] { new SeatQuantity(SeatTypeId, 5) }
                        }
            });
        }

        [Fact]
        public void when_updating_seats_then_updates_order_with_new_seats()
        {
            this.sut.UpdateSeats(new[] { new OrderItem(SeatTypeId, 20) });

            var @event = (OrderUpdated)sut.Events.Single();
            Assert.Equal(OrderId, @event.OrderId);
            Assert.Equal(1, @event.Seats.Count);
            Assert.Equal(20, @event.Seats.ElementAt(0).Quantity);
        }

        [Fact]
        public void when_marking_a_subset_of_seats_as_reserved_then_order_is_partially_reserved()
        {
            this.sut.MarkAsReserved(DateTime.UtcNow.AddMinutes(15), new[] { new SeatQuantity(SeatTypeId, 3) });

            var @event = (OrderPartiallyReserved)sut.Events.Single();
            Assert.Equal(OrderId, @event.OrderId);
            Assert.Equal(1, @event.Seats.Count);
            Assert.Equal(3, @event.Seats.ElementAt(0).Quantity);
            //Assert.Equal(, @event.ReservationExpiration);
        }

        [Fact]
        public void when_marking_all_seats_as_reserved_then_order_is_reserved()
        {
            this.sut.MarkAsReserved(DateTime.UtcNow.AddMinutes(15), new[] { new SeatQuantity(SeatTypeId, 5) });

            var @event = (OrderReservationCompleted)sut.Events.Last();
            Assert.Equal(OrderId, @event.OrderId);
            Assert.Equal(1, @event.Seats.Count);
            Assert.Equal(5, @event.Seats.ElementAt(0).Quantity);
        }

        [Fact]
        public void when_expiring_order_then_notifies()
        {
            this.sut.Expire();

            var @event = (OrderExpired)sut.Events.Single();
            Assert.Equal(OrderId, @event.OrderId);
        }

        [Fact]
        public void when_assigning_registrant_information_then_raises_integration_event()
        {
            this.sut.AssignRegistrant("foo", "bar", "foo@bar.com");

            var @event = (OrderRegistrantAssigned)sut.Events.Single();
            Assert.Equal(OrderId, @event.OrderId);
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

        private Order sut;

        public given_fully_reserved_order()
        {
            this.sut = new Order(new IEvent[] {
                    new OrderPlaced
                        {
                            OrderId = OrderId,
                            ConferenceId = ConferenceId,
                            Seats = new[] { new SeatQuantity(SeatTypeId, 5) }
                        },
                    new OrderReservationCompleted { OrderId = OrderId }
                });
            // TODO: expiration?
        }

        [Fact]
        public void when_expiring_order_then_notifies()
        {
            this.sut.Expire();

            var @event = (OrderExpired)sut.Events.Single();
            Assert.Equal(OrderId, @event.OrderId);
        }

        [Fact]
        public void when_confirming_payment_then_notifies()
        {
            this.sut.ConfirmPayment();

            var @event = (OrderPaymentConfirmed)sut.Events.Single();
            Assert.Equal(OrderId, @event.OrderId);
        }
    }
}
