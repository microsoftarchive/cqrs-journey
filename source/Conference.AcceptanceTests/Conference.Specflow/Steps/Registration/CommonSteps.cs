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
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using Conference.Specflow.Support;
using TechTalk.SpecFlow;
using Xunit;
using W = WatiN.Core;

namespace Conference.Specflow.Steps.Registration
{
    [Binding]
    public class CommonSteps
    {
        #region Given

        [Given(@"the list of the available Order Items for the CQRS summit 2012 conference with the slug code (.*)")]
        public void GivenTheListOfTheAvailableOrderItemsForTheCqrsSummit2012Conference(string conferenceSlug, Table table)
        {
            // Populate Conference data
            var conferenceInfo = ConferenceHelper.PopulateConfereceData(table, conferenceSlug);

            // Store for later use
            ScenarioContext.Current.Set(conferenceInfo);

            // Navigate to Registration page
            ScenarioContext.Current.Get<W.Browser>().GoTo(Constants.RegistrationPage(conferenceSlug));
        }

        [Given(@"the selected Order Items")]
        public void GivenTheSelectedOrderItems(Table table)
        {
            var browser = ScenarioContext.Current.Get<W.Browser>();
            foreach (var row in table.Rows)
            {
                browser.SelectListInTableRow(row["seat type"], row["quantity"]);
            }
        }

        [Given(@"the Registrant enter these details")]
        public void GivenTheRegistrantEnterTheseDetails(Table table)
        {
            // Allow some time for the events being processed
            Thread.Sleep(TimeSpan.FromSeconds(4));

            var browser = ScenarioContext.Current.Get<W.Browser>();
            browser.SetInput("FirstName", table.Rows[0]["first name"]);
            browser.SetInput("LastName", table.Rows[0]["last name"]);
            browser.SetInput("Email", table.Rows[0]["email address"]);
            browser.SetInput("data-val-required", table.Rows[0]["email address"], "Please confirm the e-mail address.");

            ScenarioContext.Current.Add("email", table.Rows[0]["email address"]);
        }

        [Given(@"these Seat Types becomes unavailable before the Registrant make the reservation")]
        public void GivenTheseSeatTypesBecomesUnavailableBeforeTheRegistrantMakeTheReservation(Table table)
        {
            var reservationId = ConferenceHelper.ReserveSeats(ScenarioContext.Current.Get<ConferenceInfo>(), table);
            // Store for revert the reservation after scenario ends
            ScenarioContext.Current.Set(reservationId, "reservationId");
        }

        [Given(@"the Registrant proceed to make the Reservation")]
        public void GivenTheRegistrantProceedToMakeTheReservation()
        {
            TheRegistrantProceedToMakeTheReservation();
        }

        [Given(@"the Registrant proceed to make the Reservation with seats already reserved")]
        public void GivenTheRegistrantProceedToMakeTheReservationWithSeatsAlreadyReserved()
        {
            TheRegistrantProceedToMakeTheReservationWithSeatsAlreadyReserved();
         }

