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

namespace Conference.Specflow.Steps
{
    [Binding]
    public class SelfRegistrationEndToEndSadSteps : StepDefinition
    {
        [When(@"the Registrant proceeds to cancel the payment")]
        public void WhenTheRegistrantProceedToCancelThePayment()
        {
            Browser.Click(Constants.UI.RejectPaymentInputValue);
        }

        [When(@"the Registrant proceeds to make the Reservation with no selected seats")]
        public void WhenTheRegistrantProceedToMakeTheReservationWithNoSelectedSeats()
        {
            Browser.Click(Constants.UI.NextStepId);
        }

        [Then(@"the payment selection page will show up")]
        public void ThenThePaymentSelectionPageWillShowUp()
        {
            Assert.True(
                Browser.SafeContainsText(Constants.UI.ReservationSuccessfull),
                string.Format("The following text was not found on the page: {0}", Constants.UI.ReservationSuccessfull));
        }
    }
}
