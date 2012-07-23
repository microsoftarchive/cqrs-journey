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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Conference.Common.Entity;
using Registration.Events;
using TechTalk.SpecFlow;
using Infrastructure.Messaging;
using Infrastructure.Serialization;
#if LOCAL
    using Infrastructure.Sql.Messaging;
    using Infrastructure.Sql.Messaging.Implementation;
#else
    using Infrastructure;
    using Infrastructure.Azure;
    using Infrastructure.Azure.Messaging;
#endif

namespace Conference.Specflow.Support
{
    static class ConferenceHelper
    {
        static ConferenceHelper()
        { 
            Database.DefaultConnectionFactory = new ServiceConfigurationSettingConnectionFactory(Database.DefaultConnectionFactory);
            Database.SetInitializer<ConferenceContext>(null);
        }

        public static ConferenceInfo PopulateConfereceData(Table table)
        {
            var svc = new ConferenceService(BuildEventBus());
            var conference = BuildInternalConferenceInfo(table);
            svc.CreateConference(conference);
            svc.Publish(conference.Id);
            // publish seats
            ICollection<SeatType> createdSeats = CreateSeats(table);
            foreach (var seat in createdSeats)
            {
                svc.CreateSeat(conference.Id, seat);
            }

            var created = MessageLogHelper.CollectEvents<AvailableSeatsChanged>(conference.Id, createdSeats.Count);

            if(!created)
                throw new TimeoutException("Conference creation error");

            // Update the confInfo with the created seats
            
            conference.Seats.AddRange(createdSeats);

            return conference;
        }

        public static ConferenceInfo FindConference(string conferenceSlug)
        {
            using (var context = new ConferenceContext("ConferenceManagement"))
            {
                return context.Conferences.Include(x => x.Seats).FirstOrDefault(x => x.Slug == conferenceSlug);
            }
        }

        public static Order FindOrder(Guid conferenceId, Guid orderId)
        {
            var svc = new ConferenceService(BuildEventBus());
            return svc.FindOrders(conferenceId).FirstOrDefault(o => o.Id == orderId);
        }

        public static void CreateSeats(string conferenceSlug, Table table)
        {
            var svc = new ConferenceService(BuildEventBus());
            var conference = FindConference(conferenceSlug);

            foreach (var row in table.Rows)
            {
                svc.CreateSeat(conference.Id, new SeatType
                                            {
                                                Name = row["Name"],
                                                Description = row["Description"],
                                                Quantity = int.Parse(row["Quantity"]),
                                                Price = decimal.Parse(row["Price"])
                                            });
            }
        }

        public static ConferenceInfo BuildConferenceInfo(Table conference)
        {
            string conferenceSlug = Slug.CreateNew().Value;
            return new ConferenceInfo
            {
                Description = conference.Rows[0]["Description"],
                Name = conferenceSlug,
                Slug = conferenceSlug,
                Location = Constants.UI.Location,
                Tagline = Constants.UI.TagLine,
                TwitterSearch = Constants.UI.TwitterSearch,
                StartDate = DateTime.Parse(conference.Rows[0]["Start"]),
                EndDate = DateTime.Parse(conference.Rows[0]["End"]),
                OwnerName = conference.Rows[0]["Name"],
                OwnerEmail = conference.Rows[0]["Email"],
                IsPublished = false,
                WasEverPublished = false
            };
        }

        public static ICollection<SeatType> CreateSeats(Table seats)
        {
            var createdSeats = new List<SeatType>();

            foreach (var row in seats.Rows)
            {
                var seat = new SeatType()
                {
                    Id = Guid.NewGuid(),
                    Description = row["seat type"],
                    Name = row["seat type"],
                    Price = Convert.ToDecimal(row["rate"].Replace("$", "")),
                    Quantity = Convert.ToInt32(row["quota"])
                };
                createdSeats.Add(seat);
            }

            return createdSeats;
        }

        private static ConferenceInfo BuildInternalConferenceInfo(Table seats)
        {
            string conferenceSlug = Slug.CreateNew().Value;
            var conference = new ConferenceInfo()
            {
                Description = Constants.UI.ConferenceDescription +  " (" + conferenceSlug + ")",
                Name = conferenceSlug,
                Slug = conferenceSlug,
                Location = Constants.UI.Location,
                Tagline = Constants.UI.TagLine,
                TwitterSearch = Constants.UI.TwitterSearch,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(1),
                OwnerName = "test",
                OwnerEmail = "testEmail@test.net",
                IsPublished = true,
                WasEverPublished = true
            };

            return conference;
        }

        internal static IEventBus BuildEventBus()
        {
            var serializer = new JsonTextSerializer();
#if LOCAL
            return new EventBus(GetMessageSender("SqlBus.Events"), serializer);
#else
            return new EventBus(GetTopicSender("events"), new StandardMetadataProvider(), serializer);
#endif
        }

        internal static ICommandBus BuildCommandBus()
        {
            var serializer = new JsonTextSerializer();
#if LOCAL
            return new CommandBus(GetMessageSender("SqlBus.Commands"), serializer);
#else
            return new SynchronousCommandBusDecorator(new CommandBus(GetTopicSender("commands"), new StandardMetadataProvider(), serializer));
#endif
        }

#if LOCAL
        private static MessageSender GetMessageSender(string tableName)
        {
            return new MessageSender(Database.DefaultConnectionFactory, "SqlBus", tableName);
        }
#else
        internal static TopicSender GetTopicSender(string topic)
        {
            var settings = InfrastructureSettings.Read("Settings.xml");
            return new TopicSender(settings.ServiceBus, "conference/" + topic);
        }
#endif
    }
}
