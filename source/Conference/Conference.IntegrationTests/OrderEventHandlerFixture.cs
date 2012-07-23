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

namespace Conference.IntegrationTests.OrderEventHandlerFixture
{
    using System;
    using System.Data.Entity;
    using System.Linq;
    using Conference.IntegrationTests.ConferenceServiceFixture;
    using Registration.Events;
    using Xunit;

    public class given_no_order : IDisposable
    {
        protected string dbName = "OrderEventHandlerFixture_" + Guid.NewGuid().ToString();
        protected OrderEventHandler sut;

        public given_no_order()
        {
            using (var context = new ConferenceContext(dbName))
            {
                if (context.Database.Exists())
                    context.Database.Delete();

                context.Database.Create();
            }

            this.sut = new OrderEventHandler(() => new ConferenceContext(dbName));
        }

        public void Dispose()
        {
            using (var context = new ConferenceContext(dbName))
            {
                if (context.Database.Exists())
                    context.Database.Delete();
            }
        }

        [Fact]
        public void when_order_placed_then_creates_order_entity()
        {
            var e = new OrderPlaced
            {
                ConferenceId = Guid.NewGuid(),
                SourceId = Guid.NewGuid(),
                AccessCode = "asdf",
            };

            this.sut.Handle(e);

            using (var context = new ConferenceContext(dbName))
            {
                var order = context.Orders.Find(e.SourceId);

                Assert.NotNull(order);
            }
        }
    }

    public class given_an_order : given_an_existing_conference_with_a_seat
    {
        private OrderPlaced placed;
        private OrderEventHandler sut;

        public given_an_order()
        {
            this.placed = new OrderPlaced
            {
                ConferenceId = Guid.NewGuid(),
                SourceId = Guid.NewGuid(),
                AccessCode = "asdf",
            };

            this.sut = new OrderEventHandler(() => new ConferenceContext(dbName));
            this.sut.Handle(placed);
        }

        [Fact]
        public void when_order_totals_calculated_then_updates_order_total()
        {
            var e = new OrderExpired { SourceId = placed.SourceId };

            this.sut.Handle(e);

            var order = FindOrder(e.SourceId);
            Assert.Null(order);
        }

        [Fact]
        public void when_order_expired_then_deletes_entity()
        {
            var e = new OrderTotalsCalculated
            {
                SourceId = placed.SourceId,
                Total = 10,
            };

            this.sut.Handle(e);

            var order = FindOrder(e.SourceId);

            Assert.Equal(e.Total, order.TotalAmount);
        }

        [Fact]
        public void when_order_registrant_assigned_then_sets_registrant()
        {
            var e = new OrderRegistrantAssigned
            {
                SourceId = placed.SourceId,
                Email = "test@contoso.com",
                FirstName = "A",
                LastName = "Z",
            };

            this.sut.Handle(e);

            var order = FindOrder(e.SourceId);

            Assert.Equal(e.Email, order.RegistrantEmail);
            Assert.Contains("A", order.RegistrantName);
            Assert.Contains("Z", order.RegistrantName);
        }


        [Fact]
        public void when_order_confirmed_then_confirms_order()
        {
            var e = new OrderConfirmed
            {
                SourceId = placed.SourceId,
            };

            this.sut.Handle(e);

            var order = FindOrder(e.SourceId);

            Assert.Equal(Order.OrderStatus.Paid, order.Status);
        }

        [Fact]
        public void when_seat_assigned_then_adds_order_seat()
        {
            this.sut.Handle(new SeatAssignmentsCreated { SourceId = placed.SourceId, OrderId = placed.SourceId });

            var e = new SeatAssigned(placed.SourceId)
            {
                Attendee = new Registration.PersonalInfo
                {
                    Email = "test@contoso.com",
                    FirstName = "A",
                    LastName = "Z",
                },
                SeatType = this.conference.Seats.First().Id,
            };

            this.sut.Handle(e);

            var order = FindOrder(e.SourceId);

            Assert.Equal(1, order.Seats.Count);
        }

        [Fact]
        public void when_seat_asignee_updated_then_updates_order_seat()
        {
            this.sut.Handle(new SeatAssignmentsCreated { SourceId = placed.SourceId, OrderId = placed.SourceId });

            var e = new SeatAssigned(placed.SourceId)
            {
                Attendee = new Registration.PersonalInfo
                {
                    Email = "test@contoso.com",
                    FirstName = "A",
                    LastName = "Z",
                },
                SeatType = this.conference.Seats.First().Id,
            };

            this.sut.Handle(e);

            e.Attendee.LastName = "B";

            this.sut.Handle(e);

            var order = FindOrder(e.SourceId);

            Assert.Equal(1, order.Seats.Count);
            Assert.Equal("B", order.Seats.First().Attendee.LastName);
        }

        private Order FindOrder(Guid orderId)
        {
            using (var context = new ConferenceContext(dbName))
            {
                return context.Orders.Include(x => x.Seats).FirstOrDefault(x => x.Id == orderId);
            }
        }

    }
}
