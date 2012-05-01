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

namespace SeatAssignment.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Infrastructure.Messaging;
    using Moq;
    using SeatAssignment.Events;
    using Xunit;

    public class OrderSeatsServiceFixture
    {
        private IEventBus bus;
        private List<IEvent> events;
        private Mock<IOrderSeatsDao> dao;

        public OrderSeatsServiceFixture()
        {
            this.events = new List<IEvent>();
            var bus = new Mock<IEventBus>();
            bus.Setup(x => x.Publish(It.IsAny<IEvent>()))
                .Callback<IEvent>(e => this.events.Add(e));
            bus.Setup(x => x.Publish(It.IsAny<IEnumerable<IEvent>>()))
                .Callback<IEnumerable<IEvent>>(e => this.events.AddRange(e));

            this.bus = bus.Object;
            this.dao = new Mock<IOrderSeatsDao>();
        }

        [Fact]
        public void when_finding_order_then_finds_confirmed_only()
        {
            var service = new OrderSeatsService(() => this.dao.Object, this.bus);
            var orderId = Guid.NewGuid();

            service.FindOrder(orderId);

            this.dao.Verify(x => x.FindOrder(orderId, true));
        }

        [Fact]
        public void when_updating_non_existent_seat_then_throws()
        {
            var service = new OrderSeatsService(() => this.dao.Object, this.bus);
            var seat = new Seat { Id = Guid.NewGuid(), SeatType = Guid.NewGuid() };

            Assert.Throws<ArgumentException>(() => service.UpdateSeat(seat));
        }

        [Fact]
        public void when_new_attendee_assigned_then_publishes_attendee_added_event()
        {
            var service = new OrderSeatsService(() => this.dao.Object, this.bus);
            var saved = new Seat { Id = Guid.NewGuid(), SeatType = Guid.NewGuid() };
            this.dao.Setup(x => x.FindSeat(saved.Id)).Returns(saved);

            var seat = new Seat
            {
                Id = saved.Id,
                SeatType = saved.SeatType,
                Email = "example@contoso.com",
                FirstName = "Hello",
                LastName = "World",
            };

            service.UpdateSeat(seat);

            this.dao.Verify(x => x.UpdateSeat(seat));

            var e = this.events.OfType<SeatAssignmentAdded>().FirstOrDefault();
            Assert.NotNull(e);
            Assert.Equal("example@contoso.com", e.Email);
            Assert.Equal("Hello", e.FirstName);
            Assert.Equal("World", e.LastName);
        }

        [Fact]
        public void when_attendee_information_updated_then_publishes_attendee_updated_event()
        {
            var service = new OrderSeatsService(() => this.dao.Object, this.bus);
            var saved = new Seat
            {
                Id = Guid.NewGuid(),
                SeatType = Guid.NewGuid(),
                Email = "example@contoso.com",
                FirstName = "test",
                LastName = "test",
            };

            this.dao.Setup(x => x.FindSeat(saved.Id)).Returns(saved);

            var seat = new Seat
            {
                Id = saved.Id,
                SeatType = saved.SeatType,
                Email = "example@contoso.com",
                FirstName = "Hello",
                LastName = "World",
            };

            service.UpdateSeat(seat);

            this.dao.Verify(x => x.UpdateSeat(seat));

            var e = this.events.OfType<AttendeeUpdated>().FirstOrDefault();
            Assert.NotNull(e);
            Assert.Equal("Hello", e.FirstName);
            Assert.Equal("World", e.LastName);
        }

        [Fact]
        public void when_new_attendee_assigned_to_already_assigned_seat_then_publishes_attendee_removed_and_added_event()
        {
            var service = new OrderSeatsService(() => this.dao.Object, this.bus);
            var saved = new Seat
            {
                Id = Guid.NewGuid(),
                SeatType = Guid.NewGuid(),
                Email = "test@contoso.com",
                FirstName = "test",
                LastName = "test",
            };

            this.dao.Setup(x => x.FindSeat(saved.Id)).Returns(saved);

            var seat = new Seat
            {
                Id = saved.Id,
                SeatType = saved.SeatType,
                Email = "example@contoso.com",
                FirstName = "Hello",
                LastName = "World",
            };

            service.UpdateSeat(seat);

            this.dao.Verify(x => x.UpdateSeat(seat));

            var added = this.events.OfType<SeatAssignmentAdded>().FirstOrDefault();
            Assert.NotNull(added);
            Assert.Equal("example@contoso.com", added.Email);
            Assert.Equal("Hello", added.FirstName);
            Assert.Equal("World", added.LastName);

            var removed = this.events.OfType<AttendeeRemoved>().FirstOrDefault();
            Assert.NotNull(removed);
            Assert.Equal("test@contoso.com", removed.Email);
            Assert.Equal("test", removed.FirstName);
            Assert.Equal("test", removed.LastName);
        }
    }
}
