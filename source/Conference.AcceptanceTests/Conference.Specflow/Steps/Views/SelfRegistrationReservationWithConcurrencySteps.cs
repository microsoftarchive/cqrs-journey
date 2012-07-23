// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
// http://go.microsoft.com/fwlink/p/?LinkID=258575
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Conference.Specflow.Support;
using TechTalk.SpecFlow;
using Xunit;
using Xunit.Sdk;

namespace Conference.Specflow.Steps
{
    [Binding]
    public class SelfRegistrationReservationWithConcurrencySteps : StepDefinition
    {
        public SelfRegistrationReservationWithConcurrencySteps() : base(ScenarioContext.Current.NewBrowser())
        {
        }

        [Given(@"another Registrant select these Order Items")]
        public void GivenAnotherRegistrantSelectsTheseOrderItems(Table table)
        {
            CommonSteps.SelectOrderItems(Browser, ScenarioContext.Current.Get<ConferenceInfo>(), table);
        }

        [When(@"another Registrant proceeds to make the Reservation")]
        public void WhenAnotherRegistrantProceedToMakeTheReservation()
        {
            CommonSteps.MakeTheReservation(Browser);
        }

        [Then(@"a second Reservation is offered to select any of these available seats")]
        public void ThenASecondReservationIsOfferedToSelectAnyOfTheseAvailableSeats(Table table)
        {
            SelfRegistrationReservationWithPartialAvailabilitySteps.AvailableSeats(Browser, table);
        }

        [When(@"another Registrant proceeds to make the Reservation with seats already reserved")]
        public void WhenAnotherRegistrantProceedToMakeTheReservationWithSeatsAlreadyReserved()
        {
            CommonSteps.MakeTheReservationWithSeatsAlreadyReserved(Browser);
        }
    }

    [Binding]
    public class SelfRegistrationReservationWithConcurrencyAndControllersSteps
    {
        private List<string> results;

        [When(@"(.*) Registrants select these Order Items")]
        public void WhenManyRegistrantsSelectsTheseOrderItems(int registrants, Table table)
        {
            Func<string> worker = () =>
            {
                using (var registrant = new SelfRegistrationEndToEndWithControllersSteps())
                {
                    try
                    {
                        registrant.GivenTheSelectedOrderItems(table);
                        registrant.GivenTheRegistrantProceedToMakeTheReservation();
                        //If we get here, the reservation was successful so proceeds to payment
                        registrant.GivenTheRegistrantProceedToCheckoutPayment();
                        registrant.WhenTheRegistrantProceedToConfirmThePayment();
                        registrant.ThenTheOrderShouldBeCreatedWithTheFollowingOrderItems(table);
                    }
                    catch (AssertException e)
                    {
                        return e.Message;
                    }
                    return null; // all went fine so nothing to report.
                }
            };

            Task<string>[] tasks = Enumerable.Range(0, registrants).Select(i => Task.Factory.StartNew(worker)).ToArray();
            Task.WaitAll(tasks);

            results = tasks.Select(t => t.Result).ToList();
        }

        [Then(@"only (.*) Registrants get confirmed registrations for the selected Order Items")]
        public void ThenOnlySomeRegistrantsGetConfirmedRegistrationsForTheSelectedOrderItems(int registrants)
        {
            int reserved = results.Count(s => s == null);
            Assert.True(registrants == reserved,
                "Reservations expected: " + registrants + " and got reserved: " + reserved + "\n\r" +
                string.Join("\r\n", results.Where(s => s != null).ToArray()));
        }
    }
}
