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

using System.Text.RegularExpressions;
using System.Threading;
using Conference.Specflow.Support;
using TechTalk.SpecFlow;
using Xunit;

namespace Conference.Specflow.Steps
{
    [Binding]
    public class SelfRegistrationReservationWithFullAvailabilitySteps
    {
        [Then(@"the Order should be found with the following Order Items")]
        public void ThenTheOrderShouldBeFoundWithTheFollowingOrderItems(Table table)
        {
            var browser = ScenarioContext.Current.Browser();
            string accessCode = browser.FindText(new Regex("[A-Z0-9]{6}"));
            Assert.False(string.IsNullOrWhiteSpace(accessCode));
            string email;
            Assert.True(ScenarioContext.Current.TryGetValue("email", out email));

            Thread.Sleep(Constants.WaitTimeout); // Wait for event processing

            // Navigate to Registration page
            browser.GoTo(Constants.FindOrderPage(ScenarioContext.Current.Get<ConferenceInfo>().Slug));
            browser.WaitForComplete((int)Constants.UI.WaitTimeout.TotalSeconds);
            browser.SetInput("name", email, "email");
            browser.SetInput("name", accessCode, "accessCode");
            browser.Click(Constants.UI.FindId);
            browser.WaitUntilContainsText(Constants.UI.FindOrderSuccessfull, (int)Constants.UI.WaitTimeout.TotalSeconds);
            
            foreach (var row in table.Rows)
            {
                Assert.True(browser.ContainsValueInTableRow(row["seat type"], row["quantity"]),
                    string.Format("The following text was not found on the page: {0} or {1}", row["seat type"], row["quantity"]));
            }
        }
    }
}
