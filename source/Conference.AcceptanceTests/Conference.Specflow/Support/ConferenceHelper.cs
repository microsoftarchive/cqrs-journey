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

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using Conference.Common.Entity;
using Infrastructure;
using Registration;
using Registration.Commands;
using TechTalk.SpecFlow;
using Infrastructure.Messaging;
using Infrastructure.Serialization;
#if LOCAL
    using Infrastructure.Sql.Messaging;
    using Infrastructure.Sql.Messaging.Implementation;
#else
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
            string conferenceSlug = Slug.CreateNew().Value;
            var svc = new ConferenceService(BuildEventBus());
            var conference = BuildConferenceInfo(table, conferenceSlug);
            svc.CreateConference(conference);
            svc.Publish(conference.Id);

            Registration.ReadModel.Conference published = null;
            while(published == null || 
                !published.IsPublished || 
                published.Seats.Count != table.Rows.Count)
            {
                published = RegistrationHelper.FindConference(conference.Id);
                Thread.Sleep(100);
            }

            return conference;
        }

        public static ConferenceInfo FindConference(string conferenceSlug)
        {
            var svc = new ConferenceService(BuildEventBus());
            var conference = svc.FindConference(conferenceSlug);
            if (null != conference)
            {
                if (conference.Seats.Count == 0)
                    svc.FindSeatTypes(conference.Id).ToList().ForEach(s => conference.Seats.Add(s));
            }
            return conference;
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

        public static Guid ReserveSeats(ConferenceInfo conference, Table table)
        {
            var seats = new List<SeatQuantity>();

            foreach (var row in table.Rows)
            {
                var seatInfo = conference.Seats.FirstOrDefault(s => s.Name == row["seat type"]);
                if (seatInfo == null) 
                    throw new InvalidOperationException("seat type not found");
                
                int qt;
                if (!row.ContainsKey("quantity") ||
                    !Int32.TryParse(row["quantity"], out qt))
                    qt = seatInfo.Quantity;
                
                seats.Add(new SeatQuantity(seatInfo.Id, qt));
            }

            var seatReservation = new MakeSeatReservation
            {
                ConferenceId = conference.Id,
                ReservationId = Guid.NewGuid(),
                Seats = seats
            };

            var commandBus = BuildCommandBus();
            commandBus.Send(seatReservation);

            // Wait for the events to be processed
            Thread.Sleep(Constants.WaitTimeout);

            return seatReservation.ReservationId;
        }

        private static ConferenceInfo BuildConferenceInfo(Table seats, string conferenceSlug)
        {
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
                conference.Seats.Add(seat);
            }

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
            return new CommandBus(GetTopicSender("commands"), new StandardMetadataProvider(), serializer);
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
