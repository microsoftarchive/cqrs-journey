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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TechTalk.SpecFlow;
using Conference.Web.Public.Controllers;
using System.Web.Mvc;
using Registration.ReadModel;
using Registration.Commands;
using Registration;
using Conference.Web.Public.Models;
using System.Web.Routing;
using Xunit;

namespace Conference.Specflow.Steps.Registration
{
    [Binding]
    [Scope(Tag = "SelfRegistrationEndToEndWithInfrastructure")]
    public class SelfRegistrationEndToEndWithInfrastructureSteps
    {
        [Given(@"the list of the available Order Items for the CQRS summit 2012 conference with the slug code (.*)")]
        public void GivenTheListOfTheAvailableOrderItemsForTheCQRSSummit2012Conference(string conferenceSlug, Table table)
        {
            // Populate Conference data
            var conferenceInfo = ConferenceHelper.PopulateConfereceData(table, conferenceSlug);

            // Store for later use
            ScenarioContext.Current.Set<ConferenceInfo>(conferenceInfo);

            // Get the RegistrationController for this conference
            var controller = RegistrationHelper.GetRegistrationController(conferenceInfo.Slug);

            // Store for later use
            ScenarioContext.Current.Set<RegistrationController>(controller);
        }

        [Given(@"the selected Order Items")]
        public void GivenTheSelectedOrderItems(Table table)
        {
            var conference = ScenarioContext.Current.Get<ConferenceInfo>();
            var controller = ScenarioContext.Current.Get<RegistrationController>();
            var seats = ((ViewResult)controller.StartRegistration()).Model as IList<ConferenceSeatTypeDTO>;
            var registration = new RegisterToConference() { ConferenceId = conference.Id, OrderId = controller.ViewBag.OrderId } ;
 
            foreach (var row in table.Rows)
            {
                var seatType = seats.FirstOrDefault(s => s.Name == row["seat type"]);
                Assert.NotNull(seatType);
                registration.Seats.Add(new SeatQuantity(seatType.Id, Int32.Parse(row["quantity"])));
            }

            ScenarioContext.Current.Set<RegisterToConference>(registration);
        }

        [Given(@"the Registrant proceed to make the Reservation")]
        public void GivenTheRegistrantProceedToMakeTheReservation()
        {
            var controller = ScenarioContext.Current.Get<RegistrationController>();
            var registration = ScenarioContext.Current.Get<RegisterToConference>();
            var redirect = controller.StartRegistration(registration) as RedirectToRouteResult;
            
            // Perform external redirection
            var timeout =  DateTime.Now.Add(Constants.UI.WaitTimeout);
            RegistrationViewModel model = null;
            while(DateTime.Now < timeout && model == null)
            {
                model = ((ViewResult)controller.SpecifyRegistrantAndPaymentDetails(
                    (Guid)redirect.RouteValues["orderId"])).Model as RegistrationViewModel;
            }

            Assert.NotNull(model);
            ScenarioContext.Current.Set<RegistrationViewModel>(model);
        }

        [Given(@"these Order Items should be reserved")]
        public void GivenTheseOrderItemsShouldBeReserved(Table table)
        {
            var model = ScenarioContext.Current.Get<RegistrationViewModel>();
            foreach (var row in table.Rows)
            {
                var seat = model.Order.Items.FirstOrDefault(i => i.SeatTypeDescription == row["seat type"]);
                Assert.NotNull(seat);
                Assert.Equal(Int32.Parse(row["quantity"]), seat.Quantity);
            }
        }

        [Given(@"these Order Items should not be reserved")]
        public void GivenTheseOrderItemsShouldNotBeReserved(Table table)
        {
            var model = ScenarioContext.Current.Get<RegistrationViewModel>();
            foreach (var row in table.Rows)
            {
                var seat = model.Order.Items.FirstOrDefault(i => i.SeatTypeDescription == row["seat type"]);
                Assert.NotNull(seat);
                Assert.Equal(0, seat.Quantity);
            }
        }

        [Given(@"the Registrant enter these details")]  
        public void GivenTheRegistrantEnterTheseDetails(Table table)
        {
            var model = ScenarioContext.Current.Get<RegistrationViewModel>();
            model.RegistrantDetails.FirstName = table.Rows[0]["First name"];
            model.RegistrantDetails.LastName = table.Rows[0]["Last name"];
            model.RegistrantDetails.Email = table.Rows[0]["email address"];
        }

        [Given(@"the Registrant proceed to Checkout:Payment")]
        public void GivenTheRegistrantProceedToCheckoutPayment()
        {
            var model = ScenarioContext.Current.Get<RegistrationViewModel>();
            var controller = ScenarioContext.Current.Get<RegistrationController>();

            var result = controller.SpecifyRegistrantAndPaymentDetails(
                model.RegistrantDetails, RegistrationController.ThirdPartyProcessorPayment) as RedirectToRouteResult;

            ScenarioContext.Current.Set<RouteValueDictionary>(result.RouteValues);
        }

        [When(@"the Registrant proceed to confirm the payment")]
        public void WhenTheRegistrantProceedToConfirmThePayment()
        {
            var conference = ScenarioContext.Current.Get<ConferenceInfo>();
            var paymentController = RegistrationHelper.GetPaymentController();
            var values = ScenarioContext.Current.Get<RouteValueDictionary>();
            
            paymentController.ThirdPartyProcessorPaymentAccepted(conference.Slug, (Guid)values["paymentId"], " ");
        }

        [Then(@"the Order should be created with the following Order Items")]
        public void ThenTheOrderShouldBeCreatedWithTheFollowingOrderItems(Table table)
        {
            var conference = ScenarioContext.Current.Get<ConferenceInfo>();
            var controller = ScenarioContext.Current.Get<RegistrationController>();
            var model = ScenarioContext.Current.Get<RegistrationViewModel>();
            
            var order = ((ViewResult)controller.ThankYou(conference.Slug, model.Order.Id)).Model as OrderDTO;

            foreach (var row in table.Rows)
            {
                string value = row["seat type"];
                var orderItem = order.Lines.FirstOrDefault(
                    l => l.SeatType == conference.Seats.First(s => s.Description == row["seat type"]).Id);

                Assert.NotNull(orderItem);
                Assert.Equal(Int32.Parse(row["quantity"]), orderItem.ReservedSeats);
            }
        }

        [AfterScenario]
        public static void AfterScenario()
        {
            RegistrationController regController;
            if (ScenarioContext.Current.TryGetValue<RegistrationController>(out regController))
            {
                regController.Dispose();
            }

            PaymentController paymentController;
            if (ScenarioContext.Current.TryGetValue<PaymentController>(out paymentController))
            {
                paymentController.Dispose();
            }
        }
    }
}
