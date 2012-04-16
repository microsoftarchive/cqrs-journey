using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TechTalk.SpecFlow;

namespace Conference.Specflow.Steps.Registration
{
    public class RegistrationSteps
    {
        private string conferenceName;

        [Given(@"the '(.*)' site conference")]
        public void GivenAConferenceNamed(string conference)
        {
            conferenceName = conference;
        }

        [Given(@"the following seating types and prices")]
        public void GivenTheFollowingSeatingTypesAndPrices(Table table)
        {
            ScenarioContext.Current.Pending();
        }


        [Given(@"the following Order Items")]
        public void GivenTheFollowingOrderItems(Table table)
        {
            ScenarioContext.Current.Pending();
        }
    }
}
