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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Conference.Specflow.Support;
using Registration.Events;
using TechTalk.SpecFlow;
using Xunit;
using W = WatiN.Core;
using System.Linq;

namespace Conference.Specflow.Steps.Views
{
    [Binding]
    public class MultipleConferenceConfigurationSteps : StepDefinition
    {
        private Table conferenceInfo;
        private Table seatsInfo;
        private List<string> slugs;

        [Given(@"this base conference information")]
        public void GivenThisBaseConferenceInformation(Table table)
        {
            conferenceInfo = table;
        }

        [Given(@"these Seat Types")]
        public void GivenTheseSeatTypes(Table table)
        {
            seatsInfo = table;
        }

        [When(@"the Business Customer proceeds to create (.*) '(.*)' conferences")]
        public void WhenTheBusinessCustomerProceedToCreateManyConsecutiveConferences(int conferences, string value)
        {
            slugs = new List<string>();
            var rnd = new Random();
            bool isRandom = value.Equals("random", StringComparison.OrdinalIgnoreCase);

            for(var i = 1; i <= conferences; i++)
            {
                Browser.GoTo(Constants.ConferenceManagementCreatePage);
                Browser.WaitForComplete((int)Constants.UI.WaitTimeout.TotalSeconds);
                PopulateConferenceInformation(isRandom ? (i + "000" + rnd.Next()) : i.ToString(CultureInfo.InvariantCulture));
                Browser.Click(Constants.UI.CreateConferenceId);
                Browser.Click(Constants.UI.PublishConferenceId);
                Browser.Click(Constants.UI.ConferenceManagementSeatTypesId);
                CreateSeats();
            }
        }

        [Then(@"all the conferences are created")]
        public void ThenAllTheConferencesAreCreated()
        {
            var failureCollector = new ConcurrentBag<string>();

            Parallel.ForEach(slugs, s =>
            {
                var mConf = ConferenceHelper.FindConference(s);
                if(mConf == null)
                {
                    failureCollector.Add(string.Format("Conference with slug '{0}' not found in management repository.", s));
                    return;
                }
                var success = MessageLogHelper.CollectEvents<AvailableSeatsChanged>(mConf.Id, seatsInfo.Rows.Count);
                if(!success)
                {
                    failureCollector.Add(string.Format("Some seats were not found in Conference '{0}'", mConf.Name));
                }
            });

            if(failureCollector.Count > 0)
            {
                var sb = new StringBuilder();
                failureCollector.ToList().ForEach(s => sb.AppendLine(s));
                Assert.True(failureCollector.Count == 0, sb.ToString()); // raise error with all the failures
            }
        }

        private void PopulateConferenceInformation(string id)
        {
            var row = conferenceInfo.Rows[0];
            var slug = ToOrdinal(row["Slug"], id);
            slugs.Add(slug);

            Browser.SetInput("Name", ToOrdinal(row["Name"], id));
            Browser.SetInput("Description", ToOrdinal(row["Description"], id));
            Browser.SetInput("StartDate", row["Start"]);
            Browser.SetInput("EndDate", row["End"]);
            Browser.SetInput("OwnerName", ToOrdinal(row["Owner"], id));
            Browser.SetInput("OwnerEmail", row["Email"]);
            Browser.SetInput("name", row["Email"], "ConfirmEmail");
            Browser.SetInput("Slug", slug);
            Browser.SetInput("Location", Constants.UI.Location);
        }

        private void CreateSeats()
        {
            foreach(var row in seatsInfo.Rows)
            {
                Browser.Click(Constants.UI.ConferenceManagementCreateNewSeatTypesId);
                Browser.WaitForComplete();
                for (int i = 0; i < 4; i++) //Browser.TextFields.Count
                {
                    Browser.TextFields[i].Value = row[i];
                }
                Browser.Click(Constants.UI.ConferenceManagementCreateNewSeatTypeId);
            }
        }

        private string ToOrdinal(string data, string value)
        {
            return data.Replace("%1", value);
        }
    }
}
