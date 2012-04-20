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
    using System.Data.Entity;
    using System.Linq;
    using Common;
    using Conference;
    using Registration.ReadModel;
    using Registration.ReadModel.Implementation;

    public class ConferenceViewModelGenerator :
        IEventHandler<ConferenceCreated>,
        IEventHandler<ConferenceUpdated>,
        IEventHandler<ConferencePublished>,
        IEventHandler<ConferenceUnpublished>,
        IEventHandler<SeatCreated>,
        IEventHandler<SeatUpdated>
    {
        private readonly Func<ConferenceRegistrationDbContext> contextFactory;

        public ConferenceViewModelGenerator(Func<ConferenceRegistrationDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public void Handle(ConferenceCreated @event)
        {
            using (var repository = this.contextFactory.Invoke())
            {
                repository.Set<ConferenceAliasDTO>().Add(new ConferenceAliasDTO(@event.SourceId, @event.Slug, @event.Name));
                repository.Set<ConferenceDescriptionDTO>().Add(new ConferenceDescriptionDTO(@event.SourceId, @event.Slug, @event.Name, @event.Description));
                repository.Set<ConferenceDTO>().Add(new ConferenceDTO(@event.SourceId, @event.Slug, @event.Name, @event.Description, Enumerable.Empty<ConferenceSeatTypeDTO>()));

                repository.SaveChanges();
            }
        }

        public void Handle(ConferenceUpdated @event)
        {
            using (var repository = this.contextFactory.Invoke())
            {
                var aliasDto = repository.Find<ConferenceAliasDTO>(@event.SourceId);
                // TODO: replace with AutoMapper one-liner!
                if (aliasDto != null)
                {
                    aliasDto.Code = @event.Slug;
                    aliasDto.Name = @event.Name;
                }

                var descDto = repository.Find<ConferenceDescriptionDTO>(@event.SourceId);
                if (descDto != null)
                {
                    descDto.Code = @event.Slug;
                    descDto.Description = @event.Description;
                    descDto.Name = @event.Name;
                }

                var confDto = repository.Find<ConferenceDTO>(@event.SourceId);
                if (confDto != null)
                {
                    confDto.Code = @event.Slug;
                    confDto.Description = @event.Description;
                    confDto.Name = @event.Name;
                }

                repository.SaveChanges();
            }
        }

        public void Handle(ConferencePublished @event)
        {
            using (var repository = this.contextFactory.Invoke())
            {
                var dto = repository.Find<ConferenceAliasDTO>(@event.SourceId);
                if (dto != null)
                {
                    dto.IsPublished = true;

                    repository.Save(dto);
                }
            }
        }

        public void Handle(ConferenceUnpublished @event)
        {
            using (var repository = this.contextFactory.Invoke())
            {
                var dto = repository.Find<ConferenceAliasDTO>(@event.SourceId);
                if (dto != null)
                {
                    dto.IsPublished = false;

                    repository.Save(dto);
                }
            }
        }

        public void Handle(SeatCreated @event)
        {
            using (var repository = this.contextFactory.Invoke())
            {
                var dto = repository.Find<ConferenceDTO>(@event.ConferenceId);
                if (dto != null)
                {
                    dto.Seats.Add(new ConferenceSeatTypeDTO(@event.SourceId, @event.Name, @event.Description, @event.Price));

                    repository.Save(dto);
                }
            }
        }

        public void Handle(SeatUpdated @event)
        {
            using (var repository = this.contextFactory.Invoke())
            {
                var dto = repository.Set<ConferenceDTO>().Include(x => x.Seats).FirstOrDefault(x => x.Id == @event.ConferenceId);
                if (dto != null)
                {
                    var seat = dto.Seats.FirstOrDefault(x => x.Id == @event.SourceId);
                    if (seat != null)
                    {
                        seat.Description = @event.Description;
                        seat.Name = @event.Name;
                        seat.Price = @event.Price;

                        repository.Save(dto);
                    }
                }
            }
        }
    }
}
