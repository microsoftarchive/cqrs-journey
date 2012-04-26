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

namespace Registration.ReadModel.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class ConferenceDao : IConferenceDao
    {
        private readonly Func<ConferenceRegistrationDbContext> contextFactory;

        public ConferenceDao(Func<ConferenceRegistrationDbContext> contextFactory)
        {
            this.contextFactory = contextFactory;
        }

        public ConferenceDescriptionDTO GetDescription(string conferenceCode)
        {
            using (var repository = this.contextFactory.Invoke())
            {
                return repository
                    .Query<ConferenceDTO>()
                    .Where(dto => dto.Code == conferenceCode)
                    .Select(x => new ConferenceDescriptionDTO { Id = x.Id, Code = x.Code, Name = x.Name, Description = x.Description, StartDate = x.StartDate })
                    .FirstOrDefault();
            }
        }

        public ConferenceAliasDTO GetConferenceAlias(string conferenceCode)
        {
            using (var repository = this.contextFactory.Invoke())
            {
                return repository
                    .Query<ConferenceDTO>()
                    .Where(dto => dto.Code == conferenceCode)
                    .Select(x => new ConferenceAliasDTO { Id = x.Id, Code = x.Code, Name = x.Name })
                    .FirstOrDefault();
            }
        }

        public IList<ConferenceAliasDTO> GetPublishedConferences()
        {
            using (var repository = this.contextFactory.Invoke())
            {
                return repository
                    .Query<ConferenceDTO>()
                    .Where(dto => dto.IsPublished)
                    .Select(x => new ConferenceAliasDTO { Id = x.Id, Code = x.Code, Name = x.Name })
                    .ToList();
            }
        }

        public IList<ConferenceSeatTypeDTO> GetPublishedSeatTypes(Guid conferenceId)
        {
            using (var repository = this.contextFactory.Invoke())
            {
                // NOTE: If the ConferenceSeatTypeDTO had the ConferenceId property exposed, this query should be simpler. Why do we need to hide the FKs in the read model?
                return repository.Query<ConferenceDTO>().Where(c => c.Id == conferenceId).Select(c => c.Seats).FirstOrDefault().ToList();
            }
        }
    }
}