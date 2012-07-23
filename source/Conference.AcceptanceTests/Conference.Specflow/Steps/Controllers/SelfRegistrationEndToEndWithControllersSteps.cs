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
using System.Linq;
using System.Threading;
using System.Web.Mvc;
using System.Web.Routing;
using Conference.Specflow.Support;
using Conference.Web.Public.Controllers;
using Conference.Web.Public.Models;
using Registration;
using Registration.Commands;
using Registration.ReadModel;
using TechTalk.SpecFlow;
using Xunit;

namespace Conference.Specflow.Steps
{
    [Binding]
    [Scope(Tag = "SelfRegistrationEndToEndWithControllers")]
    public class SelfRegistrationEndToEndWithControllersSteps : IDisposable
    {
        private ConferenceInfo conferenceInfo;
        private RegistrationController registrationController;
        private RegisterToConference registration;
        private RegistrationViewModel registrationViewModel;
        private RouteValueDictionary routeValues;
        private DraftOrder draftOrder;
        private OrderViewModel orderViewModel;
        private bool disposed;

        [Given(@"the selected Order Items")]
        [Scope(Tag = "RegistrationProcessHardeningIntegration")]
        [Scope(Tag = "SelfRegistrationEndToEndWithIntegration")]
        public void GivenTheSelectedOrderItems(Table table)
        {
            conferenceInfo = ScenarioContext.Current.Get<ConferenceInfo>();
            registrationController = RegistrationHelper.GetRegistrationController(conferenceInfo.Slug);
            
            orderViewModel = RegistrationHelper.GetModel<OrderViewModel>(registrationController.StartRegistration().Result);
            Assert.NotNull(orderViewModel);

            registration = new RegisterToConference { ConferenceId = conferenceInfo.Id, OrderId = orderViewModel.OrderId };
            
            foreach (var row in table.Rows)
            {
                var orderItemViewModel = orderViewModel.Items.FirstOrDefault(s => s.SeatType.Description == row["seat type"]);
                Assert.NotNull(orderItemViewModel);
                int qt;
                if (!row.ContainsKey("quantity") || !Int32.TryParse(row["quantity"], out qt)) qt = orderItemViewModel.SeatType.Quantity;
                registration.Seats.Add(new SeatQuantity(orderItemViewModel.SeatType.Id, qt));
            }

            // Store for sharing between steps implementations
            ScenarioContext.Current.Set(registration);
            ScenarioContext.Current.Set(registrationController.ConferenceAlias);
        }

        [Given(@"the Registrant proceeds to make the Reservation")]
        public void GivenTheRegistrantProceedToMakeTheReservation()
        {
            //Assert.True(redirect != null, "Reservation not accepted. May be waitlisted.");

            // Perform external redirection
            var timeout =  DateTime.Now.Add(Constants.UI.WaitTimeout);

            while (DateTime.Now < timeout && registrationViewModel == null)
            {
                var redirect = registrationController.StartRegistration(registration, orderViewModel.OrderVersion) as RedirectToRouteResult;
                if (redirect != null)
                {
                    //ReservationUnknown
                    var result = registrationController.SpecifyRegistrantAndPaymentDetails(
                        (Guid) redirect.RouteValues["orderId"], orderViewModel.OrderVersion).Result;

                    Assert.IsNotType<RedirectToRouteResult>(result);
                    registrationViewModel = RegistrationHelper.GetModel<RegistrationViewModel>(result);
                }
            }

            Assert.False(registrationViewModel == null, "Could not make the reservation and get the RegistrationViewModel");
        }

        [Given(@"these Order Items should be reserved")]
        public void GivenTheseOrderItemsShouldBeReserved(Table table)
        {
            foreach (var row in table.Rows)
            {
                var seat = registrationViewModel.Order.Lines.FirstOrDefault(i => i.Description == row["seat type"]);
                Assert.NotNull(seat);
                Assert.Equal(Int32.Parse(row["quantity"]), seat.Quantity);
            }
        }

        [Given(@"these Order Items should not be reserved")]
        public void GivenTheseOrderItemsShouldNotBeReserved(Table table)
        {
            foreach (var row in table.Rows)
            {
                var seat = registrationViewModel.Order.Lines.FirstOrDefault(i => i.Description == row["seat type"]);
                Assert.Null(seat);
            }
        }

        [Given(@"the Registrant enters these details")]  
        public void GivenTheRegistrantEnterTheseDetails(Table table)
        {
            GivenTheRegistrantEnterTheseDetails(table.Rows[0]["first name"], table.Rows[0]["last name"],
                                                table.Rows[0]["email address"]);
        }

