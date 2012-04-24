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
            //ScenarioContext.Current.Pending();
        }

        [Given(@"the '(.*)' Promo code should show a value of -\$(.*)")]
        public void GivenThePromocodeItemShouldShowAValue(string code, int value)
        {
            //ScenarioContext.Current.Pending();
        }

    }
}
