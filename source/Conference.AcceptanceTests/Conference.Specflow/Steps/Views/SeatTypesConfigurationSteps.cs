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

using Conference.Specflow.Support;
using TechTalk.SpecFlow;
using Xunit;
using System.Linq;

namespace Conference.Specflow.Steps
{
    [Binding]
    public class SeatTypesConfigurationSteps : StepDefinition
    {
        [Given(@"the Business Customer selects the Seat Types option")]
        public void GivenTheBusinessCustomerSelectsTheSeatTypesOption()
        {
            Browser.Click(Constants.UI.ConferenceManagementSeatTypesId);
        }

        [Given(@"the Business Customer proceeds to create new Seat Types")]
        public void GivenTheBusinessCustomerProceedToCreateNewSeatTypes()
        {
            Browser.Click(Constants.UI.ConferenceManagementCreateNewSeatTypesId);
        }

        [When(@"the Business Customer proceeds to create the Seat Types")]
        public void WhenTheBusinessCustomerProceedToCreateTheSeatTypes(Table table)
        {
            var slug = ScenarioContext.Current.Get<string>("slug");
            ConferenceHelper.CreateSeats(slug, table);
        }

        [Then(@"the new Seat Types with this information are created")]
        public void ThenTheNewSeatTypesWithThisInformationAreCreated(Table table)
        {
            var conferenceInfo = ConferenceHelper.FindConference(ScenarioContext.Current.Get<string>("slug"));
            var seats = conferenceInfo.Seats.ToList();

            foreach (var row in table.Rows)
            {
                Assert.True(seats.Any(s => s.Name == row["Name"]));
                Assert.True(seats.Any(s => s.Description == row["Description"]));
                Assert.True(seats.Any(s => s.Quantity == int.Parse(row["Quantity"])));
                Assert.True(seats.Any(s => s.Price == decimal.Parse(row["Price"])));
            }
        }
    }
}
