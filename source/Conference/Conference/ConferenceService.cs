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

namespace Conference
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Entity;
    using System.Linq;

    public class ConferenceService
    {
        // TODO: transactionally save to DB the outgoing events
        // and an async process should pick and push to the bus.

        // using (tx)
        // {
        //  DB save (state snapshot)
        //  DB queue (events) -> push to bus (async)
        // }

        public void CreateConference(ConferenceInfo conference)
        {
            using (var context = new DomainContext())
            {
                var existingSlug = context.Conferences
                    .Where(c => c.Slug == conference.Slug)
                    .Select(c => c.Slug)
                    .Any();

                if (existingSlug)
                    throw new DuplicateNameException("The chosen conference slug is already taken.");

                conference.Id = Guid.NewGuid();
                context.Conferences.Add(conference);
                context.SaveChanges();
            }
        }

        public void CreateSeat(Guid conferenceId, SeatInfo seat)
        {
            using (var context = new DomainContext())
            {
                var conference = context.Conferences.Find(conferenceId);
                if (conference == null)
                    throw new ObjectNotFoundException();

                seat.Id = Guid.NewGuid();
                conference.Seats.Add(seat);
                context.SaveChanges();
            }
        }

        public ConferenceInfo FindConference(string slug)
        {
            using (var context = new DomainContext())
            {
                return context.Conferences.FirstOrDefault(x => x.Slug == slug);
            }
        }

        public ConferenceInfo FindConference(string email, string accessCode)
        {
            using (var context = new DomainContext())
            {
                return context.Conferences.FirstOrDefault(x => x.OwnerEmail == email && x.AccessCode == accessCode);
            }
        }

        public IEnumerable<SeatInfo> FindSeats(Guid conferenceId)
        {
            using (var context = new DomainContext())
            {
                return context.Conferences.Include(x => x.Seats)
                    .Where(x => x.Id == conferenceId)
                    .Select(x => x.Seats)
                    .FirstOrDefault() ??
                    Enumerable.Empty<SeatInfo>();
            }
        }

        public SeatInfo FindSeat(Guid seatId)
        {
            using (var context = new DomainContext())
            {
                return context.Seats.Find(seatId);
            }
        }

        public void UpdateConference(ConferenceInfo conference)
        {
            using (var context = new DomainContext())
            {
                var existing = context.Conferences.Find(conference.Id);
                if (existing == null)
                    throw new ObjectNotFoundException();

                context.Entry(existing).CurrentValues.SetValues(conference);
                context.SaveChanges();
            }
        }

        public void UpdateSeat(SeatInfo seat)
        {
            using (var context = new DomainContext())
            {
                var existing = context.Seats.Find(seat.Id);
                if (existing == null)
                    throw new ObjectNotFoundException();

                context.Entry(existing).CurrentValues.SetValues(seat);
                context.SaveChanges();
            }
        }

        public void UpdatePublished(Guid conferenceId, bool isPublished)
        {
            using (var context = new DomainContext())
            {
                var conference = context.Conferences.Find(conferenceId);
                if (conference == null)
                    throw new ObjectNotFoundException();

                conference.IsPublished = isPublished;
                context.SaveChanges();
            }
        }

        public void DeleteSeat(Guid id)
        {
            using (var context = new DomainContext())
            {
                var seat = context.Seats.Find(id);
                if (seat == null)
                    throw new ObjectNotFoundException();

                context.Seats.Remove(seat);
                context.SaveChanges();
            }
        }
    }
}
