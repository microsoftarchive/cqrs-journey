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

namespace Conference.Tests.ConferenceServiceTests
{
    using System;
    using System.Data;
    using System.Data.Entity;
    using System.Linq;
    using Common;
    using Xunit;

    public class given_no_conference
    {
        private MemoryEventBus bus;
        private ConferenceService service;

        static given_no_conference()
        {
            Database.SetInitializer(new DropCreateDatabaseAlways<ConferenceContext>());
        }

        public given_no_conference()
        {
            this.bus = new MemoryEventBus();
            this.service = new ConferenceService(this.bus);
        }

        [Fact]
        public void when_finding_by_non_existing_slug_then_returns_null()
        {
            var conference = service.FindConference(Guid.NewGuid().ToString());

            Assert.Null(conference);
        }

        [Fact]
        public void when_finding_by_non_existing_email_and_access_code_then_returns_null()
        {
            var conference = service.FindConference("foo@bar.com", Guid.NewGuid().ToString());

            Assert.Null(conference);
        }

        [Fact]
        public void when_finding_seats_by_non_existing_conference_id_then_returns_empty()
        {
            var conference = service.FindSeats(Guid.NewGuid());

            Assert.Empty(conference);
        }

        [Fact]
        public void when_creating_seat_then_throws()
        {
            Assert.Throws<ObjectNotFoundException>(() => service.CreateSeat(Guid.NewGuid(), new SeatInfo()));
        }

        [Fact]
        public void when_updating_non_existing_conference_then_throws()
        {
            Assert.Throws<ObjectNotFoundException>(() => service.UpdateConference(new ConferenceInfo()));
        }

        [Fact]
        public void when_updating_non_existing_seat_then_throws()
        {
            Assert.Throws<ObjectNotFoundException>(() => service.UpdateSeat(new SeatInfo()));
        }

        [Fact]
        public void when_updating_published_non_existing_conference_then_throws()
        {
            Assert.Throws<ObjectNotFoundException>(() => service.UpdatePublished(Guid.NewGuid(), false));
        }

        [Fact]
        public void when_deleting_non_existing_seat_then_throws()
        {
            Assert.Throws<ObjectNotFoundException>(() => service.DeleteSeat(Guid.NewGuid()));
        }
    }

    public class given_an_existing_conference_with_a_seat : IDisposable
    {
        private string dbName = Guid.NewGuid().ToString();
        private ConferenceInfo conference;
        private MemoryEventBus bus;
        private ConferenceService service;

        static given_an_existing_conference_with_a_seat()
        {
            Database.SetInitializer(new DropCreateDatabaseAlways<ConferenceContext>());
        }

        public given_an_existing_conference_with_a_seat()
        {
            using (var context = new ConferenceContext(dbName))
            {
                if (context.Database.Exists())
                    context.Database.Delete();

                context.Database.CreateIfNotExists();
            }

            this.bus = new MemoryEventBus();
            this.service = new ConferenceService(this.bus, this.dbName);
            this.conference = new ConferenceInfo
            {
                OwnerEmail = "test@contoso.com",
                OwnerName = "test owner",
                AccessCode = "qwerty",
                Name = "test conference",
                Description = "test conference description",
                Slug = "test",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.Add(TimeSpan.FromDays(2)),
                IsPublished = true,
                Seats = 
                {
                    new SeatInfo
                    {
                        Name = "general", 
                        Description = "general description", 
                        Price = 100, 
                        Quantity = 10,
                    }
                }
            };
            service.CreateConference(this.conference);
        }

        [Fact]
        public void then_conference_is_persisted()
        {
            using (var context = new ConferenceContext(this.dbName))
            {
                Assert.NotNull(context.Conferences.Find(this.conference.Id));
            }
        }

        [Fact]
        public void then_conference_created_is_published()
        {
            var e = bus.Events.OfType<ConferenceCreated>().Single();

            Assert.NotNull(e);
            Assert.Equal(this.conference.Id, e.SourceId);
        }

