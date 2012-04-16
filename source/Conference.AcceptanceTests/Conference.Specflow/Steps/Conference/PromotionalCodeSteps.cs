using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Registration;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace Conference.Specflow.Steps.Conference
{
    [Binding]
    public class PromotionalCodeSteps
    {
        private List<SeatQuantity> _seatConfiguration = new List<SeatQuantity>();
        private List<dynamic> _promotionalCodes = new List<dynamic>();

        private dynamic _newPromoCode;
        
        [Given(@"the ?|following Promotional Codes")]
        public void GivenThePromotionalCodes(Table table)
        {
            _promotionalCodes.Clear();
            _promotionalCodes.AddRange(table.CreateSet<dynamic>());
        }

        [Given(@"the following Seat Types")]
        public void GivenTheSeatTypesConfiguration(Table table)
        {
            _seatConfiguration.Clear();
            _seatConfiguration.AddRange(table.CreateSet<SeatQuantity>());
        }

        [Given(@"Add new promotional code is selected")]
        public void GivenTheBusinessCustomerSelectsAddNewPromotionalCodeOption()
        {
            ScenarioContext.Current.Pending();
        }

        [Given(@"the Business Customer enter the 'NEWCODE' Promotional Code and these attributes")]
        public void GivenTheBusinessCustomerEnterTheNEWCODEPromotionalCodeAndTheseAttributes(Table table)
        {
            ScenarioContext.Current.Pending();
        }

        [When(@"the following Promotional Code is entered")]
        public void PromotionalCodeIsEntered(Table table)
        {
            _newPromoCode = table.CreateInstance<dynamic>();
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
