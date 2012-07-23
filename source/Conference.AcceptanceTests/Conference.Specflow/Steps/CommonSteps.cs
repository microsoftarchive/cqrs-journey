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
using System.Globalization;
using System.Linq;
using System.Threading;
using Conference.Specflow.Support;
using TechTalk.SpecFlow;
using WatiN.Core;
using Xunit;
using Table = TechTalk.SpecFlow.Table;
using W = WatiN.Core;

namespace Conference.Specflow.Steps
{
    [Binding]
    public class CommonSteps : StepDefinition
    {
        private ConferenceInfo conferenceInfo;

        #region Given

        [Given(@"the list of the available Order Items for the CQRS summit 2012 conference")]
        public void GivenTheListOfTheAvailableOrderItemsForTheCqrsSummit2012Conference(Table table)
        {
            // Populate Conference data
            conferenceInfo = ConferenceHelper.PopulateConfereceData(table);

            // Store for being used by external step classes
            ScenarioContext.Current.Set(conferenceInfo);
        }

        [Given(@"the selected Order Items")]
        public void GivenTheSelectedOrderItems(Table table)
        {
            SelectOrderItems(Browser, conferenceInfo, table);
        }

        [Given(@"the Registrant enters these details")]
        public void GivenTheRegistrantEnterTheseDetails(Table table)
        {
            Browser.SetInput("FirstName", table.Rows[0]["first name"]);
            Browser.SetInput("LastName", table.Rows[0]["last name"]);
            Browser.SetInput("Email", table.Rows[0]["email address"]);
            Browser.SetInput("data-val-required", table.Rows[0]["email address"], "Please confirm the e-mail address.");

            // Store email in case is needed for later use (Find Order by Code + email access)
            ScenarioContext.Current.Set(table.Rows[0]["email address"], "email");
        }

        [Given(@"these Seat Types become unavailable before the Registrant makes the reservation")]
        public void GivenTheseSeatTypesBecomeUnavailableBeforeTheRegistrantMakesTheReservation(Table table)
        {
            var controllerSteps = new SelfRegistrationEndToEndWithControllersSteps();
            controllerSteps.GivenTheSelectedOrderItems(table);
            controllerSteps.GivenTheRegistrantProceedToMakeTheReservation();
            controllerSteps.GivenTheRegistrantEnterTheseDetails("first", "last", "email@m.com");
            controllerSteps.GivenTheRegistrantProceedToCheckoutPayment();
            controllerSteps.WhenTheRegistrantProceedToConfirmThePayment();
        }

        [Given(@"the Registrant proceeds to make the Reservation")]
        public void GivenTheRegistrantProceedToMakeTheReservation()
        {
            MakeTheReservation(Browser);
        }

        [Given(@"the Registrant proceeds to make the Reservation with seats already reserved")]
        public void GivenTheRegistrantProceedToMakeTheReservationWithSeatsAlreadyReserved()
        {
            MakeTheReservationWithSeatsAlreadyReserved(Browser);
        }

        [Given(@"the Registrant proceeds to Checkout:Payment")]
        public void GivenTheRegistrantProceedToCheckoutPayment()
        {
            TheRegistrantProceedToCheckoutPayment();
        }

        [When(@"the Registrant proceeds to Checkout:NoPayment")]
        public void WhenTheRegistrantProceedToCheckoutNoPayment()
        {
            Browser.Click(Constants.UI.NextStepId);
        }

        [Given(@"the total should read \$(.*)")]
        public void GivenTheTotalShouldRead(int value)
        {
            TheTotalShouldRead(value);
        }

        [Given(@"the message '(.*)' will show up")]
        public void GivenTheMessageWillShowUp(string message)
        {
            TheMessageWillShowUp(message);
        }

        [Given(@"the Order should be created with the following Order Items")]
        public void GivenTheOrderShouldBeCreatedWithTheFollowingOrderItems(Table table)
        {
            TheOrderShouldBeCreatedWithTheFollowingOrderItems(table);
        }