        [Fact]
        public void then_seat_created_is_published()
        {
            var e = bus.Events.OfType<SeatCreated>().Single();
            var seat = this.conference.Seats.Single();

            Assert.Equal(seat.Id, e.SourceId);
            Assert.Equal(seat.Name, e.Name);
            Assert.Equal(seat.Description, e.Description);
            Assert.Equal(seat.Price, e.Price);
        }

        [Fact]
        public void then_seats_added_is_published()
        {
            var e = bus.Events.OfType<SeatsAdded>().Single();
            var seat = this.conference.Seats.Single();

            Assert.Equal(seat.Id, e.SourceId);
            Assert.Equal(seat.Quantity, e.AddedQuantity);
            Assert.Equal(seat.Quantity, e.TotalQuantity);
        }

        [Fact]
        public void when_finding_by_slug_then_returns_conference()
        {
            var conference = service.FindConference(this.conference.Slug);

            Assert.NotNull(conference);
        }

        [Fact]
        public void when_finding_by_existing_email_and_access_code_then_returns_conference()
        {
            var conference = service.FindConference(this.conference.OwnerEmail, this.conference.AccessCode);

            Assert.NotNull(conference);
        }

        [Fact]
        public void when_finding_seats_by_non_existing_conference_id_then_returns_empty()
        {
            var conference = service.FindSeats(this.conference.Id);

            Assert.NotEmpty(conference);
        }

        [Fact]
        public void when_creating_conference_with_existing_slug_then_throws()
        {
            this.conference.Id = Guid.NewGuid();

            Assert.Throws<DuplicateNameException>(() => service.CreateConference(this.conference));
        }

        [Fact]
        public void when_creating_conference_then_new_id_is_assigned()
        {
            this.conference.Id = Guid.NewGuid();
            this.conference.Slug = "asdfgh";
            this.conference.Seats.Clear();
            var existingId = this.conference.Id;

            var newId = service.CreateConference(this.conference);

            Assert.NotEqual(existingId, this.conference.Id);
            Assert.Equal(newId, this.conference.Id);
        }

        [Fact]
        public void when_creating_seat_then_adds_to_conference()
        {
            var seat = new SeatInfo
            {
                Name = "precon",
                Description = "precon desc",
                Price = 100,
                Quantity = 100,
            };

            service.CreateSeat(this.conference.Id, seat);

            var seats = service.FindSeats(this.conference.Id);

            Assert.Equal(2, seats.Count());
        }

        [Fact]
        public void when_creating_seat_then_seat_created_is_published()
        {
            var seat = new SeatInfo
            {
                Name = "precon",
                Description = "precon desc",
                Price = 100,
                Quantity = 100,
            };

            var id = service.CreateSeat(this.conference.Id, seat);

            var e = bus.Events.OfType<SeatCreated>().Single(x => x.SourceId == id);

            Assert.Equal(id, e.SourceId);
            Assert.Equal(seat.Name, e.Name);
            Assert.Equal(seat.Description, e.Description);
            Assert.Equal(seat.Price, e.Price);
        }

        [Fact]
        public void when_creating_seat_then_seats_added_is_published()
        {
            var seat = new SeatInfo
            {
                Name = "precon",
                Description = "precon desc",
                Price = 100,
                Quantity = 100,
            };

            var id = service.CreateSeat(this.conference.Id, seat);

            var e = bus.Events.OfType<SeatsAdded>().Single(x => x.SourceId == id);

            Assert.Equal(seat.Quantity, e.AddedQuantity);
            Assert.Equal(seat.Quantity, e.TotalQuantity);
        }

        [Fact]
        public void when_creating_seat_then_sets_new_id()
        {
            var seat = new SeatInfo
            {
                Name = "precon",
                Description = "precon desc",
                Price = 100,
                Quantity = 100,
            };

            var existingId = seat.Id;

            var newId = service.CreateSeat(this.conference.Id, seat);

            Assert.NotEqual(existingId, seat.Id);
            Assert.Equal(newId, seat.Id);
        }

