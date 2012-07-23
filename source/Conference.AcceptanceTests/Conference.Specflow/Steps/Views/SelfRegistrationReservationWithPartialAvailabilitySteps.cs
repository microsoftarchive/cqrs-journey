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
using W = WatiN.Core;
using Xunit;

namespace Conference.Specflow.Steps
{
    [Binding]
    public class SelfRegistrationReservationWithPartialAvailabilitySteps
    {
        [Given(@"the Registrant is offered to select any of these available seats")]
        [Then(@"the Registrant is offered to select any of these available seats")]
        public void ThenTheRegistrantIsOfferedToSelectAnyOfTheseAvailableSeats(Table table)
        {
            AvailableSeats(ScenarioContext.Current.Browser(), table);
        }

        internal static void AvailableSeats(W.Browser browser, Table table)
        {
            foreach (var row in table.Rows)
            {
                Assert.True(browser.ContainsListItemsInTableRow(row["seat type"], row["selected"], row["message"]),
                    string.Format("some of these text where not found in the current page: '{0}', '{1}', '{2}'", row["seat type"], row["selected"], row["message"]));
            }
        }
    }
}
