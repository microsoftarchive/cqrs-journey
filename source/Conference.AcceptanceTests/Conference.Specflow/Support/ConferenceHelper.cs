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

        public static ConferenceInfo PopulateConfereceData(Table table, string conferenceSlug)
        {
            var svc = new ConferenceService(BuildEventBus());
            var conference = svc.FindConference(conferenceSlug);

            if (null != conference)
            {
                if(conference.Seats.Count == 0)
                    svc.FindSeats(conference.Id).ToList().ForEach(s => conference.Seats.Add(s));
                return conference;
            }

            conference = BuildConferenceInfo(table, conferenceSlug);
            svc.CreateConference(conference);
            // Wait for the events to be processed
            // This delay may be removed after fixing the events ordering issue 
            Thread.Sleep(Constants.WaitTimeout);
            svc.Publish(conference.Id);
            // Wait for the events to be processed
            // This delay may be removed after fixing the events ordering issue 
            Thread.Sleep(Constants.WaitTimeout);
            return conference;
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

        public static void CancelSeatReservation(Guid conferenceId, Guid reservationId)
        {
            var seatReservation = new CancelSeatReservation 
            { 
                ConferenceId = conferenceId, 
                ReservationId = reservationId 
            };
            
            var commandBus = BuildCommandBus();
            commandBus.Send(seatReservation);

            // Wait for the events to be processed
            Thread.Sleep(Constants.WaitTimeout);
        }

        public static ICommandBus GetCommandBus()
        {
            return BuildCommandBus();
        }

        private static ConferenceInfo BuildConferenceInfo(Table seats, string conferenceSlug)
        {
            var conference = new ConferenceInfo()
            {
                Description = "Acceptance Tests CQRS summit 2012 conference (" + conferenceSlug + ")",
                Name = conferenceSlug,
                Slug = conferenceSlug,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(1),
                OwnerName = "test",
                OwnerEmail = "testEmail",
                IsPublished = true,
                WasEverPublished = true
            };

            foreach (var row in seats.Rows)
            {
                var seat = new SeatInfo()
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

        private static IEventBus BuildEventBus()
        {
#if LOCAL
            return new EventBus(GetMessageSender("SqlBus.Events"), new JsonTextSerializer());
#else
            return new EventBus(GetTopicSender("events"), new MetadataProvider(), new JsonTextSerializer());
#endif
        }

        private static ICommandBus BuildCommandBus()
        {
#if LOCAL
            return new CommandBus(GetMessageSender("SqlBus.Commands"), new JsonTextSerializer());
#else
            return new CommandBus(GetTopicSender("commands"), new MetadataProvider(), new JsonTextSerializer());
#endif
        }

#if LOCAL
        private static MessageSender GetMessageSender(string tableName)
        {
            return new MessageSender(Database.DefaultConnectionFactory, "SqlBus", tableName);
        }
#else
        private static TopicSender GetTopicSender(string topic)
        {
            var settings = InfrastructureSettings.ReadMessaging("Settings.xml");
            return new TopicSender(settings, "conference/" + topic);
        }
#endif
    }
}
