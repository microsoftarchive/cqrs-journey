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
using System.Globalization;
using System.Threading.Tasks;
using Conference.Specflow.Support;
using TechTalk.SpecFlow;
using Xunit;
using W = WatiN.Core;

namespace Conference.Specflow.Steps.Views
{
    [Binding]
    public class MultipleConferenceConfigurationSteps : StepDefinition
    {
        private Table conferenceInfo;
        private Table seatsInfo;
        private readonly List<string> slugs = new List<string>();

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

        [When(@"the Business Customer proceed to create (.*) '(.*)' conferences")]
        public void WhenTheBusinessCustomerProceedToCreateManyConsecutiveConferences(int conferences, string value)
        {
            var rnd = new Random();
            bool isRandom = value.Equals("random", StringComparison.OrdinalIgnoreCase);

            for(var i = 1; i <= conferences; i++)
            {
                Browser.GoTo(Constants.ConferenceManagementCreatePage);
                PopulateConferenceInformation(isRandom ? rnd.Next() : i);
                Browser.Click(Constants.UI.CreateConferenceId);
                Browser.Click(Constants.UI.PublishConferenceId);
                Browser.Click(Constants.UI.ConferenceManagementSeatTypesId);
                CreateSeats();
            }
        }

        [Then(@"all the conferences are created")]
        public void ThenAllTheConferencesAreCreated()
        {
            Parallel.ForEach(slugs,
                             s => Assert.NotNull(ConferenceHelper.FindConference(s)));
        }

        private void PopulateConferenceInformation(int number)
        {
            var row = conferenceInfo.Rows[0];
            var slug = ToOrdinal(row["Slug"], number);
            slugs.Add(slug);

            Browser.SetInput("Name", ToOrdinal(row["Name"], number));
            Browser.SetInput("Description", ToOrdinal(row["Description"], number));
            Browser.SetInput("StartDate", row["Start"]);
            Browser.SetInput("EndDate", row["End"]);
            Browser.SetInput("OwnerName", ToOrdinal(row["Owner"],number));
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
                for (int i = 0; i < 4; i++) //Browser.TextFields.Count
                {
                    Browser.TextFields[i].Value = row[i];
                }
                Browser.Click(Constants.UI.ConferenceManagementCreateNewSeatTypeId);
            }
        }

        private string ToOrdinal(string data, int value)
        {
            return data.Replace("%1", value.ToString(CultureInfo.InvariantCulture));
        }
    }
}
