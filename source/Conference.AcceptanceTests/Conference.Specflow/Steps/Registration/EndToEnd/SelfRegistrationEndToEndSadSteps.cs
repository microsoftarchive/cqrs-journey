using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TechTalk.SpecFlow;
using W = WatiN.Core;

namespace Conference.Specflow.Steps.Registration.EndToEnd
{
    [Binding]
    public class SelfRegistrationEndToEndSadSteps
    {
        [Given(@"the Registrant enter these details")]
        public void GivenTheRegistrantEnterTheseDetails(Table table)
        {
            var browser = ScenarioContext.Current.Get<W.Browser>(); 
            browser.SetInputvalue("RegistrantDetails_FirstName", table.Rows[0]["First name"]);
            browser.SetInputvalue("RegistrantDetails_LastName", table.Rows[0]["Last name"]);
            browser.SetInputvalue("RegistrantDetails_Email", table.Rows[0]["email address"]);
            browser.SetInputvalue("data-val-required", table.Rows[0]["email address"], "Please confirm the e-mail address.");
            
            ScenarioContext.Current.Add("email", table.Rows[0]["email address"]);
        }

        [When(@"the Registrant proceed to confirm the payment")]
        public void WhenTheRegistrantProceedToConfirmThePayment()
        {
            ScenarioContext.Current.Get<W.Browser>().Click(Constants.UI.AcceptPaymentInputValue);
        }

    }
}
