using System;
using System.Linq;
using TechTalk.SpecFlow;
using Registration.ReadModel.Implementation;
using Registration.ReadModel;
using System.Transactions;

namespace Conference.Specflow
{
    static class ConferenceHelper
    {
        public static void PopulateConfereceData(Table table)
        {
            if (IsConferenceCreated())
                return;

            ConferenceInfo conference = new ConferenceInfo()
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

            CreateConference(conference);
        }

        private static void CreateConference(ConferenceInfo conference)
        {
            using (var transaction = new TransactionScope(TransactionScopeOption.Required, 
                new TransactionOptions() { IsolationLevel = IsolationLevel.ReadCommitted }))
            {
                using (var context = new ConferenceContext())
                {
                    context.Conferences.Add(conference);
                    context.SaveChanges();
                }
                using (var repository = new ConferenceRegistrationDbContext())
                {
                    if (null == repository.Find<ConferenceDTO>(conference.Id))
                    {
                        var entity = new ConferenceDTO(
                                conference.Id,
                                conference.Slug,
                                conference.Name,
                                conference.Description,
                                conference.StartDate,
                                conference.Seats.Select(s => new ConferenceSeatTypeDTO(s.Id, s.Name, s.Description, s.Price)));
                        entity.IsPublished = conference.IsPublished;

                        repository.Save<ConferenceDTO>(entity);
                    }
                }
                transaction.Complete();
            }

            // Using ConferenceService
            //var serializer = new JsonSerializerAdapter(JsonSerializer.Create(new JsonSerializerSettings
            //{
            //    // Allows deserializing to the actual runtime type
            //    TypeNameHandling = TypeNameHandling.Objects,
            //    // In a version resilient way
            //    TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple
            //}));
            //var settings = MessagingSettings.Read("Settings.xml");
            //IEventBus eventBus = new EventBus(new TopicSender(settings, "conference/events"), new MetadataProvider(), serializer);
            //ConferenceService svc = new ConferenceService(eventBus);

            //if (null == svc.FindConference(Constants.ConferenceSlug))
            //{
            //    svc.CreateConference(conference);
            //    svc.Publish(conference.Id);
            //}
        }

        private static bool IsConferenceCreated()
        {
            using (var context = new ConferenceContext())
            {
                return context.Conferences
                    .Where(c => c.Slug == Constants.ConferenceSlug)
                    .Select(c => c.Slug)
                    .Any();
            }
        }
    }
}
