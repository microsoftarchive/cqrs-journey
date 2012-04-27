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
        [When(@"the Registrant proceed to cancel the payment")]
        public void WhenTheRegistrantProceedToCancelThePayment()
        {
            ScenarioContext.Current.Get<W.Browser>().Click(Constants.UI.RejectPaymentInputValue);
        }

    }
}
