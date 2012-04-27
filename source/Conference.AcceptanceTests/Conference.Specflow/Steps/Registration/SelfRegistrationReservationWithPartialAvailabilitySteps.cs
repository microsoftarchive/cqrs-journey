using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TechTalk.SpecFlow;
using W = WatiN.Core;

namespace Conference.Specflow.Steps.Registration
{
    [Binding]
    public class SelfRegistrationReservationWithPartialAvailabilitySteps
    {
        [Given(@"the list of Order Items offered to be waitlisted and selected by the Registrant")]
        public void GivenTheListOfOrderItemsOfferedToBeWaitlistedAndSelectedByTheRegistrant(Table table)
        {
            var browser = ScenarioContext.Current.Get<W.Browser>();
            foreach (var row in table.Rows)
            {
                browser.SelectListInTableRow(row["seat type"], row["quantity"]);
            }
        }

        [When(@"the Registrant proceed to make the Reservation with missing or invalid data")]
        public void WhenTheRegistrantProceedToMakeTheReservationWithMissingOrInvalidData()
        {
            ScenarioContext.Current.Get<W.Browser>().Click(Constants.UI.NextStepButtonID);
        }
    }
}
