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
    using Registration.Events;
    using Registration.Tests;
    using Xunit;

    public class given_no_order
    {
        private static readonly Guid OrderId = Guid.NewGuid();
        private static readonly Guid ConferenceId = Guid.NewGuid();
        private static readonly Guid SeatTypeId = Guid.NewGuid();

        private Order sut;
        private IPersistenceProvider sutProvider;

        protected given_no_order(IPersistenceProvider sutProvider)
        {
            this.sutProvider = sutProvider;
        }

        public given_no_order()
            : this(new NoPersistenceProvider())
        {
        }

        [Fact]
        public void when_placing_order_then_state_is_created()
        {
            PlaceOrder();

            Assert.Equal(OrderId, sut.Id);
            Assert.Equal(Order.States.Created, sut.State);
            Assert.Equal(null, sut.ReservationExpirationDate);
        }

        [Fact]
        public void when_placing_order_then_raises_integration_event()
        {
            PlaceOrder();

            Assert.Single(sut.Events);
            Assert.Equal(OrderId, ((OrderPlaced)sut.Events.Single()).OrderId);
        }

        [Fact]
        public void when_placing_order_then_raises_integration_event_with_full_detauls()
        {
            PlaceOrder();

            var @event = (OrderPlaced)sut.Events.Single();
            Assert.Equal(OrderId, @event.OrderId);
            Assert.Equal(ConferenceId, @event.ConferenceId);
            Assert.Equal(1, @event.Seats.Count);
            Assert.Equal(5, @event.Seats.ElementAt(0).Quantity);
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
        private IPersistenceProvider sutProvider;

        protected given_placed_order(IPersistenceProvider sutProvider)
        {
            this.sutProvider = sutProvider;

            var seats = new[] { new OrderItem(SeatTypeId, 5) };
            this.sut = new Order(OrderId, ConferenceId, seats);

            this.sut = this.sutProvider.PersistReload(this.sut);
        }

        public given_placed_order()
            : this(new NoPersistenceProvider())
        {
        }

        [Fact]
        public void when_updating_seats_then_raises_integration_event()
        {
            this.sut.UpdateSeats(new[] { new OrderItem(SeatTypeId, 20) });

            var @event = (OrderUpdated)sut.Events.Last();
            Assert.Equal(OrderId, @event.OrderId);
            Assert.Equal(1, @event.Seats.Count);
            Assert.Equal(20, @event.Seats.ElementAt(0).Quantity);
        }

        [Fact]
        public void when_making_partial_reservation_then_changes_order_state()
        {
            this.sut.MarkAsReserved(DateTime.UtcNow.AddMinutes(15), new[] { new SeatQuantity(SeatTypeId, 3) });

            Assert.Equal(Order.States.PartiallyReserved, this.sut.State);
        }

        [Fact]
        public void when_making_partial_reservation_then_raises_integration_event()
        {
            this.sut.MarkAsReserved(DateTime.UtcNow.AddMinutes(15), new[] { new SeatQuantity(SeatTypeId, 3) });

            var @event = (OrderPartiallyReserved)sut.Events.Last();
            Assert.Equal(OrderId, @event.OrderId);
            Assert.Equal(1, @event.Seats.Count);
            Assert.Equal(3, @event.Seats.ElementAt(0).Quantity);
        }

        [Fact]
        public void when_making_full_reservation_then_changes_order_state()
        {
            this.sut.MarkAsReserved(DateTime.UtcNow.AddMinutes(15), new[] { new SeatQuantity(SeatTypeId, 5) });

            Assert.Equal(Order.States.ReservationCompleted, this.sut.State);
        }

        [Fact]
        public void when_making_full_reservation_then_raises_integration_event()
        {
            this.sut.MarkAsReserved(DateTime.UtcNow.AddMinutes(15), new[] { new SeatQuantity(SeatTypeId, 5) });

            var @event = (OrderReservationCompleted)sut.Events.Last();
            Assert.Equal(OrderId, @event.OrderId);
            Assert.Equal(1, @event.Seats.Count);
            Assert.Equal(5, @event.Seats.ElementAt(0).Quantity);
        }

        [Fact]
        public void when_marking_as_rejected_then_changes_order_state()
        {
            this.sut.Reject();

            Assert.Equal(Order.States.Rejected, this.sut.State);
        }

        [Fact]
        public void when_assigning_registrant_information_then_raises_integration_event()
        {
            this.sut.AssignRegistrant("foo", "bar", "foo@bar.com");

            var @event = (OrderRegistrantAssigned)sut.Events.Last();
            Assert.Equal(OrderId, @event.OrderId);
            Assert.Equal("foo", @event.FirstName);
            Assert.Equal("bar", @event.LastName);
            Assert.Equal("foo@bar.com", @event.Email);
        }

    }

    public class given_fully_reserved_order
    {
        private static readonly Guid OrderId = Guid.NewGuid();
        private static readonly Guid ConferenceId = Guid.NewGuid();
        private static readonly Guid SeatTypeId = Guid.NewGuid();

        private Order sut;
        private IPersistenceProvider sutProvider;

        protected given_fully_reserved_order(IPersistenceProvider sutProvider)
        {
            this.sutProvider = sutProvider;

            var lines = new[] { new OrderItem(SeatTypeId, 5) };
            this.sut = new Order(OrderId, ConferenceId, lines);

            this.sut.MarkAsReserved(DateTime.UtcNow.AddMinutes(15), new[] { new SeatQuantity(SeatTypeId, 5) });

            this.sut = this.sutProvider.PersistReload(this.sut);
        }

        public given_fully_reserved_order()
            : this(new NoPersistenceProvider())
        {
        }

        [Fact]
        public void when_marking_as_rejected_then_changes_order_state()
        {
            this.sut.Reject();

            Assert.Equal(Order.States.Rejected, this.sut.State);
        }

        [Fact(Skip="Not implemented")]
        public void when_marking_as_rejected_then_resets_expiration()
        {
            this.sut.Reject();

            Assert.Equal(null, this.sut.ReservationExpirationDate);
        }
    }
}