        [Fact]
        public void when_creating_seat_then_can_find_seat()
        {
            var seat = new SeatInfo
            {
                Name = "precon",
                Description = "precon desc",
                Price = 100,
                Quantity = 100,
            };

            service.CreateSeat(this.conference.Id, seat);

            Assert.NotNull(service.FindSeat(seat.Id));
        }

        [Fact]
        public void when_updating_conference_then_can_find_updated_information()
        {
            this.conference.Name = "foo";
            this.conference.Description = "bar";
            this.conference.Seats.Clear();

            service.UpdateConference(this.conference);

            var saved = service.FindConference(this.conference.Slug);

            Assert.Equal(this.conference.Name, saved.Name);
            Assert.Equal(this.conference.Description, saved.Description);
        }

        [Fact]
        public void when_updating_seat_then_can_find_updated_information()
        {
            var seat = this.conference.Seats.First();
            seat.Name = "precon";
            seat.Description = "precon desc";
            seat.Price = 200;

            service.UpdateSeat(seat);

            var saved = service.FindSeat(seat.Id);

            Assert.Equal(seat.Name, saved.Name);
            Assert.Equal(seat.Description, saved.Description);
            Assert.Equal(seat.Quantity, saved.Quantity);
        }

        [Fact]
        public void when_updating_seat_adds_then_seats_added_is_published()
        {
            var seat = this.conference.Seats.First();
            seat.Quantity += 100;

            service.UpdateSeat(seat);

            var e = bus.Events.OfType<SeatsAdded>().LastOrDefault();

            Assert.Equal(seat.Id, e.SourceId);
            Assert.Equal(seat.Quantity, e.TotalQuantity);
            Assert.Equal(100, e.AddedQuantity);
        }

        [Fact]
        public void when_updating_seat_removes_then_seats_removed_is_published()
        {
            var seat = this.conference.Seats.First();
            seat.Quantity -= 50;

            service.UpdateSeat(seat);

            var e = bus.Events.OfType<SeatsRemoved>().LastOrDefault();

            Assert.Equal(seat.Id, e.SourceId);
            Assert.Equal(seat.Quantity, e.TotalQuantity);
            Assert.Equal(50, e.RemovedQuantity);
        }

        [Fact]
        public void when_updating_published_then_updates_conference()
        {
            service.UpdatePublished(this.conference.Id, true);

            Assert.True(service.FindConference(this.conference.Slug).IsPublished);

            service.UpdatePublished(this.conference.Id, false);

            Assert.False(service.FindConference(this.conference.Slug).IsPublished);
        }

        [Fact]
        public void when_updating_published_then_sets_conference_ever_published()
        {
            service.UpdatePublished(this.conference.Id, true);

            Assert.True(service.FindConference(this.conference.Slug).WasEverPublished);
        }

        [Fact]
        public void when_updating_published_to_false_then_conference_ever_published_remains_true()
        {
            service.UpdatePublished(this.conference.Id, true);
            service.UpdatePublished(this.conference.Id, false);

            Assert.True(service.FindConference(this.conference.Slug).WasEverPublished);
        }

        [Fact]
        public void when_deleting_seat_then_updates_conference_seats()
        {
            Assert.Equal(1, service.FindSeats(this.conference.Id).Count());

            service.DeleteSeat(this.conference.Seats.First().Id);

            Assert.Equal(0, service.FindSeats(this.conference.Id).Count());
        }

        [Fact]
        public void when_deleting_seat_from_published_conference_then_throws()
        {
            service.UpdatePublished(this.conference.Id, true);

            Assert.Throws<InvalidOperationException>(() => service.DeleteSeat(this.conference.Seats.First().Id));
        }

        [Fact]
        public void when_deleting_seat_from_previously_published_conference_then_throws()
        {
            service.UpdatePublished(this.conference.Id, true);
            service.UpdatePublished(this.conference.Id, false);

            Assert.Throws<InvalidOperationException>(() => service.DeleteSeat(this.conference.Seats.First().Id));
        }

        public void Dispose()
        {
            using (var context = new ConferenceContext(dbName))
            {
                if (context.Database.Exists())
                    context.Database.Delete();
            }
        }
    }
}