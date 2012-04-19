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
        [Given(@"the list of the available Order Items for the CQRS summit 2012 conference")]
        public void GivenTheListOfTheAvailableOrderItemsForTheCQRSSummit2012Conference(Table table)
        {
            ScenarioContext.Current.Pending();
        }

        [Given(@"the selected Order Items")]
        public void GivenTheSelectedOrderItems(Table table)
        {
            ScenarioContext.Current.Pending();
        }

        [Given(@"the Promotional Codes")]
        public void GivenThePromotionalCodes(Table table)
        {
            ScenarioContext.Current.Pending();
        }
    }
}