        [Given(@"the Registrant proceeds to confirm the payment")]
        public void GivenTheRegistrantProceedToConfirmThePayment()
        {
            TheRegistrantProceedToConfirmThePayment();
        }

        [Given(@"the Registration process was successful")]
        public void GivenTheRegistrationProcessWasSuccessful()
        {
            TheRegistrationProcessWasSuccessful();
        }

        #endregion

        #region When

        [When(@"the Registrant proceeds to make the Reservation")]
        public void WhenTheRegistrantProceedToMakeTheReservation()
        {
            MakeTheReservation(Browser);
        }

        [When(@"the Registrant proceeds to make the Reservation with seats already reserved")]
        public void WhenTheRegistrantProceedToMakeTheReservationWithSeatsAlreadyReserved()
        {
            MakeTheReservationWithSeatsAlreadyReserved(Browser);
        }

        [When(@"the Registrant proceeds to Checkout:Payment")]
        public void WhenTheRegistrantProceedToCheckoutPayment()
        {
            TheRegistrantProceedToCheckoutPayment();
        }

        [When(@"the Registrant proceeds to confirm the payment")]
        public void WhenTheRegistrantProceedToConfirmThePayment()
        {
            TheRegistrantProceedToConfirmThePayment();
        }

        #endregion

        #region Then

        [Then(@"the selected Order Items")]
        public void ThenTheSelectedOrderItems(Table table)
        {
            SelectOrderItems(Browser, conferenceInfo, table, false);
            MakeTheReservation(Browser);
        }

        [Then(@"the Reservation is confirmed for all the selected Order Items")]
        public void ThenTheReservationIsConfirmedForAllTheSelectedOrderItems()
        {
            ReservationConfirmed(Browser);
        }

        [Then(@"the countdown is started")]
        public void ThenTheCountdownIsStarted()
        {
            var countdown = ScenarioContext.Current.Get<W.Browser>().Div("countdown_time").Text;

            Assert.False(string.IsNullOrWhiteSpace(countdown));
            var countdownTime = TimeSpan.ParseExact(countdown, @"mm\:ss", CultureInfo.InvariantCulture);
            Assert.True(countdownTime.Minutes > 0 && countdownTime.Minutes < 15);
        }

        [Then(@"the payment options should be offered for a total of \$(.*)")]
        public void ThenThePaymentOptionsShouldBeOfferedForATotalOf(int value)
        {
            Assert.True(Browser.SafeContainsText(value.ToString(CultureInfo.InvariantCulture)),
                string.Format("The following text was not found on the page: {0}", value));
        }

        [Then(@"these seats are assigned")]
        public void ThenTheseSeatsAreAssigned(Table table)
        {
            TheseOrderItemsShouldBeReserved(table);
        }

        [Then(@"these Order Items should be reserved")]
        public void ThenTheseOrderItemsShouldBeReserved(Table table)
        {
            TheseOrderItemsShouldBeReserved(table);
        }

        [Then(@"these Order Items should not be reserved")]
        public void ThenTheseOrderItemsShouldNotBeReserved(Table table)
        {
            foreach (var row in table.Rows)
            {
                Assert.True(Browser.ContainsValueInTableRow(row["seat type"], "0"),
                    string.Format("The following text was not found on the page: {0}", row["seat type"]));
            }
        }

        [Then(@"the total should read \$(.*)")]
        public void ThenTheTotalShouldRead(int value)
        {
            TheTotalShouldRead(value);
        }

        [Then(@"the error message for '(.*)' with value '(.*)' will show up")]
        public void ThenTheErrorMessageForIdWithValueWillShowUp(string id, string message)
        {
            var input = Browser.TextField(id);
            const string requiredValAttrId = "data-val-required";
            if (!input.Exists)
                input = Browser.TextFields.FirstOrDefault(t => t.GetAttributeValue(requiredValAttrId) == message);

            Assert.True(input.Exists);
            Assert.Equal(message, input.GetAttributeValue(requiredValAttrId));
        }

