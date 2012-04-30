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
using System.Linq;
using System.Text;
using TechTalk.SpecFlow;
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

        [When(@"the Registrant proceed to make the Reservation with missing or invalid data")]
        public void WhenTheRegistrantProceedToMakeTheReservationWithMissingOrInvalidData()
        {
            ScenarioContext.Current.Get<W.Browser>().Click(Constants.UI.NextStepButtonID);
        }
    }
}
