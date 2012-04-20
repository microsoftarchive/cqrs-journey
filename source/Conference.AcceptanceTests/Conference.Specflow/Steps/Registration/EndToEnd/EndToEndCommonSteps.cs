using TechTalk.SpecFlow;
using Xunit;

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

            }
            //ScenarioContext.Current.Pending();
            //ScenarioContext.Current.Browser()
        }

        [Given(@"the Promotional Codes")]
        public void GivenThePromotionalCodes(Table table)
        {
            //ScenarioContext.Current.Pending();
        }

        [When(@"the Registrant proceed to make the Reservation\tfor the selected Order Items")]
        public void WhenTheRegistrantProceedToMakeTheReservationForTheSelectedOrderItems()
        {
            ScenarioContext.Current.Browser().Click(Constants.UI.RegistrationOrderButtonID);
        }


        [Then(@"the Reservation is confirmed for all the selected Order Items")]
        public void ThenTheReservationIsConfirmedForAllTheSelectedOrderItems()
        {
            Assert.True(ScenarioContext.Current.Browser().ContainsText(Constants.UI.RegistrationSucessfull),
                string.Format("The following text was not found on the page: {0}", Constants.UI.RegistrationSucessfull)); 
        }

        [Then(@"the total should read \$(.*)")]
        public void ThenTheTotalShouldRead(int value)
        {
            Assert.True(ScenarioContext.Current.Browser().ContainsText(value.ToString()),
                string.Format("The following value text was not found on the page: {0}", value)); 
        }

        [Then(@"the countdown started")]
        public void ThenTheCountdownStarted()
        {
            //ScenarioContext.Current.Pending();
        }


    }
}
