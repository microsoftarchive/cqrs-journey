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

namespace SeatAssignment.Tests.OrderHandlerFixture
{
    using System;
    using System.Linq;
    using Moq;
    using Registration;
    using Registration.Events;
    using Xunit;

    public class given_no_order_seats
    {
        [Fact]
        public void when_order_reservation_completed_then_populates_order_seats()
        {
            var dao = new Mock<IOrderSeatsDao>();
            OrderSeats seats = null;
            dao.Setup(x => x.SaveOrder(It.IsAny<OrderSeats>()))
                .Callback<OrderSeats>(x => seats = x);

            var handler = new OrderHandler(() => dao.Object);
            handler.Handle(new OrderReservationCompleted
            {
                Seats = new[]
                {
                    new SeatQuantity(Guid.Empty, 10), 
                }
            });

            Assert.NotNull(seats);
            Assert.Equal(10, seats.Seats.Count);
            Assert.True(seats.Seats.All(s => s.SeatType == Guid.Empty));
        }
    }

    public class given_existing_order_seats
    {
        private Mock<IOrderSeatsDao> dao;
        private OrderSeats seats;

        public given_existing_order_seats()
        {
            this.dao = new Mock<IOrderSeatsDao>();
            this.dao.Setup(x => x.SaveOrder(It.IsAny<OrderSeats>()))
                .Callback<OrderSeats>(x => this.seats = x);
            this.dao.Setup(x => x.FindOrder(It.IsAny<Guid>(), false))
                .Returns<Guid, bool>((id, confirmed) => this.seats == null ? null :
                    (id == this.seats.OrderId ? this.seats : null));

            new OrderHandler(() => dao.Object)
                .Handle(new OrderReservationCompleted
                {
                    SourceId = Guid.NewGuid(),
                    Seats = new[]
                    {
                        new SeatQuantity(Guid.NewGuid(), 10), 
                        new SeatQuantity(Guid.NewGuid(), 20), 
                    }
                });
        }

        [Fact]
        public void then_contains_reserved_seats()
        {
            Assert.Equal(30, this.seats.Seats.Count);
        }

        [Fact]
        public void when_order_reservation_completed_then_replaces_existing_seats()
        {
            var dao = new Mock<IOrderSeatsDao>();
            OrderSeats seats = null;
            dao.Setup(x => x.SaveOrder(It.IsAny<OrderSeats>()))
                .Callback<OrderSeats>(x => seats = x);

            var handler = new OrderHandler(() => dao.Object);
            handler.Handle(new OrderReservationCompleted
            {
                Seats = new[]
                {
                    new SeatQuantity(Guid.Empty, 10), 
                }
            });

            Assert.NotNull(seats);
            Assert.Equal(10, seats.Seats.Count);
            Assert.True(seats.Seats.All(s => s.SeatType == Guid.Empty));
        }

        [Fact]
        public void when_order_confirmed_but_no_previous_reservation_then_throws()
        {
            var handler = new OrderHandler(() => this.dao.Object);

            Assert.Throws<InvalidOperationException>(() =>
                handler.Handle(new OrderPaymentConfirmed { SourceId = Guid.NewGuid() }));
        }

        [Fact]
        public void when_order_confirmed_then_order_seats_become_confirmed()
        {
            new OrderHandler(() => this.dao.Object)
                .Handle(new OrderPaymentConfirmed { SourceId = this.seats.OrderId });

            Assert.True(this.seats.IsOrderConfirmed);
        }

        [Fact]
        public void when_reservation_completed_for_confirmed_order_then_throws()
        {
            var handler = new OrderHandler(() => this.dao.Object);
            handler.Handle(new OrderPaymentConfirmed { SourceId = this.seats.OrderId });

            Assert.Throws<InvalidOperationException>(() =>
                handler.Handle(new OrderReservationCompleted { SourceId = this.seats.OrderId }));
        }
    }
}
