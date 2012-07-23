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

namespace Registration.Tests.SeatAssignmentsFixture
{
    using System;
    using System.Linq;
    using Conference.Common.Utils;
    using Infrastructure.EventSourcing;
    using Moq;
    using Registration.Commands;
    using Registration.Events;
    using Registration.Handlers;
    using Xunit;

    public class given_a_paid_order
    {
        private Guid orderId = Guid.NewGuid();
        private EventSourcingTestHelper<SeatAssignments> sut;
        private EventSourcingTestHelper<Order> orderHelper;

        public given_a_paid_order()
        {
            this.sut = new EventSourcingTestHelper<SeatAssignments>();
            this.orderHelper = new EventSourcingTestHelper<Order>();
            this.orderHelper.Setup(new OrderCommandHandler(this.orderHelper.Repository, Mock.Of<IPricingService>()));
            this.orderHelper.Given(
                new OrderPlaced
                {
                    SourceId = orderId,
                    ConferenceId = Guid.NewGuid(),
                    Seats = new[] 
                    {
                        new SeatQuantity(Guid.NewGuid(), 5), 
                        new SeatQuantity(Guid.NewGuid(), 10),
                    },
                    ReservationAutoExpiration = DateTime.UtcNow.AddDays(1),
                    AccessCode = HandleGenerator.Generate(6),
                },
                new OrderPaymentConfirmed { SourceId = orderId });

            this.sut.Setup(new SeatAssignmentsHandler(this.orderHelper.Repository, this.sut.Repository));
        }

        [Fact]
        public void when_order_confirmed_then_seats_assignments_created()
        {
            sut.When(new OrderConfirmed { SourceId = orderId });

            var @event = sut.ThenHasSingle<SeatAssignmentsCreated>();
            // We do not reuse the order id.
            Assert.NotEqual(orderId, @event.SourceId);
            Assert.Equal(orderId, @event.OrderId);
            Assert.Equal(15, @event.Seats.Count());
        }
    }

    public class given_seat_assignments
    {
        protected Guid assignmentsId = Guid.NewGuid();
        protected Guid orderId = Guid.NewGuid();
        protected Guid seatType = Guid.NewGuid();
        protected EventSourcingTestHelper<SeatAssignments> sut;

        public given_seat_assignments()
        {
            this.sut = new EventSourcingTestHelper<SeatAssignments>();
            this.sut.Setup(new SeatAssignmentsHandler(Mock.Of<IEventSourcedRepository<Order>>(), this.sut.Repository));
            this.sut.Given(new SeatAssignmentsCreated
            {
                SourceId = assignmentsId,
                OrderId = orderId,
                Seats = Enumerable.Range(0, 5).Select(i =>
                    new SeatAssignmentsCreated.SeatAssignmentInfo
                    {
                        Position = i,
                        SeatType = seatType,
                    })
            },
            new SeatAssigned(assignmentsId)
            {
                Position = 0,
                SeatType = seatType,
                Attendee = new PersonalInfo
                {
                    Email = "a@a.com",
                    FirstName = "A",
                    LastName = "Z",
                }
            });
        }

        [Fact]
        public void when_assigning_unassigned_seat_then_seat_is_assigned()
        {
            var command = new AssignSeat
            {
                SeatAssignmentsId = assignmentsId,
                Position = 1,
                Attendee = new PersonalInfo
                {
                    Email = "a@a.com",
                    FirstName = "A",
                    LastName = "Z",
                }
            };
            sut.When(command);

            var @event = sut.ThenHasSingle<SeatAssigned>();

            Assert.Equal(1, @event.Position);
            Assert.Equal(seatType, @event.SeatType);
            Assert.Equal(assignmentsId, @event.SourceId);
            Assert.Equal(command.Attendee, @event.Attendee);
        }

        [Fact]
        public void when_unassigning_seat_then_seat_is_unassigned()
        {
            var command = new UnassignSeat
            {
                SeatAssignmentsId = assignmentsId,
                Position = 0,
            };
            sut.When(command);

            var @event = sut.ThenHasSingle<SeatUnassigned>();

            Assert.Equal(0, @event.Position);
            Assert.Equal(assignmentsId, @event.SourceId);
        }

        [Fact]
        public void when_unassigning_already_unnassigned_seat_then_no_event_is_raised()
        {
            var command = new UnassignSeat
            {
                SeatAssignmentsId = assignmentsId,
                Position = 1,
            };
            sut.When(command);

            Assert.False(sut.Events.OfType<SeatUnassigned>().Any());
        }

        [Fact]
        public void when_assigning_previously_assigned_seat_to_new_email_then_reassigns_seat_with_two_events()
        {
            var command = new AssignSeat
            {
                SeatAssignmentsId = assignmentsId,
                Position = 0,
                Attendee = new PersonalInfo
                {
                    Email = "b@b.com",
                    FirstName = "B",
                    LastName = "Z",
                }
            };
            sut.When(command);

            var unassign = sut.ThenHasOne<SeatUnassigned>();

            Assert.Equal(0, unassign.Position);
            Assert.Equal(assignmentsId, unassign.SourceId);

            var assign = sut.ThenHasOne<SeatAssigned>();

            Assert.Equal(0, assign.Position);
            Assert.Equal(seatType, assign.SeatType);
            Assert.Equal(assignmentsId, assign.SourceId);
            Assert.Equal(command.Attendee, assign.Attendee);
        }

        [Fact]
        public void when_assigning_previously_assigned_seat_to_same_email_then_updates_assignment()
        {
            var command = new AssignSeat
            {
                SeatAssignmentsId = assignmentsId,
                Position = 0,
                Attendee = new PersonalInfo
                {
                    Email = "a@a.com",
                    FirstName = "B",
                    LastName = "Z",
                }
            };
            sut.When(command);

            var assign = sut.ThenHasSingle<SeatAssignmentUpdated>();

            Assert.Equal(0, assign.Position);
            Assert.Equal(assignmentsId, assign.SourceId);
            Assert.Equal(command.Attendee, assign.Attendee);
        }
    }
}
