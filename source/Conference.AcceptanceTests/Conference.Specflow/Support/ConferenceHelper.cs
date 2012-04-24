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
        private static object syncLock = new object();

        public static void PopulateConfereceData(Table table)
        {
            ConferenceInfo conference = BuildConferenceInfo();

            foreach (var row in table.Rows)
            {
                SeatInfo seat = new SeatInfo()
                {
                    Id = Guid.NewGuid(),
                    Description = row["seat type"],
                    Name = row["seat type"],
                    Price = Convert.ToDecimal(row["rate"].Replace("$", "")),
                    Quantity = 500
                };
                conference.Seats.Add(seat);
            }

            SaveConferenceInfo(conference);
        }

        private static void SaveConferenceInfo(ConferenceInfo conference)
        {
            ConferenceService svc = new ConferenceService(BuildEventBus());

            lock (syncLock)
            {
                if (null == svc.FindConference(Constants.ConferenceSlug))
                {
                    svc.CreateConference(conference);
                    svc.Publish(conference.Id);
                    // Wait for the events to be processed
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
            }
        }

        private static ConferenceInfo BuildConferenceInfo()
        {
            return new ConferenceInfo()
            {
                Description = "CQRS summit 2012 conference",
                Name = "test",
                Slug = Constants.ConferenceSlug,
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
