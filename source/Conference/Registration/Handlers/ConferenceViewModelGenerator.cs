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

namespace Registration.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Diagnostics;
    using System.Linq;
    using Conference;
    using Infrastructure.EventSourcing;
    using Infrastructure.Messaging;
    using Infrastructure.Messaging.Handling;
    using Registration.Commands;
    using Registration.Events;
    using Registration.ReadModel;
    using Registration.ReadModel.Implementation;

    // TODO: should work correctly with out of order messages instead of dropping events!
    public class ConferenceViewModelGenerator :
        IEventHandler<ConferenceCreated>,
        IEventHandler<ConferenceUpdated>,
        IEventHandler<ConferencePublished>,
        IEventHandler<ConferenceUnpublished>,
        IEventHandler<SeatCreated>,
        IEventHandler<SeatUpdated>,
        IEventHandler<AvailableSeatsChanged>,
        IEventHandler<SeatsReserved>,
        IEventHandler<SeatsReservationCancelled>
    {
        private readonly Func<ConferenceRegistrationDbContext> contextFactory;
        private ICommandBus bus;

        public ConferenceViewModelGenerator(Func<ConferenceRegistrationDbContext> contextFactory, ICommandBus bus)
        {
            this.contextFactory = contextFactory;
            this.bus = bus;
        }

        public void Handle(ConferenceCreated @event)
        {
            using (var repository = this.contextFactory.Invoke())
            {
                repository.Set<Conference>().Add(new Conference(
                    @event.SourceId,
                    @event.Slug,
                    @event.Name,
                    @event.Description,
                    @event.Location,
                    @event.Tagline,
                    @event.TwitterSearch,
                    @event.StartDate,
                    Enumerable.Empty<SeatType>()));

                repository.SaveChanges();
            }
        }

        public void Handle(ConferenceUpdated @event)
        {
            using (var repository = this.contextFactory.Invoke())
            {
                var confDto = repository.Find<Conference>(@event.SourceId);
                if (confDto != null)
                {
                    confDto.Code = @event.Slug;
                    confDto.Description = @event.Description;
                    confDto.Location = @event.Location;
                    confDto.Name = @event.Name;
                    confDto.StartDate = @event.StartDate;
                    confDto.Tagline = @event.Tagline;
                    confDto.TwitterSearch = @event.TwitterSearch;

                    repository.SaveChanges();
                }
                else
                {
                    Trace.TraceError("Failed to locate Conference read model for updated conference with id {0}.", @event.SourceId);
                }
            }
        }

        public void Handle(ConferencePublished @event)
        {
            using (var repository = this.contextFactory.Invoke())
            {
                var dto = repository.Find<Conference>(@event.SourceId);
                if (dto != null)
                {
                    dto.IsPublished = true;

                    repository.Save(dto);
                }
                else
                {
                    Trace.TraceError("Failed to locate Conference read model for published conference with id {0}.", @event.SourceId);
                }
            }
        }

        public void Handle(ConferenceUnpublished @event)
        {
            using (var repository = this.contextFactory.Invoke())
            {
                var dto = repository.Find<Conference>(@event.SourceId);
                if (dto != null)
                {
                    dto.IsPublished = false;

                    repository.Save(dto);
                }
                else
                {
                    Trace.TraceError("Failed to locate Conference read model for unpublished conference with id {0}.", @event.SourceId);
                }
            }
        }

        public void Handle(SeatCreated @event)
        {
            using (var repository = this.contextFactory.Invoke())
            {
                var dto = repository.Find<Conference>(@event.ConferenceId);
                if (dto != null)
                {
                    dto.Seats.Add(new SeatType(@event.SourceId, @event.ConferenceId, @event.Name, @event.Description, @event.Price, @event.Quantity));

                    this.bus.Send(new AddSeats
                    {
                        ConferenceId = @event.ConferenceId,
                        SeatType = @event.SourceId,
                        Quantity = @event.Quantity
                    });

                    repository.Save(dto);
                }
                else
                {
                    Trace.TraceError("Failed to locate Conference read model for created seat, with conference id {0}.", @event.ConferenceId);
                }
            }
        }

        public void Handle(SeatUpdated @event)
        {
            using (var repository = this.contextFactory.Invoke())
            {
                var dto = repository.Set<Conference>().Include(x => x.Seats).FirstOrDefault(x => x.Id == @event.ConferenceId);
                if (dto != null)
                {
                    var seat = dto.Seats.FirstOrDefault(x => x.Id == @event.SourceId);
                    if (seat != null)
                    {
                        seat.Description = @event.Description;
                        seat.Name = @event.Name;
                        seat.Price = @event.Price;

                        // Calculate diff to drive the seat availability.
                        // Is this appropriate to have it here?
                        var diff = @event.Quantity - seat.Quantity;

                        seat.Quantity = @event.Quantity;

                        repository.Save(dto);

                        if (diff > 0)
                        {
                            this.bus.Send(new AddSeats
                            {
                                ConferenceId = @event.ConferenceId,
                                SeatType = @event.SourceId,
                                Quantity = diff,
                            });
                        }
                        else
                        {
                            this.bus.Send(new RemoveSeats
                            {
                                ConferenceId = @event.ConferenceId,
                                SeatType = @event.SourceId,
                                Quantity = Math.Abs(diff),
                            });
                        }
                    }
                    else
                    {
                        Trace.TraceError("Failed to locate Seat Type read model being updated with id {0}.", @event.SourceId);
                    }
                }
                else
                {
                    Trace.TraceError("Failed to locate Conference read model for updated seat type, with conference id {0}.", @event.ConferenceId);
                }
            }
        }

        public void Handle(AvailableSeatsChanged @event)
        {
            this.UpdateAvailableQuantity(@event, @event.Seats);
        }

        public void Handle(SeatsReserved @event)
        {
            this.UpdateAvailableQuantity(@event, @event.AvailableSeatsChanged);
        }

        public void Handle(SeatsReservationCancelled @event)
        {
            this.UpdateAvailableQuantity(@event, @event.AvailableSeatsChanged);
        }

        private void UpdateAvailableQuantity(IVersionedEvent @event, IEnumerable<SeatQuantity> seats)
        {
            using (var repository = this.contextFactory.Invoke())
            {
                var dto = repository.Set<Conference>().Include(x => x.Seats).FirstOrDefault(x => x.Id == @event.SourceId);
                if (dto != null)
                {
                    // This check assumes events might be received more than once, but not out of order
                    if (@event.Version > dto.SeatsAvailabilityVersion)
                    {
                        foreach (var seat in seats)
                        {
                            var seatDto = dto.Seats.FirstOrDefault(x => x.Id == seat.SeatType);
                            if (seatDto != null)
                            {
                                seatDto.AvailableQuantity += seat.Quantity;
                            }
                            else
                            {
                                // TODO should reject the entire update?
                                Trace.TraceError("Failed to locate Seat Type read model being updated with id {0}.", seat.SeatType);
                            }
                        }

                        dto.SeatsAvailabilityVersion = @event.Version;

                        repository.Save(dto);
                    }
                    else
                    {
                        Trace.TraceWarning(
                            "Ignoring availability update message with version {1} for conference id {0}, last known version {2}.",
                            @event.SourceId,
                            @event.Version,
                            dto.SeatsAvailabilityVersion);
                    }
                }
                else
                {
                    Trace.TraceError("Failed to locate Conference read model for updated seat type, with conference id {0}.", @event.SourceId);
                }
            }
        }
    }
}
