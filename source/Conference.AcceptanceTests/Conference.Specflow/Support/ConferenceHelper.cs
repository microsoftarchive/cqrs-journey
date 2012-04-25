using System;
using System.Threading;
using Infrastructure.Azure;
using Infrastructure.Azure.Messaging;
using Infrastructure.Messaging;
using Infrastructure.Serialization;
using Newtonsoft.Json;
using TechTalk.SpecFlow;

namespace Conference.Specflow
{
    static class ConferenceHelper
    {
        public static void PopulateConfereceData(Table table, string conferenceSlug)
        {
            ConferenceInfo conference = BuildConferenceInfo(conferenceSlug);

            foreach (var row in table.Rows)
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

            ConferenceService svc = new ConferenceService(BuildEventBus());
            if (null == svc.FindConference(conferenceSlug))
            {
                svc.CreateConference(conference);
                svc.Publish(conference.Id);
                // Wait for the events to be processed
                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
        }

        private static ConferenceInfo BuildConferenceInfo(string conferenceSlug)
        {
            return new ConferenceInfo()
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
        }

        private static IEventBus BuildEventBus()
        {
            // Using ConferenceService
            var serializer = new JsonSerializerAdapter(JsonSerializer.Create(new JsonSerializerSettings
            {
                // Allows deserializing to the actual runtime type
                TypeNameHandling = TypeNameHandling.Objects,
                // In a version resilient way
                TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple
            }));

            var settings = MessagingSettings.Read("Settings.xml");

            return new EventBus(new TopicSender(settings, "conference/events"), new MetadataProvider(), serializer);
        }
    }
}
