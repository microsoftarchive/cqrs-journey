using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TechTalk.SpecFlow;
using Registration.ReadModel;
using Registration.ReadModel.Implementation;

namespace Conference.Specflow
{
    static class ConferenceHelper
    {
        public static void PopulateConfereceData(Table table)
        {
            List<ConferenceSeatTypeDTO> seats = new List<ConferenceSeatTypeDTO>();
            foreach (var row in table.Rows)
            {
                seats.Add(new ConferenceSeatTypeDTO(Guid.NewGuid(), row["seat type"], Convert.ToDouble(row["rate"].Replace("$", ""))));
            }

            using (ConferenceRegistrationDbContext registrationCtx = new ConferenceRegistrationDbContext())
            {
                ConferenceDTO conference = registrationCtx.Find<ConferenceDTO>(Guid.Empty);
                if (conference != null && conference.Seats.Count == 1)
                {
                    conference.Seats.AddRange(seats);
                }
                registrationCtx.Save<ConferenceDTO>(conference);
            }

            //SqlEventRepository<SeatsAvailability> sqlEventRepository = new SqlEventRepository<SeatsAvailability>();
        }
    }
}
