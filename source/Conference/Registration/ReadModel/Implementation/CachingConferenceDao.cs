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

namespace Registration.ReadModel.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Caching;

    /// <summary>
    /// Decorator that wraps <see cref="ConferenceDao"/> and caches this data in memory, as it can be accessed several times.
    /// This embraces eventual consistency, as we acknowledge the fact that the read model is stale even without caching.
    /// </summary>
    /// <remarks>
    /// For more information on the optimizations we did for V3
    /// see <see cref="http://go.microsoft.com/fwlink/p/?LinkID=258557"> Journey chapter 7</see>.
    /// </remarks>
    public class CachingConferenceDao : IConferenceDao
    {
        private readonly IConferenceDao decoratedDao;
        private readonly ObjectCache cache;

        public CachingConferenceDao(IConferenceDao decoratedDao, ObjectCache cache)
        {
            this.decoratedDao = decoratedDao;
            this.cache = cache;
        }

        public ConferenceDetails GetConferenceDetails(string conferenceCode)
        {
            var key = "ConferenceDao_Details_" + conferenceCode;
            var conference = this.cache.Get(key) as ConferenceDetails;
            if (conference == null)
            {
                conference = this.decoratedDao.GetConferenceDetails(conferenceCode);
                if (conference != null)
                {
                    this.cache.Set(key, conference, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(10) });
                }
            }

            return conference;
        }

        public ConferenceAlias GetConferenceAlias(string conferenceCode)
        {
            var key = "ConferenceDao_Alias_" + conferenceCode;
            var conference = this.cache.Get(key) as ConferenceAlias;
            if (conference == null)
            {
                conference = this.decoratedDao.GetConferenceAlias(conferenceCode);
                if (conference != null)
                {
                    this.cache.Set(key, conference, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(20) });
                }
            }

            return conference;
        }

        public IList<ConferenceAlias> GetPublishedConferences()
        {
            var key = "ConferenceDao_PublishedConferences";
            var cached = this.cache.Get(key) as IList<ConferenceAlias>;
            if (cached == null)
            {
                cached = this.decoratedDao.GetPublishedConferences();
                if (cached != null)
                {
                    this.cache.Set(key, cached, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(10) });
                }
            }

            return cached;
        }

        /// <summary>
        /// Gets ifnromation about the seat types.
        /// </summary>
        /// <remarks>
        /// Because the seat type contains the number of available seats, and this information can change often, notice
        /// how we manage the risks associated with displaying data that is very stale by adjusting caching duration 
        /// or not even caching at all if only a few seats remain.
        /// For more information on the optimizations we did for V3
        /// see <see cref="http://go.microsoft.com/fwlink/p/?LinkID=258557"> Journey chapter 7</see>.
        /// </remarks>
        public IList<SeatType> GetPublishedSeatTypes(Guid conferenceId)
        {
            var key = "ConferenceDao_PublishedSeatTypes_" + conferenceId;
            var seatTypes = this.cache.Get(key) as IList<SeatType>;
            if (seatTypes == null)
            {
                seatTypes = this.decoratedDao.GetPublishedSeatTypes(conferenceId);
                if (seatTypes != null)
                {
                    // determine how long to cache depending on criticality of using stale data.
                    TimeSpan timeToCache;
                    if (seatTypes.All(x => x.AvailableQuantity > 200 || x.AvailableQuantity <= 0))
                    {
                        timeToCache = TimeSpan.FromMinutes(5);
                    }
                    else if (seatTypes.Any(x => x.AvailableQuantity < 30 && x.AvailableQuantity > 0))
                    {
                        // there are just a few seats remaining. Do not cache.
                        timeToCache = TimeSpan.Zero;
                    }
                    else if (seatTypes.Any(x => x.AvailableQuantity < 100 && x.AvailableQuantity > 0))
                    {
                        timeToCache = TimeSpan.FromSeconds(20);
                    }
                    else
                    {
                        timeToCache = TimeSpan.FromMinutes(1);
                    }

                    if (timeToCache > TimeSpan.Zero)
                    {
                        this.cache.Set(key, seatTypes, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.UtcNow.Add(timeToCache) });
                    }
                }
            }

            return seatTypes;
        }

        public IList<SeatTypeName> GetSeatTypeNames(IEnumerable<Guid> seatTypes)
        {
            return this.decoratedDao.GetSeatTypeNames(seatTypes);
        }
    }
}
