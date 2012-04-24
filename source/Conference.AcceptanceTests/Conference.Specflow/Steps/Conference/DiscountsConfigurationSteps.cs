using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TechTalk.SpecFlow;

namespace Conference.Specflow.Steps.Conference
{
    [Binding]
    public class DiscountsConfigurationSteps
    {
        [Given(@"the Seat Types configuration")]
        public void GivenTheSeatTypesConfiguration(Table table)
        {
            ScenarioContext.Current.Pending();
        }

        [Given(@"the Business Customer selects 'Add new Promotional code' option")]
        public void GivenTheBusinessCustomerSelectsAddNewPromotionalCodeOption()
        {
            ScenarioContext.Current.Pending();
        }

        [Given(@"the Business Customer enter the 'NEWCODE' Promotional Code and these attributes")]
        public void GivenTheBusinessCustomerEnterTheNEWCODEPromotionalCodeAndTheseAttributes(Table table)
        {
            ScenarioContext.Current.Pending();
        }

        [When(@"the 'Save' option is selected")]
        public void WhenTheSaveOptionIsSelected()
        {
            ScenarioContext.Current.Pending();
        }

        [Then(@"the new Promotional Code is added to the list of existing codes")]
        public void ThenTheNewPromotionalCodeIsAddedToTheListOfExistingCodes()
        {
            ScenarioContext.Current.Pending();
        }

    }
}
