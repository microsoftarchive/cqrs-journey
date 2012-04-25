using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TechTalk.SpecFlow;

namespace Conference.Specflow.Steps.Registration.EndToEnd
{
    [Binding]
    public class SelfRegistrationEndToEndHappySteps
    {
        [Given(@"the Registrant apply the '(.*)' Promotional Code")]
        public void GivenTheRegistrantApplyThePromotionalCode(string code)
        {
            ScenarioContext.Current.Pending();
        }

        [Given(@"the '(.*)' Promo code should show a value of -\$(.*)")]
        public void GivenThePromocodeItemShouldShowAValue(string code, int value)
        {
            ScenarioContext.Current.Pending();
        }

        [Given(@"the Registrant enter these details")]
        public void GivenTheRegistrantEnterTheseDetails(Table table)
        {
            ScenarioContext.Current.Browser().SetInputvalue("RegistrantDetails_FirstName", table.Rows[0]["First name"]);
            ScenarioContext.Current.Browser().SetInputvalue("RegistrantDetails_LastName", table.Rows[0]["Last name"]);
            ScenarioContext.Current.Browser().SetInputvalue("RegistrantDetails_Email", table.Rows[0]["email address"]);
            ScenarioContext.Current.Browser().SetInputvalue("data-val-required", table.Rows[0]["email address"], "Please confirm the e-mail address.");
        }

        [When(@"the Registrant proceed to confirm the payment")]
        public void WhenTheRegistrantProceedToConfirmThePayment()
        {
            ScenarioContext.Current.Browser().Click(Constants.UI.AcceptPaymentInputValue);
        }

    }
}
