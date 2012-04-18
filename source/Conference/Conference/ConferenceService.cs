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
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Linq;

    public class ConferenceService
    {
        public ConferenceInfo Find(string slug)
        {
            using (var context = new DomainContext())
            {
                return context.Conferences.FirstOrDefault(x => x.Slug == slug);
            }
        }

        public ConferenceInfo Find(string email, string accessCode)
        {
            using (var context = new DomainContext())
            {
                return context.Conferences.FirstOrDefault(x => x.OwnerEmail == email && x.AccessCode == accessCode);
            }
        }

        public IEnumerable<SeatInfo> FindSeats(string slug)
        {
            using (var context = new DomainContext())
            {
                return context.Conferences.Include(x => x.Seats).FirstOrDefault(x => x.Slug == slug).Seats;
            }
        }


        public void Create(ConferenceInfo conference)
        {
            // using (tx)
            // {
            //  DB save (state snapshot)
            //  DB queue (events) -> push to bus (async)
            // }
        }

        public void Create(SeatInfo seat)
        {
        }

        public void Update(ConferenceInfo conference)
        {
        }

        public void Update(SeatInfo seat)
        {
        }

    }
}
