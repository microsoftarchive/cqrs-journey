using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TechTalk.SpecFlow;
using W = WatiN.Core;

namespace Conference.Specflow.Steps.Registration.EndToEnd
{
    [Binding]
    public class SelfRegistrationEndToEndHappySteps
    {
        [When(@"the Registrant proceed to confirm the payment")]
        public void WhenTheRegistrantProceedToConfirmThePayment()
        {
            ScenarioContext.Current.Get<W.Browser>().Click(Constants.UI.AcceptPaymentInputValue);
        }

        [Then(@"the Order should be located from the Find Order page")]
        public void ThenTheOrderShouldBeLocatedFromTheFindOrderPage()
        {
            var browser = ScenarioContext.Current.Get<W.Browser>();
            string accessCode = browser.FindText(new Regex("[A-Z0-9]{6}"));
            Assert.IsFalse(string.IsNullOrWhiteSpace(accessCode));

            // Navigate to Registration page
            browser.GoTo(Constants.FindOrderPage(FeatureContext.Current.Get<ConferenceInfo>().Slug));

            var email = ScenarioContext.Current.Get<string>("email");

            browser.SetInputvalue("name", email, "email");
            browser.SetInputvalue("name", accessCode, "accessCode");
            browser.Click("find");

            Assert.IsTrue(browser.SafeContainsText(accessCode),
                   string.Format("The following text was not found on the page: {0}", accessCode));
        }
    }
}
