using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechTalk.SpecFlow;
using System.Globalization;
using System.Text.RegularExpressions;
using W = WatiN.Core;

namespace Conference.Specflow.Steps.Registration.EndToEnd
{
    [Binding]
    public class EndToEndCommonSteps
    {
        [Given(@"the list of the available Order Items for the CQRS summit 2012 conference (.*)")]
        public void GivenTheListOfTheAvailableOrderItemsForTheCQRSSummit2012Conference(string conferenceSlug, Table table)
        {
            //Setinto Feature scope because this step should be Background 
            FeatureContext.Current.Add("conferenceSlug", conferenceSlug);

            // Populate Conference data
            ConferenceHelper.PopulateConfereceData(table, conferenceSlug);

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
            ScenarioContext.Current.Get<W.Browser>().Click(Constants.UI.NextStepButtonID);
        }

        [When(@"the Registrant proceed to Checkout:Payment")]
        public void WhenTheRegistrantProceedToCheckoutPayment()
        {
            ScenarioContext.Current.Get<W.Browser>().Click(Constants.UI.NextStepButtonID);
        }

        [Then(@"the Reservation is confirmed for all the selected Order Items")]
        public void ThenTheReservationIsConfirmedForAllTheSelectedOrderItems()
        {
            Assert.IsTrue(ScenarioContext.Current.Get<W.Browser>().SafeContainsText(Constants.UI.ReservationSucessfull),
                string.Format("The following text was not found on the page: {0}", Constants.UI.ReservationSucessfull)); 
        }

        [Then(@"the total should read \$(.*)")]
        public void ThenTheTotalShouldRead(int value)
        {
            Assert.IsTrue(ScenarioContext.Current.Get<W.Browser>().SafeContainsText(value.ToString()),
                string.Format("The following text was not found on the page: {0}", value)); 
        }

        [Then(@"the message '(.*)' will show up")]
        public void ThenTheMessageWillShowUp(string message)
        {
            Assert.IsTrue(ScenarioContext.Current.Get<W.Browser>().SafeContainsText(message),
                string.Format("The following text was not found on the page: {0}", message)); 
        }

        [Then(@"the countdown started")]
        public void ThenTheCountdownStarted()
        {
            var countdown = ScenarioContext.Current.Get<W.Browser>().Div("countdown_time").Text;

            Assert.IsFalse(string.IsNullOrWhiteSpace(countdown));
            TimeSpan countdownTime = TimeSpan.ParseExact(countdown, @"mm\:ss", CultureInfo.InvariantCulture);
            Assert.IsTrue(countdownTime.Minutes > 0 && countdownTime.Minutes < 15);
        }

        [Then(@"the payment options should be offered for a total of \$(.*)")]
        public void ThenThePaymentOptionsShouldBeOfferedForATotalOf(int value)
        {
            Assert.IsTrue(ScenarioContext.Current.Get<W.Browser>().SafeContainsText(value.ToString()),
                string.Format("The following text was not found on the page: {0}", value)); 
        }

        [Then(@"these Order Items should be listed")]
        public void ThenTheseOrderItemsShouldBeListed(Table table)
        {
            var browser = ScenarioContext.Current.Get<W.Browser>(); 
            foreach (var row in table.Rows)
            {
                string value = row["seat type"];
                Assert.IsTrue(browser.SafeContainsText(value),
                    string.Format("The following text was not found on the page: {0}", value)); 
            }
        }

        [Then(@"these Order Items should not be listed")]
        public void ThenTheseOrderItemsShouldNotBeListed(Table table)
        {
            var browser = ScenarioContext.Current.Get<W.Browser>(); 
            foreach (var row in table.Rows)
            {
                string value = row["seat type"];
                Assert.IsFalse(browser.SafeContainsText(value),
                    string.Format("The following text was found on the page and not expected: {0}", value));
            }
        }
    }
}
