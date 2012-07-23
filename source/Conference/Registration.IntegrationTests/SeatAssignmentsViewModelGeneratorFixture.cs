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

namespace Registration.IntegrationTests.SeatAssignmentsViewModelGeneratorFixture
{
    using System;
    using System.Collections.Generic;
    using Infrastructure.Serialization;
    using Moq;
    using Registration.Events;
    using Registration.Handlers;
    using Registration.ReadModel;
    using Registration.ReadModel.Implementation;
    using Xunit;

    public class given_created_seat_assignments : given_a_read_model_database
    {
        private static readonly List<SeatTypeName> seatTypes = new List<SeatTypeName>
        {
            new SeatTypeName { Id = Guid.NewGuid(), Name= "General" }, 
            new SeatTypeName { Id = Guid.NewGuid(), Name= "Precon" }, 
        };

        protected static readonly Guid assignmentsId = Guid.NewGuid();
        protected static readonly Guid orderId = Guid.NewGuid();
        protected SeatAssignmentsViewModelGenerator sut;
        protected IOrderDao dao;

        public given_created_seat_assignments()
        {
            var conferenceDao = new Mock<IConferenceDao>();
            conferenceDao.Setup(x => x.GetSeatTypeNames(It.IsAny<IEnumerable<Guid>>()))
                .Returns(seatTypes);

            var blobs = new MemoryBlobStorage();
            this.dao = new OrderDao(() => new ConferenceRegistrationDbContext(dbName), blobs, new JsonTextSerializer());
            this.sut = new SeatAssignmentsViewModelGenerator(conferenceDao.Object, blobs, new JsonTextSerializer());

            this.sut.Handle(new SeatAssignmentsCreated
            {
                SourceId = assignmentsId,
                OrderId = orderId,
                Seats = new[]
                {
                    new SeatAssignmentsCreated.SeatAssignmentInfo { Position = 0, SeatType = seatTypes[0].Id },
                    new SeatAssignmentsCreated.SeatAssignmentInfo { Position = 1, SeatType = seatTypes[1].Id },
                }
            });
        }

        [Fact]
        public void then_creates_model_with_seat_names()
        {
            var dto = this.dao.FindOrderSeats(assignmentsId);

            Assert.NotNull(dto);
            Assert.Equal(dto.Seats.Count, 2);
            Assert.Equal(seatTypes[0].Name, dto.Seats[0].SeatName);
            Assert.Equal(0, dto.Seats[0].Position);
            Assert.Equal(seatTypes[1].Name, dto.Seats[1].SeatName);
            Assert.Equal(1, dto.Seats[1].Position);
        }

        [Fact]
        public void when_seat_assigned_then_sets_attendee()
        {
            this.sut.Handle(new SeatAssigned(assignmentsId)
            {
                Position = 0,
                Attendee = new PersonalInfo
                {
                    Email = "a@b.com",
                    FirstName = "a",
                    LastName = "b",
                }
            });

            var dto = this.dao.FindOrderSeats(assignmentsId);

            Assert.Equal("a@b.com", dto.Seats[0].Attendee.Email);
            Assert.Equal("a", dto.Seats[0].Attendee.FirstName);
            Assert.Equal("b", dto.Seats[0].Attendee.LastName);
        }

        [Fact]
        public void when_assigned_seat_unassigned_then_clears_attendee_info()
        {
            this.sut.Handle(new SeatAssigned(assignmentsId)
            {
                Position = 0,
                Attendee = new PersonalInfo
                {
                    Email = "a@b.com",
                    FirstName = "a",
                    LastName = "b",
                }
            });

            this.sut.Handle(new SeatUnassigned(assignmentsId) { Position = 0 });

            var dto = this.dao.FindOrderSeats(assignmentsId);

            Assert.Null(dto.Seats[0].Attendee.Email);
            Assert.Null(dto.Seats[0].Attendee.FirstName);
            Assert.Null(dto.Seats[0].Attendee.LastName);
        }

        [Fact]
        public void when_assigned_seat_updated_then_sets_attendee_info()
        {
            this.sut.Handle(new SeatAssigned(assignmentsId)
            {
                Position = 0,
                Attendee = new PersonalInfo
                {
                    Email = "a@b.com",
                    FirstName = "a",
                    LastName = "b",
                }
            });

            this.sut.Handle(new SeatAssignmentUpdated(assignmentsId)
            {
                Position = 0,
                Attendee = new PersonalInfo
                {
                    Email = "b@c.com",
                    FirstName = "b",
                    LastName = "c",
                }
            });

            var dto = this.dao.FindOrderSeats(assignmentsId);

            Assert.Equal("b@c.com", dto.Seats[0].Attendee.Email);
            Assert.Equal("b", dto.Seats[0].Attendee.FirstName);
            Assert.Equal("c", dto.Seats[0].Attendee.LastName);
        }
    }


}