        public void GivenTheRegistrantEnterTheseDetails(string firstName, string lastName, string email)
        {
            registrationViewModel.RegistrantDetails.FirstName = firstName;
            registrationViewModel.RegistrantDetails.LastName = lastName;
            registrationViewModel.RegistrantDetails.Email = email;
        }

        [Given(@"the Registrant proceeds to Checkout:Payment")]
        public void GivenTheRegistrantProceedToCheckoutPayment()
        {
            var result = registrationController.SpecifyRegistrantAndPaymentDetails(
                registrationViewModel.RegistrantDetails,
                RegistrationController.ThirdPartyProcessorPayment, orderViewModel.OrderVersion).Result as RedirectToRouteResult;

            var timeout = DateTime.Now.Add(Constants.UI.WaitTimeout);
            while((result == null || !result.RouteValues.ContainsKey("paymentId")) &&
                DateTime.Now < timeout)
            {
                result = registrationController.StartPayment(registrationViewModel.RegistrantDetails.OrderId,
                    RegistrationController.ThirdPartyProcessorPayment, orderViewModel.OrderVersion).Result as RedirectToRouteResult;
            }

            Assert.NotNull(result);
            Assert.True(result.RouteValues.ContainsKey("paymentId"), "No 'paymentId' key, Checkout payment not completed");

            routeValues = result.RouteValues;
        }

        [When(@"the Registrant proceeds to confirm the payment")]
        public void WhenTheRegistrantProceedToConfirmThePayment()
        {
            using (var paymentController = RegistrationHelper.GetPaymentController())
            {
                paymentController.ThirdPartyProcessorPaymentAccepted(
                    conferenceInfo.Slug, (Guid) routeValues["paymentId"], " ");
            }
        }

        [Then(@"the Order should be created with the following Order Items")]
        public void ThenTheOrderShouldBeCreatedWithTheFollowingOrderItems(Table table)
        {
            draftOrder = RegistrationHelper.GetModel<DraftOrder>(registrationController.ThankYou(registrationViewModel.Order.OrderId));
            Assert.NotNull(draftOrder);

            foreach (var row in table.Rows)
            {
                var orderItem = draftOrder.Lines.FirstOrDefault(
                    l => l.SeatType == conferenceInfo.Seats.First(s => s.Description == row["seat type"]).Id);

                Assert.NotNull(orderItem);
                Assert.Equal(Int32.Parse(row["quantity"]), orderItem.ReservedSeats);
            }

            // Wait for event processing
            Thread.Sleep(Constants.WaitTimeout);
        }

        [Then(@"the Registrant assigns these seats")]
        public void ThenTheRegistrantAssignsTheseSeats(Table table)
        {
            using (var orderController = RegistrationHelper.GetOrderController(conferenceInfo.Slug))
            {
                PricedOrder pricedOrder = null;
                var timeout = DateTime.Now.Add(Constants.UI.WaitTimeout);
                while((pricedOrder == null || !pricedOrder.AssignmentsId.HasValue) && DateTime.Now < timeout)
                {
                    pricedOrder = RegistrationHelper.GetModel<PricedOrder>(orderController.Display(draftOrder.OrderId));
                }

                Assert.NotNull(pricedOrder);
                Assert.True(pricedOrder.AssignmentsId.HasValue);

                var orderSeats =
                    RegistrationHelper.GetModel<OrderSeats>(orderController.AssignSeats(pricedOrder.AssignmentsId.Value));

                foreach (var row in table.Rows)
                {
                    var seat = orderSeats.Seats.FirstOrDefault(s => s.SeatName == row["seat type"]);
                    Assert.NotNull(seat);
                    seat.Attendee.FirstName = row["first name"];
                    seat.Attendee.LastName = row["last name"];
                    seat.Attendee.Email = row["email address"];
                }

                orderController.AssignSeats(pricedOrder.AssignmentsId.Value, orderSeats.Seats.ToList());
            }
        }

        [Then(@"these seats are assigned")]
        public void ThenTheseSeatsAreAssigned(Table table)
        {
            ThenTheOrderShouldBeCreatedWithTheFollowingOrderItems(table);
        }

        /// <summary>
        /// Disposes the resources used by SelfRegistrationEndToEndWithControllersSteps.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the resources used by SelfRegistrationEndToEndWithControllersSteps.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.registrationController.Dispose();
                    this.disposed = true;
                }
            }
        }

        ~SelfRegistrationEndToEndWithControllersSteps()
        {
            Dispose(false);
        }
    }
}
