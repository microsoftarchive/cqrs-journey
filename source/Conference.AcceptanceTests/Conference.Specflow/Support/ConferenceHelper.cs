using System;
using System.Threading;
using Infrastructure.Azure;
using Infrastructure.Azure.Messaging;
using Infrastructure.Messaging;
using Infrastructure.Serialization;
using Newtonsoft.Json;
using TechTalk.SpecFlow;
using Registration.Commands;
using Registration;
using System.Collections.Generic;
using System.Linq;

namespace Conference.Specflow
{
    static class ConferenceHelper
    {    
        public static ConferenceInfo PopulateConfereceData(Table table, string conferenceSlug)
        {
            ConferenceService svc = new ConferenceService(BuildEventBus());
            ConferenceInfo conference = svc.FindConference(conferenceSlug);

            if (null == conference)
            {
                conference = BuildConferenceInfo(table, conferenceSlug);
                svc.CreateConference(conference);
                svc.Publish(conference.Id);
                // Wait for the events to be processed
                Thread.Sleep(Constants.WaitTimeout);
            }

            return conference;
        }

        public static Guid ReserveSeats(ConferenceInfo conference, Table table)
        {
            List<SeatQuantity> seats = new List<SeatQuantity>();

            foreach (var row in table.Rows)
            {
                var seatInfo = conference.Seats.FirstOrDefault(s => s.Name == row["seat type"]);
                if (seatInfo != null)
                {
                    int qt = row.ContainsKey("quantity") ? Int32.Parse(row["quantity"]) : seatInfo.Quantity;
                    seats.Add(new SeatQuantity(seatInfo.Id, qt));
                }
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
                SeatInfo seat = new SeatInfo()
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
            return new EventBus(GetTopicSender("events"), new MetadataProvider(), GetSerializer());
        }

        private static ICommandBus BuildCommandBus()
        {
            return new CommandBus(GetTopicSender("commands"), new MetadataProvider(), GetSerializer());
        }

        private static TopicSender GetTopicSender(string topic)
        {
            var settings = MessagingSettings.Read("Settings.xml");
            return new TopicSender(settings, "conference/" + topic);
        }

        private static JsonSerializerAdapter GetSerializer()
        {
            return new JsonSerializerAdapter(JsonSerializer.Create(new JsonSerializerSettings
             {
                 // Allows deserializing to the actual runtime type
                 TypeNameHandling = TypeNameHandling.Objects,
                 // In a version resilient way
                 TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple
             }));
        }
    }
}
