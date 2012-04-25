using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechTalk.SpecFlow;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Conference.Specflow.Steps.Registration.EndToEnd
{
    [Binding]
    public class EndToEndCommonSteps
    {
        [Given(@"the list of the available Order Items for the CQRS summit 2012 conference")]
        public void GivenTheListOfTheAvailableOrderItemsForTheCQRSSummit2012Conference(Table table)
        {
            // Populate Conference data
            ConferenceHelper.PopulateConfereceData(table);

            // Navigate to Registration page
            ScenarioContext.Current.Browser().GoTo(Constants.RegistrationPage);
        }

        [Given(@"the selected Order Items")]
        public void GivenTheSelectedOrderItems(Table table)
        {
            foreach (var row in table.Rows)
            {
                ScenarioContext.Current.Browser().SelectListInTableRow(row["seat type"], row["quantity"]);
            }
        }

        [Given(@"the Promotional Codes")]
        public void GivenThePromotionalCodes(Table table)
        {
            ScenarioContext.Current.Pending();
        }

        [Given(@"the Registrant proceed to make the Reservation")]
        public void GivenTheRegistrantProceedToMakeTheReservation()
        {
            WhenTheRegistrantProceedToMakeTheReservation();
        }

        [Given(@"the Registrant proceed to Checkout:Payment")]
        public void GivenTheRegistrantProceedToCheckoutPayment()
        {
            WhenTheRegistrantProceedToCheckoutPayment();
        }

        [When(@"the Registrant proceed to make the Reservation")]
        public void WhenTheRegistrantProceedToMakeTheReservation()
        {
            ScenarioContext.Current.Browser().Click(Constants.UI.NextStepButtonID);
        }

        [When(@"the Registrant proceed to Checkout:Payment")]
        public void WhenTheRegistrantProceedToCheckoutPayment()
        {
            ScenarioContext.Current.Browser().Click(Constants.UI.NextStepButtonID);
        }

        [Then(@"the Reservation is confirmed for all the selected Order Items")]
        public void ThenTheReservationIsConfirmedForAllTheSelectedOrderItems()
        {
            Assert.IsTrue(ScenarioContext.Current.Browser().SafeContainsText(Constants.UI.ReservationSucessfull),
                string.Format("The following text was not found on the page: {0}", Constants.UI.ReservationSucessfull)); 
        }

        [Then(@"the total should read \$(.*)")]
        public void ThenTheTotalShouldRead(int value)
        {
            Assert.IsTrue(ScenarioContext.Current.Browser().SafeContainsText(value.ToString()),
                string.Format("The following text was not found on the page: {0}", value)); 
        }

        [Then(@"the message '(.*)' will show up")]
        public void ThenTheMessageWillShowUp(string message)
        {
            Assert.IsTrue(ScenarioContext.Current.Browser().SafeContainsText(message),
                string.Format("The following text was not found on the page: {0}", message)); 
        }

        [Then(@"the countdown started")]
        public void ThenTheCountdownStarted()
        {
            var countdown = ScenarioContext.Current.Browser().Div("countdown_time").Text;

            Assert.IsFalse(string.IsNullOrWhiteSpace(countdown));
            TimeSpan countdownTime = TimeSpan.ParseExact(countdown, @"mm\:ss", CultureInfo.InvariantCulture);
            Assert.IsTrue(countdownTime.Minutes > 0 && countdownTime.Minutes < 15);
        }

        [Then(@"the payment options should be offered for a total of \$(.*)")]
        public void ThenThePaymentOptionsShouldBeOfferedForATotalOf(int value)
        {
            Assert.IsTrue(ScenarioContext.Current.Browser().SafeContainsText(value.ToString()),
                string.Format("The following text was not found on the page: {0}", value)); 
        }

        [Then(@"these Order Items should be listed")]
        public void ThenTheseOrderItemsShouldBeListed(Table table)
        {
            foreach (var row in table.Rows)
            {
                string value = row["seat type"];
                Assert.IsTrue(ScenarioContext.Current.Browser().SafeContainsText(value),
                    string.Format("The following text was not found on the page: {0}", value)); 
            }
        }

        [Then(@"these Order Items should not be listed")]
        public void ThenTheseOrderItemsShouldNotBeListed(Table table)
        {
            foreach (var row in table.Rows)
            {
                string value = row["seat type"];
                Assert.IsFalse(ScenarioContext.Current.Browser().SafeContainsText(value),
                    string.Format("The following text was found on the page and not expected: {0}", value));
            }
        }

        [Then(@"an email with the Access Code will be send to the registered email\.")]
        public void ThenAnEmailWithTheAccessCodeWillBeSendToTheRegisteredEmail_()
        {
            //ScenarioContext.Current.Pending();
            //TODO: Check email sent with access code
            // Read access code from the page
            string accessCode = ScenarioContext.Current.Browser().FindText(new Regex("[A-Z0-9]{6}"));

            Assert.IsFalse(string.IsNullOrWhiteSpace(accessCode));
        }

    }
}
