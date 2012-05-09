// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
// http://cqrsjourney.github.com/contributors/members
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

using System;
using System.Threading.Tasks;
using Conference.Specflow.Support;
using TechTalk.SpecFlow;
using Xunit;
using Xunit.Sdk;
using W = WatiN.Core;

namespace Conference.Specflow.Steps.Registration
{
    [Binding]
    public class SelfRegistrationReservationWithConcurrencySteps
    {
        private readonly W.Browser browser;

        public SelfRegistrationReservationWithConcurrencySteps()
        {
            browser = ScenarioContext.Current.NewBrowser();
        }

        [Given(@"another Registrant selects these Order Items")]
        public void GivenAnotherRegistrantSelectsTheseOrderItems(Table table)
        {
            CommonSteps.SelectOrderItems(browser, ScenarioContext.Current.Get<ConferenceInfo>(), table);
        }

        [When(@"another Registrant proceed to make the Reservation")]
        public void WhenAnotherRegistrantProceedToMakeTheReservation()
        {
            CommonSteps.MakeTheReservation(browser);
        }

        [Then(@"a second Reservation is offered to select any of these available seats")]
        public void ThenASecondReservationIsOfferedToSelectAnyOfTheseAvailableSeats(Table table)
        {
            SelfRegistrationReservationWithPartialAvailabilitySteps.AvailableSeats(browser, table);
        }

        [When(@"another Registrant proceed to make the Reservation with seats already reserved")]
        public void WhenAnotherRegistrantProceedToMakeTheReservationWithSeatsAlreadyReserved()
        {
            CommonSteps.MakeTheReservationWithSeatsAlreadyReserved(browser);
        }
    }

    [Binding]
    public class SelfRegistrationReservationWithConcurrencyAndInfrastructureSteps
    {
        private bool onlyOneReserved;

        [When(@"two Registrants selects these Order Items")]
        public void WhenTwoRegistrantsSelectsTheseOrderItems(Table table)
        {
            using (var firstRegistrant = new SelfRegistrationEndToEndWithInfrastructureSteps())
            using (var secondRegistrant = new SelfRegistrationEndToEndWithInfrastructureSteps())
            {
                firstRegistrant.GivenTheSelectedOrderItems(table);
                secondRegistrant.GivenTheSelectedOrderItems(table);
                try
                {
                    Parallel.Invoke(firstRegistrant.GivenTheRegistrantProceedToMakeTheReservation,
                                    secondRegistrant.GivenTheRegistrantProceedToMakeTheReservation);
                }
                catch (AggregateException ae)
                {
                    ae.Handle(e => e is FalseException);
                    onlyOneReserved = true;
                }
            }
        }

        [Then(@"only one Registrant is confirmed for all the selected Order Items")]
        public void ThenOnlyOneRegistrantIsConfirmedForAllTheSelectedOrderItems()
        {
            Assert.True(onlyOneReserved);
        }
    }
}
