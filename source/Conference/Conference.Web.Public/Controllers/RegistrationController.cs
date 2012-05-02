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
    using Conference.Web.Public.Models;
    using Infrastructure.Messaging;
    using Payments.Contracts.Commands;
    using Registration.Commands;
    using Registration.ReadModel;

    public class RegistrationController : Controller
    {
        public const string ThirdPartyProcessorPayment = "thirdParty";
        public const string InvoicePayment = "invoice";
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
        [OutputCache(Duration = 0)]
        public ActionResult StartRegistration(Guid? orderId = null)
        {
            OrderViewModel viewModel;
            int orderVersion = 0;

            if (!orderId.HasValue)
            {
                orderId = Guid.NewGuid();
                viewModel = this.CreateViewModel();
                this.ViewBag.ExpirationDateUTC = DateTime.MinValue;
                ViewBag.PartiallFulfilled = false;
            }
            else
            {
                var order = this.orderDao.GetOrderDetails(orderId.Value);
                orderVersion = order.OrderVersion;
                viewModel = this.CreateViewModel(order);
                ViewBag.ExpirationDateUTC = order.ReservationExpirationDate;
                ViewBag.PartiallFulfilled = order.State == OrderDTO.States.PartiallyReserved;
            }

            ViewBag.OrderId = orderId;
            ViewBag.OrderVersion = orderVersion;

            return View(viewModel);
        }

        [HttpPost]
        public ActionResult StartRegistration(RegisterToConference command, int orderVersion)
        {
            if (!ModelState.IsValid)
            {
                return StartRegistration(command.OrderId);
            }

            // TODO: validate incoming seat types correspond to the conference.

            command.ConferenceId = this.Conference.Id;
            this.commandBus.Send(command);

            return RedirectToAction(
                "SpecifyRegistrantAndPaymentDetails",
                new { conferenceCode = this.Conference.Code, orderId = command.OrderId, orderVersion = orderVersion });
        }

        [HttpGet]
        [OutputCache(Duration = 0)]
        public ActionResult SpecifyRegistrantAndPaymentDetails(Guid orderId, int orderVersion)
        {
            var order = this.WaitUntilSeatsAreAssigned(orderId, orderVersion);
            if (order == null)
            {
                return View("ReservationUnknown");
            }

            if (order.State == OrderDTO.States.Rejected)
            {
                return View("ReservationRejected");
            }

            if (order.State == OrderDTO.States.PartiallyReserved)
            {
                return this.RedirectToAction("StartRegistration", new { conferenceCode = this.Conference.Code, orderId, orderVersion = order.OrderVersion });
            }

            if (order.ReservationExpirationDate.HasValue && order.ReservationExpirationDate < DateTime.UtcNow)
            {
                return RedirectToAction("ShowExpiredOrder", new { conferenceCode = this.Conference.Code, orderId = orderId });
            }

            var totalledOrder = orderDao.GetTotalledOrder(orderId);
            if (totalledOrder == null)
            {
                return View("ReservationUnknown");
            }

            // NOTE: we use the view bag to pass out of band details needed for the UI.
            this.ViewBag.ExpirationDateUTC = order.ReservationExpirationDate;

            // We just render the command which is later posted back.
            return View(
                new RegistrationViewModel
                {
                    RegistrantDetails = new AssignRegistrantDetails { OrderId = orderId },
                    Order = totalledOrder
                });
        }

        [HttpPost]
        public ActionResult SpecifyRegistrantAndPaymentDetails(AssignRegistrantDetails command, string paymentType, int orderVersion)
        {
            var orderId = command.OrderId;

            if (!ModelState.IsValid)
            {
                return SpecifyRegistrantAndPaymentDetails(orderId, orderVersion);
            }

            var order = this.orderDao.GetOrderDetails(orderId);

            // TODO check conference and order exist.
            // TODO validate that order belongs to the user.

            if (order == null)
            {
                throw new ArgumentException();
            }

            if (order.ReservationExpirationDate.HasValue && order.ReservationExpirationDate < DateTime.UtcNow)
            {
                return RedirectToAction("ShowExpiredOrder", new { conferenceCode = this.Conference.Code, orderId = orderId });
            }

            switch (paymentType)
            {
                case ThirdPartyProcessorPayment:

                    return InitiateRegistrationWithThirdPartyProcessorPayment(command, orderId);

                case InvoicePayment:
                    break;

                default:
                    break;
            }

            throw new InvalidOperationException();
        }

        [HttpGet]
        [OutputCache(Duration = 0)]
        public ActionResult ShowExpiredOrder(Guid orderId)
        {
            return View();
        }

        [HttpGet]
        [OutputCache(Duration = 0)]
        public ActionResult ThankYou(string conferenceCode, Guid orderId)
        {
            var order = this.orderDao.GetOrderDetails(orderId);

            return View(order);
        }

        private ActionResult InitiateRegistrationWithThirdPartyProcessorPayment(AssignRegistrantDetails command, Guid orderId)
        {
            var paymentCommand = CreatePaymentCommand(orderId);

            this.commandBus.Send(new ICommand[] { command, paymentCommand });

            var paymentAcceptedUrl = this.Url.Action("ThankYou", new { conferenceCode = this.Conference.Code, orderId });
            var paymentRejectedUrl = this.Url.Action("SpecifyRegistrantAndPaymentDetails", new { conferenceCode = this.Conference.Code, orderId });

            return RedirectToAction(
                "ThirdPartyProcessorPayment",
                "Payment",
                new
                {
                    conferenceCode = this.Conference.Code,
                    paymentId = paymentCommand.PaymentId,
                    paymentAcceptedUrl,
                    paymentRejectedUrl
                });
        }

        private InitiateThirdPartyProcessorPayment CreatePaymentCommand(Guid orderId)
        {
            var totalledOrder = this.orderDao.GetTotalledOrder(orderId);
            // TODO: should add the line items?

            var description = "Registration for " + this.Conference.Name;
            var totalAmount = totalledOrder.Total;

            var paymentCommand =
                new InitiateThirdPartyProcessorPayment
                {
                    PaymentId = Guid.NewGuid(),
                    ConferenceId = this.Conference.Id,
                    PaymentSourceId = orderId,
                    Description = description,
                    TotalAmount = totalAmount
                };

            return paymentCommand;
        }

        private OrderViewModel CreateViewModel()
        {
            var seatTypes = this.conferenceDao.GetPublishedSeatTypes(this.Conference.Id);
            var viewModel =
                new OrderViewModel
                {
                    ConferenceId = this.Conference.Id,
                    ConferenceCode = this.Conference.Code,
                    ConferenceName = this.Conference.Name,
                    Items =
                        seatTypes.Select(
                            s =>
                                new OrderItemViewModel
                                {
                                    SeatType = s,
                                    OrderItem = new OrderItemDTO(s.Id, 0),
                                    MaxSeatSelection = 20
                                }).ToList(),
                };

            return viewModel;
        }

        private OrderViewModel CreateViewModel(OrderDTO order)
        {
            var viewModel = this.CreateViewModel();
            viewModel.Id = order.OrderId;

            // TODO check DTO matches view model

            foreach (var line in order.Lines)
            {
                var seat = viewModel.Items.First(s => s.SeatType.Id == line.SeatType);
                seat.OrderItem = line;
                if (line.RequestedSeats > line.ReservedSeats)
                {
                    seat.PartiallyFulfilled = true;
                    seat.MaxSeatSelection = line.ReservedSeats;
                }
            }

            return viewModel;
        }

        private OrderDTO WaitUntilSeatsAreAssigned(Guid orderId, int lastOrderVersion)
        {
            var deadline = DateTime.Now.AddSeconds(WaitTimeoutInSeconds);

            while (DateTime.Now < deadline)
            {
                var order = this.orderDao.GetOrderDetails(orderId);

                if (order != null && order.State != OrderDTO.States.PendingReservation && order.OrderVersion > lastOrderVersion)
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
