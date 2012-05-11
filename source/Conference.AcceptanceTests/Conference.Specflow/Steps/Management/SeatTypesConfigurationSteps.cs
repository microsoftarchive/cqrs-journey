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

namespace Conference.Specflow.Steps.Management
{
    [Binding]
    public class SeatTypesConfigurationSteps : StepDefinition
    {
        [Given(@"the Business Customer selects the Seat Types opcion")]
        public void GivenTheBusinessCustomerSelectsTheSeatTypesOpcion()
        {
            Browser.Click(Constants.UI.ConferenceManagementSeatTypesId);
        }

        [Given(@"the Business Customer proceed to create new Seat Types")]
        public void GivenTheBusinessCustomerProceedToCreateNewSeatTypes()
        {
            Browser.Click(Constants.UI.ConferenceManagementCreateNewSeatTypesId);
        }

        [Given(@"the information for the Seat Types")]
        public void GivenTheInformationForTheSeatTypes(Table table)
        {
            ScenarioContext.Current.Pending();
        }

        [When(@"the Business Customer proceed to create the Seat Types")]
        public void WhenTheBusinessCustomerProceedToCreateTheSeatTypes()
        {
            ScenarioContext.Current.Pending();
        }

        [Then(@"the new Seat Types with this information are created")]
        public void ThenTheNewSeatTypesWithThisInformationAreCreated(Table table)
        {
            ScenarioContext.Current.Pending();
        }
    }
}
