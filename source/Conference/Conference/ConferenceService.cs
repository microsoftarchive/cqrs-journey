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

namespace Conference
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Entity;
    using System.Diagnostics;
    using System.Linq;
    using Infrastructure.Messaging;
    using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.SqlAzure;
    using Microsoft.Practices.TransientFaultHandling;

    /// <summary>
    /// Transaction-script style domain service that manages 
    /// the interaction between the MVC controller and the 
    /// ORM persistence, as well as the publishing of integration 
    /// events.
    /// </summary>
    public class ConferenceService
    {
        private readonly IEventBus eventBus;
        private readonly string nameOrConnectionString;
        private readonly RetryPolicy<SqlAzureTransientErrorDetectionStrategy> retryPolicy;

        public ConferenceService(IEventBus eventBus, string nameOrConnectionString = "ConferenceManagement")
        {
            // NOTE: the database storage cannot be transactionally consistent with the 
            // event bus, so there is a chance that the conference state is saved 
            // to the database but the events are not published. The recommended 
            // mechanism to solve this lack of transaction support is to persist 
            // failed events to a table in the same database as the conference, in a 
            // queue that is retried until successful delivery of events is 
            // guaranteed. This mechanism has been implemented for the AzureEventSourcedRepository
            // and that implementation can be used as a guide to implement it here too.

            this.eventBus = eventBus;
            this.nameOrConnectionString = nameOrConnectionString;

            this.retryPolicy = new RetryPolicy<SqlAzureTransientErrorDetectionStrategy>(new Incremental(5, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1.5)) { FastFirstRetry = true });
            this.retryPolicy.Retrying += (s, e) =>
                Trace.TraceWarning("An error occurred in attempt number {1} to access the database in ConferenceService: {0}", e.LastException.Message, e.CurrentRetryCount);
 
        }

        public void CreateConference(ConferenceInfo conference)
        {
            using (var context = new ConferenceContext(this.nameOrConnectionString))
            {
                var existingSlug = this.retryPolicy.ExecuteAction(() => 
                    context.Conferences
                        .Where(c => c.Slug == conference.Slug)
                        .Select(c => c.Slug)
                        .Any());

                if (existingSlug)
                    throw new DuplicateNameException("The chosen conference slug is already taken.");

                // Conference publishing is explicit. 
                if (conference.IsPublished)
                    conference.IsPublished = false;

                context.Conferences.Add(conference);
                this.retryPolicy.ExecuteAction(() => context.SaveChanges());

                this.PublishConferenceEvent<ConferenceCreated>(conference);
            }
        }

        public void CreateSeat(Guid conferenceId, SeatType seat)
        {
            using (var context = new ConferenceContext(this.nameOrConnectionString))
            {
                var conference = this.retryPolicy.ExecuteAction(() => context.Conferences.Find(conferenceId));
                if (conference == null)
                    throw new ObjectNotFoundException();

                conference.Seats.Add(seat);
                this.retryPolicy.ExecuteAction(() => context.SaveChanges());

                // Don't publish new seats if the conference was never published 
                // (and therefore is not published either).
                if (conference.WasEverPublished)
                    this.PublishSeatCreated(conferenceId, seat);
            }
        }

        public ConferenceInfo FindConference(string slug)
        {
            using (var context = new ConferenceContext(this.nameOrConnectionString))
            {
                return this.retryPolicy.ExecuteAction(() => context.Conferences.FirstOrDefault(x => x.Slug == slug));
            }
        }

        public ConferenceInfo FindConference(string email, string accessCode)
        {
            using (var context = new ConferenceContext(this.nameOrConnectionString))
            {
                return this.retryPolicy.ExecuteAction(() => context.Conferences.FirstOrDefault(x => x.OwnerEmail == email && x.AccessCode == accessCode));
            }
        }

        public IEnumerable<SeatType> FindSeatTypes(Guid conferenceId)
        {
            using (var context = new ConferenceContext(this.nameOrConnectionString))
            {
                return this.retryPolicy.ExecuteAction(() => 
                    context.Conferences
                        .Include(x => x.Seats)
                        .Where(x => x.Id == conferenceId)
                        .Select(x => x.Seats)
                        .FirstOrDefault()) ??
                    Enumerable.Empty<SeatType>();
            }
        }

        public SeatType FindSeatType(Guid seatTypeId)
        {
            using (var context = new ConferenceContext(this.nameOrConnectionString))
            {
                return this.retryPolicy.ExecuteAction(() => context.Seats.Find(seatTypeId));
            }
        }

        public IEnumerable<Order> FindOrders(Guid conferenceId)
        {
            using (var context = new ConferenceContext(this.nameOrConnectionString))
            {
                return this.retryPolicy.ExecuteAction(() => context.Orders.Include("Seats.SeatInfo")
                    .Where(x => x.ConferenceId == conferenceId)
                    .ToList());
            }
        }

        public void UpdateConference(ConferenceInfo conference)
        {
            using (var context = new ConferenceContext(this.nameOrConnectionString))
            {
                var existing = this.retryPolicy.ExecuteAction(() => context.Conferences.Find(conference.Id));
                if (existing == null)
                    throw new ObjectNotFoundException();

                context.Entry(existing).CurrentValues.SetValues(conference);
                this.retryPolicy.ExecuteAction(() => context.SaveChanges());

                this.PublishConferenceEvent<ConferenceUpdated>(conference);
            }
        }

        public void UpdateSeat(Guid conferenceId, SeatType seat)
        {
            using (var context = new ConferenceContext(this.nameOrConnectionString))
            {
                var existing = this.retryPolicy.ExecuteAction(() => context.Seats.Find(seat.Id));
                if (existing == null)
                    throw new ObjectNotFoundException();

                context.Entry(existing).CurrentValues.SetValues(seat);
                this.retryPolicy.ExecuteAction(() => context.SaveChanges());

                // Don't publish seat updates if the conference was never published 
                // (and therefore is not published either).
                if (this.retryPolicy.ExecuteAction(() => context.Conferences.Where(x => x.Id == conferenceId).Select(x => x.WasEverPublished).FirstOrDefault()))
                {
                    this.eventBus.Publish(new SeatUpdated
                    {
                        ConferenceId = conferenceId,
                        SourceId = seat.Id,
                        Name = seat.Name,
                        Description = seat.Description,
                        Price = seat.Price,
                        Quantity = seat.Quantity,
                    });
                }
            }
        }

        public void Publish(Guid conferenceId)
        {
            this.UpdatePublished(conferenceId, true);
        }

        public void Unpublish(Guid conferenceId)
        {
            this.UpdatePublished(conferenceId, false);
        }

        private void UpdatePublished(Guid conferenceId, bool isPublished)
        {
            using (var context = new ConferenceContext(this.nameOrConnectionString))
            {
                var conference = this.retryPolicy.ExecuteAction(() => context.Conferences.Find(conferenceId));
                if (conference == null)
                    throw new ObjectNotFoundException();

                conference.IsPublished = isPublished;
                if (isPublished && !conference.WasEverPublished)
                {
                    // This flags prevents any further seat type deletions.
                    conference.WasEverPublished = true;
                    this.retryPolicy.ExecuteAction(() => context.SaveChanges());

                    // We always publish events *after* saving to store.
                    // Publish all seats that were created before.
                    foreach (var seat in conference.Seats)
                    {
                        PublishSeatCreated(conference.Id, seat);
                    }
                }
                else
                {
                    this.retryPolicy.ExecuteAction(() => context.SaveChanges());
                }

                if (isPublished)
                    this.eventBus.Publish(new ConferencePublished { SourceId = conferenceId });
                else
                    this.eventBus.Publish(new ConferenceUnpublished { SourceId = conferenceId });
            }
        }

        public void DeleteSeat(Guid id)
        {
            using (var context = new ConferenceContext(this.nameOrConnectionString))
            {
                var seat = this.retryPolicy.ExecuteAction(() => context.Seats.Find(id));
                if (seat == null)
                    throw new ObjectNotFoundException();

                var wasPublished = this.retryPolicy.ExecuteAction(() => context.Conferences
                    .Where(x => x.Seats.Any(s => s.Id == id))
                    .Select(x => x.WasEverPublished)
                    .FirstOrDefault());

                if (wasPublished)
                    throw new InvalidOperationException("Can't delete seats from a conference that has been published at least once.");

                context.Seats.Remove(seat);
                this.retryPolicy.ExecuteAction(() => context.SaveChanges());
            }
        }

        private void PublishConferenceEvent<T>(ConferenceInfo conference)
            where T : ConferenceEvent, new()
        {
            this.eventBus.Publish(new T()
            {
                SourceId = conference.Id,
                Owner = new Owner
                {
                    Name = conference.OwnerName,
                    Email = conference.OwnerEmail,
                },
                Name = conference.Name,
                Description = conference.Description,
                Location = conference.Location,
                Slug = conference.Slug,
                Tagline = conference.Tagline,
                TwitterSearch = conference.TwitterSearch,
                StartDate = conference.StartDate,
                EndDate = conference.EndDate,
            });
        }

        private void PublishSeatCreated(Guid conferenceId, SeatType seat)
        {
            this.eventBus.Publish(new SeatCreated
            {
                ConferenceId = conferenceId,
                SourceId = seat.Id,
                Name = seat.Name,
                Description = seat.Description,
                Price = seat.Price,
                Quantity = seat.Quantity,
            });
        }
    }
}
