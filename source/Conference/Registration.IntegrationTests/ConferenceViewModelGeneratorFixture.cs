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

namespace Registration.Tests.ConferenceViewModelGeneratorFixture
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Conference;
    using Infrastructure.Messaging;
    using Moq;
    using Registration.Commands;
    using Registration.Handlers;
    using Registration.ReadModel;
    using Registration.ReadModel.Implementation;
    using Xunit;

    public class given_a_database : IDisposable
    {
        protected string dbName = Guid.NewGuid().ToString();
        protected ConferenceViewModelGenerator sut;
        protected List<ICommand> commands = new List<ICommand>();

        public given_a_database()
        {
            using (var context = new ConferenceRegistrationDbContext(dbName))
            {
                if (context.Database.Exists())
                    context.Database.Delete();

                context.Database.Create();
            }

            var bus = new Mock<ICommandBus>();
            bus.Setup(x => x.Send(It.IsAny<Envelope<ICommand>>()))
                .Callback<Envelope<ICommand>>(x => this.commands.Add(x.Body));
            bus.Setup(x => x.Send(It.IsAny<IEnumerable<Envelope<ICommand>>>()))
                .Callback<IEnumerable<Envelope<ICommand>>>(x => this.commands.AddRange(x.Select(e => e.Body)));

            this.sut = new ConferenceViewModelGenerator(() => new ConferenceRegistrationDbContext(dbName), bus.Object);
        }

        public void Dispose()
        {
            using (var context = new ConferenceRegistrationDbContext(dbName))
            {
                if (context.Database.Exists())
                    context.Database.Delete();
            }
        }
    }

    public class given_no_conference : given_a_database
    {
        [Fact]
        public void when_conference_created_then_conference_dto_populated()
        {
            var conferenceId = Guid.NewGuid();

            this.sut.Handle(new ConferenceCreated
            {
                Name = "name",
                Description = "description",
                Slug = "test",
                Owner = new Owner
                {
                    Name = "owner",
                    Email = "owner@email.com",
                },
                SourceId = conferenceId,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date,
            });

            using (var context = new ConferenceRegistrationDbContext(dbName))
            {
                var dto = context.Find<ConferenceDTO>(conferenceId);

                Assert.NotNull(dto);
                Assert.Equal("name", dto.Name);
                Assert.Equal("description", dto.Description);
                Assert.Equal("test", dto.Code);
                Assert.Equal(0, dto.Seats.Count);
            }
        }
    }

    public class given_existing_conference : given_a_database
    {
        private Guid conferenceId = Guid.NewGuid();

        public given_existing_conference()
        {
            this.sut.Handle(new ConferenceCreated
            {
                SourceId = conferenceId,
                Name = "name",
                Description = "description",
                Slug = "test",
                Owner = new Owner
                {
                    Name = "owner",
                    Email = "owner@email.com",
                },
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date,
            });
        }

        [Fact]
        public void when_conference_updated_then_conference_dto_populated()
        {
            var startDate = new DateTimeOffset(2012, 04, 20, 15, 0, 0, TimeSpan.FromHours(-8));
            this.sut.Handle(new ConferenceUpdated
            {
                Name = "newname",
                Description = "newdescription",
                Slug = "newtest",
                Owner = new Owner
                {
                    Name = "owner",
                    Email = "owner@email.com",
                },
                SourceId = conferenceId,
                StartDate = startDate.UtcDateTime,
                EndDate = DateTime.UtcNow.Date,
            });

            using (var context = new ConferenceRegistrationDbContext(dbName))
            {
                var dto = context.Find<ConferenceDTO>(conferenceId);

                Assert.NotNull(dto);
                Assert.Equal("newname", dto.Name);
                Assert.Equal("newdescription", dto.Description);
                Assert.Equal("newtest", dto.Code);
                Assert.Equal(startDate, dto.StartDate);
                Assert.Equal(0, dto.Seats.Count);
            }
        }

        [Fact]
        public void when_seat_created_then_adds_seat_to_conference_dto()
        {
            var seatId = Guid.NewGuid();

            this.sut.Handle(new SeatCreated
            {
                ConferenceId = conferenceId,
                SourceId = seatId,
                Name = "seat",
                Description = "description",
                Price = 200,
            });

            using (var context = new ConferenceRegistrationDbContext(dbName))
            {
                var dto = context.Set<ConferenceDTO>()
                    .Where(x => x.Id == conferenceId)
                    .SelectMany(x => x.Seats)
                    .FirstOrDefault(x => x.Id == seatId);

                Assert.NotNull(dto);
                Assert.Equal("seat", dto.Name);
                Assert.Equal("description", dto.Description);
                Assert.Equal(200, dto.Price);
            }

        }

        [Fact]
        public void when_seat_created_then_add_seats_command_sent()
        {
            var seatId = Guid.NewGuid();

            this.sut.Handle(new SeatCreated
            {
                ConferenceId = conferenceId,
                SourceId = seatId,
                Name = "seat",
                Description = "description",
                Price = 200,
                Quantity = 100,
            });

            var e = this.commands.OfType<AddSeats>().FirstOrDefault();

            Assert.NotNull(e);
            Assert.Equal(conferenceId, e.ConferenceId);
            Assert.Equal(seatId, e.SeatType);
            Assert.Equal(100, e.Quantity);
        }

        [Fact]
        public void when_seat_updated_then_updates_seat_on_conference_dto()
        {
            var seatId = Guid.NewGuid();

            this.sut.Handle(new SeatCreated
            {
                ConferenceId = conferenceId,
                SourceId = seatId,
                Name = "seat",
                Description = "description",
                Price = 200,
            });

            this.sut.Handle(new SeatUpdated
            {
                ConferenceId = conferenceId,
                SourceId = seatId,
                Name = "newseat",
                Description = "newdescription",
                Price = 100,
            });

            using (var context = new ConferenceRegistrationDbContext(dbName))
            {
                var dto = context.Set<ConferenceDTO>()
                    .Where(x => x.Id == conferenceId)
                    .SelectMany(x => x.Seats)
                    .FirstOrDefault(x => x.Id == seatId);

                Assert.NotNull(dto);
                Assert.Equal("newseat", dto.Name);
                Assert.Equal("newdescription", dto.Description);
                Assert.Equal(100, dto.Price);
            }
        }

        [Fact]
        public void when_seats_added_then_add_seats_command_sent()
        {
            var seatId = Guid.NewGuid();

            this.sut.Handle(new SeatCreated
            {
                ConferenceId = conferenceId,
                SourceId = seatId,
                Name = "seat",
                Description = "description",
                Price = 200,
                Quantity = 100,
            });

            this.commands.Clear();

            this.sut.Handle(new SeatUpdated
            {
                ConferenceId = conferenceId,
                SourceId = seatId,
                Name = "newseat",
                Description = "newdescription",
                Price = 100,
                Quantity = 200,
            });

            var e = this.commands.OfType<AddSeats>().FirstOrDefault();

            Assert.NotNull(e);
            Assert.Equal(conferenceId, e.ConferenceId);
            Assert.Equal(seatId, e.SeatType);
            Assert.Equal(100, e.Quantity);
        }

        [Fact]
        public void when_seats_removed_then_add_seats_command_sent()
        {
            var seatId = Guid.NewGuid();

            this.sut.Handle(new SeatCreated
            {
                ConferenceId = conferenceId,
                SourceId = seatId,
                Name = "seat",
                Description = "description",
                Price = 200,
                Quantity = 100,
            });

            this.commands.Clear();

            this.sut.Handle(new SeatUpdated
            {
                ConferenceId = conferenceId,
                SourceId = seatId,
                Name = "newseat",
                Description = "newdescription",
                Price = 100,
                Quantity = 50,
            });

            var e = this.commands.OfType<RemoveSeats>().FirstOrDefault();

            Assert.NotNull(e);
            Assert.Equal(conferenceId, e.ConferenceId);
            Assert.Equal(seatId, e.SeatType);
            Assert.Equal(50, e.Quantity);
        }
    }
}
