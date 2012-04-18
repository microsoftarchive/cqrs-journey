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

namespace Conference.Web.Public.Controllers
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Web.Mvc;
    using Common;
    using Conference.Web.Public.Models;
    using Registration.Commands;
    using Registration.ReadModel;

    public class RegistrationController : Controller
    {
        private const int WaitTimeoutInSeconds = 5;

        private readonly ICommandBus commandBus;
        private readonly IOrderDao orderDao;
        private readonly IConferenceDao conferenceDao;

        public RegistrationController(ICommandBus commandBus, IOrderDao orderDao, IConferenceDao conferenceDao)
        {
            this.commandBus = commandBus;
            this.orderDao = orderDao;
            this.conferenceDao = conferenceDao;
        }

        [HttpGet]
        public ActionResult StartRegistration(string conferenceCode)
        {
            ViewBag.OrderId = Guid.NewGuid();

            return View(this.conferenceDao.GetPublishedSeatTypes(this.Conference.Id));
        }

        [HttpPost]
        public ActionResult StartRegistration(string conferenceCode, RegisterToConference command)
        {
            if (!ModelState.IsValid)
            {
                return StartRegistration(conferenceCode);
            }

            // TODO: validate incoming seat types correspond to the conference.

            command.ConferenceId = this.Conference.Id;
            this.commandBus.Send(command);

            return RedirectToAction("SpecifyRegistrantDetails", new { conferenceCode = conferenceCode, orderId = command.OrderId });
        }

        [HttpGet]
        public ActionResult SpecifyRegistrantDetails(string conferenceCode, Guid orderId)
        {
            var order = this.WaitUntilUpdated(orderId);
            if (order == null)
            {
                return View("ReservationUnknown");
            }

            if (order.State == OrderDTO.States.Rejected)
            {
                return View("ReservationRejected");
            }

            // NOTE: we use the view bag to pass out of band details needed for the UI.
            this.ViewBag.ExpirationDateUTC = order.ReservationExpirationDate;

            // We just render the command which is later posted back.
            return View(new AssignRegistrantDetails { OrderId = orderId });
        }

        [HttpPost]
        public ActionResult SpecifyRegistrantDetails(string conferenceCode, AssignRegistrantDetails command)
        {
            if (!ModelState.IsValid)
            {
                return SpecifyRegistrantDetails(conferenceCode, command.OrderId);
            }

            this.commandBus.Send(command);

            return RedirectToAction("SpecifyPaymentDetails", new { conferenceCode = conferenceCode, orderId = command.OrderId });
        }

        [HttpGet]
        public ActionResult SpecifyPaymentDetails(string conferenceCode, Guid orderId)
        {
            var order = this.orderDao.GetOrderDetails(orderId);
            var viewModel = this.CreateViewModel(conferenceCode, order);

            this.ViewBag.ExpirationDateUTC = order.ReservationExpirationDate;

            return View(viewModel);
        }

        [HttpPost]
        public ActionResult SpecifyPaymentDetails(string conferenceCode, Guid orderId, PaymentDetails paymentDetails)
        {
            return RedirectToAction("Display", "Payment", new { conferenceCode = conferenceCode, orderId = orderId });
        }

        [HttpGet]
        public ActionResult TransactionCompleted(string conferenceCode, Guid orderId, string transactionResult)
        {
            if (transactionResult == "accepted")
            {
                return RedirectToAction("ThankYou", new { conferenceCode = conferenceCode, orderId = orderId });
            }
            else
            {
                return RedirectToAction("SpecifyPaymentDetails", new { conferenceCode = conferenceCode, orderId = orderId });
            }
        }

        [HttpGet]
        public ActionResult DisplayOrderStatus(string conferenceCode, Guid orderId)
        {
            // TODO: What is this? There is no backing view for this action!
            return View();
        }

        [HttpGet]
        public ActionResult ThankYou(string conferenceCode, Guid orderId)
        {
            var order = this.orderDao.GetOrderDetails(orderId);

            return View(order);
        }

        private OrderViewModel CreateViewModel(string conferenceCode)
        {
            var seats = this.conferenceDao.GetPublishedSeatTypes(this.Conference.Id);
            var viewModel =
                new OrderViewModel
                {
                    ConferenceId = this.Conference.Id,
                    ConferenceCode = this.Conference.Code,
                    ConferenceName = this.Conference.Name,
                    Items = seats.Select(s => new OrderItemViewModel { SeatTypeId = s.Id, SeatTypeDescription = s.Description, Price = s.Price }).ToList(),
                };

            return viewModel;
        }

        private OrderViewModel CreateViewModel(string conferenceCode, OrderDTO order)
        {
            var viewModel = this.CreateViewModel(conferenceCode);
            viewModel.Id = order.OrderId;

            // TODO check DTO matches view model


            foreach (var line in order.Lines)
            {
                var seat = viewModel.Items.First(s => s.SeatTypeId == line.SeatType);
                seat.Quantity = line.ReservedSeats;
            }

            return viewModel;
        }

        private OrderDTO WaitUntilUpdated(Guid orderId)
        {
            var deadline = DateTime.Now.AddSeconds(WaitTimeoutInSeconds);

            while (DateTime.Now < deadline)
            {
                var order = this.orderDao.GetOrderDetails(orderId);

                if (order != null && order.State != OrderDTO.States.Created)
                {
                    return order;
                }

                Thread.Sleep(500);
            }

            return null;
        }

        private ConferenceAliasDTO conference;
        protected ConferenceAliasDTO Conference
        {
            get
            {
                if (this.conference == null)
                {
                    var conferenceCode = (string)ControllerContext.RouteData.Values["conferenceCode"];
                    this.conference = this.conferenceDao.GetConferenceAlias(conferenceCode);
                }

                return this.conference;
            }
        }

        protected override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            base.OnResultExecuting(filterContext);

            if (filterContext.Result is ViewResultBase)
            {
                this.ViewBag.Conference = this.Conference;
            }
        }
    }
}