        [Given(@"the Registrant proceed to Checkout:Payment")]
        public void GivenTheRegistrantProceedToCheckoutPayment()
        {
            TheRegistrantProceedToCheckoutPayment();
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

        [Given(@"the Registrant proceed to confirm the payment")]
        public void GivenTheRegistrantProceedToConfirmThePayment()
        {
            TheRegistrantProceedToConfirmThePayment();
        }

        #endregion

        #region When

        [When(@"the Registrant proceed to make the Reservation")]
        public void WhenTheRegistrantProceedToMakeTheReservation()
        {
            TheRegistrantProceedToMakeTheReservation();
        }

        [When(@"the Registrant proceed to make the Reservation with seats already reserved")]
        public void WhenTheRegistrantProceedToMakeTheReservationWithSeatsAlreadyReserved()
        {
            TheRegistrantProceedToMakeTheReservationWithSeatsAlreadyReserved();
        }

        [When(@"the Registrant proceed to Checkout:Payment")]
        public void WhenTheRegistrantProceedToCheckoutPayment()
        {
            TheRegistrantProceedToCheckoutPayment();
        }

        [When(@"the Registrant proceed to confirm the payment")]
        public void WhenTheRegistrantProceedToConfirmThePayment()
        {
            TheRegistrantProceedToConfirmThePayment();
        }

        #endregion

        #region Then

        [Then(@"the Reservation is confirmed for all the selected Order Items")]
        public void ThenTheReservationIsConfirmedForAllTheSelectedOrderItems()
        {
            Assert.True(ScenarioContext.Current.Get<W.Browser>().SafeContainsText(Constants.UI.ReservationSuccessfull),
                string.Format("The following text was not found on the page: {0}", Constants.UI.ReservationSuccessfull));
        }

        [Then(@"the countdown started")]
        public void ThenTheCountdownStarted()
        {
            var countdown = ScenarioContext.Current.Get<W.Browser>().Div("countdown_time").Text;

            Assert.False(string.IsNullOrWhiteSpace(countdown));
            var countdownTime = TimeSpan.ParseExact(countdown, @"mm\:ss", CultureInfo.InvariantCulture);
            Assert.True(countdownTime.Minutes > 0 && countdownTime.Minutes < 15);
        }

        [Then(@"the payment options should be offered for a total of \$(.*)")]
        public void ThenThePaymentOptionsShouldBeOfferedForATotalOf(int value)
        {
            Assert.True(ScenarioContext.Current.Get<W.Browser>().SafeContainsText(value.ToString(CultureInfo.InvariantCulture)),
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
            var browser = ScenarioContext.Current.Get<W.Browser>();
            foreach (var row in table.Rows)
            {
                Assert.False(browser.ContainsValueInTableRow(row["seat type"], ""),
                    string.Format("The following text was not found on the page: {0}", row["seat type"]));
            }
        }

        [Then(@"the total should read \$(.*)")]
        public void ThenTheTotalShouldRead(int value)
        {
            TheTotalShouldRead(value);
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

        #endregion

        #region Events

        [AfterScenario]
        public static void AfterScenario()
        {
            // Restore the available setes previous to the reservation
            Guid reservationId;
            if (ScenarioContext.Current.TryGetValue("reservationId", out reservationId))
            {
                ConferenceHelper.CancelSeatReservation(ScenarioContext.Current.Get<ConferenceInfo>().Id, reservationId);
            }
        }

        #endregion

        #region Common code

        private void TheseOrderItemsShouldBeReserved(Table table)
        {
            var browser = ScenarioContext.Current.Get<W.Browser>();
            foreach (var row in table.Rows)
            {
                Assert.True(browser.ContainsValueInTableRow(row["seat type"], row["quantity"]),
                    string.Format("The following text was not found on the page: {0} or {1}", row["seat type"], row["quantity"]));
            }
        }

        private void TheRegistrantProceedToConfirmThePayment()
        {
            ScenarioContext.Current.Get<W.Browser>().Click(Constants.UI.AcceptPaymentInputValue);
        }

        private void TheOrderShouldBeCreatedWithTheFollowingOrderItems(Table table)
        {
            var browser = ScenarioContext.Current.Get<W.Browser>();

            // Check id the access code was created
            string accessCode = browser.FindText(new Regex("[A-Z0-9]{6}"));
            Assert.False(string.IsNullOrWhiteSpace(accessCode), "Access Code with pattern '[A-Z0-9]{6}' not found");

            // Navigate to the Seat Assignement page
            browser.Click(Constants.UI.ProceedToSeatAssignementId);

            TheseOrderItemsShouldBeReserved(table);
        }

        private void TheMessageWillShowUp(string message)
        {
            Assert.True(ScenarioContext.Current.Get<W.Browser>().SafeContainsText(message),
                string.Format("The following text was not found on the page: {0}", message));
        }

        private void TheTotalShouldRead(int value)
        {
            Assert.True(ScenarioContext.Current.Get<W.Browser>().SafeContainsText(value.ToString(CultureInfo.InvariantCulture)),
                string.Format("The following text was not found on the page: {0}", value));
        }

        private void TheRegistrantProceedToMakeTheReservation()
        {
            ScenarioContext.Current.Get<W.Browser>().ClickAndWait(Constants.UI.NextStepId, Constants.UI.ReservationSuccessfull);
        }

        public void TheRegistrantProceedToMakeTheReservationWithSeatsAlreadyReserved()
        {
            ScenarioContext.Current.Get<W.Browser>().ClickAndWait(Constants.UI.NextStepId, Constants.UI.ReservationUnsuccessfull);
        }

        public void TheRegistrantProceedToCheckoutPayment()
        {
            ScenarioContext.Current.Get<W.Browser>().Click(Constants.UI.NextStepId);
        }

        #endregion
    }
}
