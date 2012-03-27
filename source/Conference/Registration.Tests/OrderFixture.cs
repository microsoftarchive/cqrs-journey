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
        private static readonly Guid TicketTypeId = Guid.NewGuid();

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
            Assert.Equal(1, @event.Items.Count);
            Assert.Equal(5, @event.Items.ElementAt(0).Quantity);
        }

        private void PlaceOrder()
        {
            var lines = new[] { new OrderItem(TicketTypeId, 5) };
            this.sut = new Order(OrderId, ConferenceId, lines);
        }
    }

    public class given_placed_order
    {
        private static readonly Guid OrderId = Guid.NewGuid();
        private static readonly Guid ConferenceId = Guid.NewGuid();
        private static readonly Guid TicketTypeId = Guid.NewGuid();

        private Order sut;
        private IPersistenceProvider sutProvider;

        protected given_placed_order(IPersistenceProvider sutProvider)
        {
            this.sutProvider = sutProvider;

            var lines = new[] { new OrderItem(TicketTypeId, 5) };
            this.sut = new Order(OrderId, ConferenceId, lines);

            this.sut = this.sutProvider.PersistReload(this.sut);
        }

        public given_placed_order()
            : this(new NoPersistenceProvider())
        {
        }

        [Fact]
        public void when_marking_as_booked_then_changes_order_state()
        {
            this.sut.MarkAsBooked();

            Assert.Equal(Order.States.Booked, this.sut.State);
        }

        [Fact]
        public void when_marking_as_rejected_then_changes_order_state()
        {
            this.sut.Reject();

            Assert.Equal(Order.States.Rejected, this.sut.State);
        }
    }
}