        [Then(@"the message '(.*)' will show up")]
        public void ThenTheMessageWillShowUp(string message)
        {
            TheMessageWillShowUp(message);
        }

        [Then(@"the Order should be created with the following Order Items")]
        public void ThenTheOrderShouldBeCreatedWithTheFollowingOrderItems(Table table)
        {
            TheOrderShouldBeCreatedWithTheFollowingOrderItems(table);
        }

        [Then(@"the Registration process was successful")]
        public void ThenTheRegistrationProcessWasSuccessful()
        {
            TheRegistrationProcessWasSuccessful();
        }

        #endregion

        #region Common code

        internal static void SelectOrderItems(Browser browser, ConferenceInfo conferenceInfo, Table table, bool navigateToRegPage = true)
        {
            if (navigateToRegPage)
            {
                browser.GoTo(Constants.RegistrationPage(conferenceInfo.Slug));
                browser.WaitForComplete((int)Constants.UI.WaitTimeout.TotalSeconds);
            }

            foreach (var row in table.Rows)
            {
                browser.SelectListInTableRow(row["seat type"], row["quantity"]);
            }
        }

        internal static void ReservationConfirmed(Browser browser)
        {
            Assert.True(browser.SafeContainsText(Constants.UI.ReservationSuccessfull),
                string.Format("The following text was not found on the page: {0}", Constants.UI.ReservationSuccessfull));
        }

        private void TheRegistrationProcessWasSuccessful()
        {
            Browser.WaitUntilContainsText(Constants.UI.RegistrationSuccessfull, (int)Constants.UI.WaitTimeout.TotalSeconds);
        }

        private void TheseOrderItemsShouldBeReserved(Table table)
        {
            foreach (var row in table.Rows)
            {
                Assert.True(Browser.ContainsValueInTableRow(row["seat type"], row["quantity"]),
                    string.Format("The following text was not found on the page: {0} or {1}", row["seat type"], row["quantity"]));
            }
        }

        private void TheRegistrantProceedToConfirmThePayment()
        {
            Browser.Click(Constants.UI.AcceptPaymentInputValue);
            Browser.WaitForComplete((int)Constants.UI.WaitTimeout.TotalSeconds);
        }

        private void TheOrderShouldBeCreatedWithTheFollowingOrderItems(Table table)
        {
            // Check id the access code was created
            string accessCode = Browser.FindText(Slug.FindBy);
            Assert.False(string.IsNullOrWhiteSpace(accessCode), "Access Code not found");

            // Navigate to the Seat Assignement page
            Browser.ClickAndWait(Constants.UI.ProceedToSeatAssignementId, Constants.UI.FindOrderSuccessfull);

            TheseOrderItemsShouldBeReserved(table);
        }

        private void TheMessageWillShowUp(string message)
        {
            Assert.True(Browser.SafeContainsText(message),
                string.Format("The following text was not found on the page: {0}", message));
        }

        private void TheTotalShouldRead(int value)
        {
            Assert.True(Browser.SafeContainsText(value.ToString(CultureInfo.InvariantCulture)),
                string.Format("The following text was not found on the page: {0}", value));
        }

        internal static void MakeTheReservation(Browser browser)
        {
            browser.ClickAndWait(Constants.UI.NextStepId, Constants.UI.ReservationSuccessfull);
        }

        internal static void MakeTheReservationWithSeatsAlreadyReserved(Browser browser)
        {
            browser.ClickAndWait(Constants.UI.NextStepId, Constants.UI.ReservationUnsuccessfull);
        }

        public void TheRegistrantProceedToCheckoutPayment()
        {
            Browser.ClickAndWait(Constants.UI.NextStepId, Constants.UI.ThirdpartyPayment);
        }

        #endregion
    }
}
