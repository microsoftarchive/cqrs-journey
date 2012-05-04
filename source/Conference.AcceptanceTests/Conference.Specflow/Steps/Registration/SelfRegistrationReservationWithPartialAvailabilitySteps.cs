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

using Conference.Specflow.Support;
using TechTalk.SpecFlow;
using Xunit;
using W = WatiN.Core;

namespace Conference.Specflow.Steps.Registration
{
    [Binding]
    public class SelfRegistrationReservationWithPartialAvailabilitySteps
    {
        [Given(@"the list of Order Items offered to be waitlisted and selected by the Registrant")]
        public void GivenTheListOfOrderItemsOfferedToBeWaitlistedAndSelectedByTheRegistrant(Table table)
        {
            var browser = ScenarioContext.Current.Get<W.Browser>();
            foreach (var row in table.Rows)
            {
                browser.SelectListInTableRow(row["seat type"], row["quantity"]);
            }
        }

        [Given(@"the Registrant is offered to select any of these available seats")]
        [Then(@"the Registrant is offered to select any of these available seats")]
        public void ThenTheRegistrantIsOfferedToSelectAnyOfTheseAvailableSeats(Table table)
        {
            var browser = ScenarioContext.Current.Get<W.Browser>();
            foreach (var row in table.Rows)
            {
                Assert.True(browser.ContainsListItemsInTableRow(row["seat type"], row["selected"], row["message"]));
            }
        }
    }
}
