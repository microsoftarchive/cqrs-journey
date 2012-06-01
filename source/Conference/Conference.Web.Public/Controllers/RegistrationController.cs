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

    public class RegistrationController : ConferenceTenantController
    {
        public const string ThirdPartyProcessorPayment = "thirdParty";
        public const string InvoicePayment = "invoice";
        private const int DraftOrderWaitTimeoutInSeconds = 5;
        private const int PricedOrderWaitTimeoutInSeconds = 5;

        private readonly ICommandBus commandBus;
        private readonly IOrderDao orderDao;

        public RegistrationController(ICommandBus commandBus, IOrderDao orderDao, IConferenceDao conferenceDao)
            : base(conferenceDao)
        {
            this.commandBus = commandBus;
            this.orderDao = orderDao;
        }

        [HttpGet]
        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult StartRegistration(Guid? orderId = null)
        {
            OrderViewModel viewModel;

            if (!orderId.HasValue)
            {
                viewModel = this.CreateViewModel();
                viewModel.OrderId = Guid.NewGuid();
            }
            else
            {
                var order = this.WaitUntilSeatsAreConfirmed(orderId.Value, 0);

                if (order == null)
                {
                    return View("ReservationUnknown");
                }

                if (order.State == DraftOrder.States.Confirmed)
                {
                    return View("ShowCompletedOrder");
                }

                if (order.ReservationExpirationDate.HasValue && order.ReservationExpirationDate < DateTime.UtcNow)
                {
                    return RedirectToAction("ShowExpiredOrder", new { conferenceCode = this.ConferenceAlias.Code, orderId = orderId });
                }

                viewModel = this.CreateViewModel(order);
            }

            return View(viewModel);
        }

        [HttpPost]
        public ActionResult StartRegistration(RegisterToConference command, int orderVersion)
        {
            var existingOrder = orderVersion != 0 ? this.orderDao.FindDraftOrder(command.OrderId) : null;
            var viewModel = existingOrder == null ? this.CreateViewModel() : this.CreateViewModel(existingOrder);
            viewModel.OrderId = command.OrderId;
            
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            // checks that there are still enough available seats, and the seat type IDs submitted ar valid.
            ModelState.Clear();
            bool needsExtraValidation = false;
            foreach (var seat in command.Seats)
            {
                var modelItem = viewModel.Items.FirstOrDefault(x => x.SeatType.Id == seat.SeatType);
                if (modelItem != null)
                {
                    if (seat.Quantity > modelItem.MaxSelectionQuantity)
                    {
                        modelItem.PartiallyFulfilled = needsExtraValidation = true;
                        modelItem.OrderItem.ReservedSeats = modelItem.MaxSelectionQuantity;
                    }
                }
                else
                {
                    // seat type no longer exists for conference.
                    needsExtraValidation = true;
                }
            }

            if (needsExtraValidation)
            {
                return View(viewModel);
            }

            command.ConferenceId = this.ConferenceAlias.Id;
            this.commandBus.Send(command);

            return RedirectToAction(
                "SpecifyRegistrantAndPaymentDetails",
                new { conferenceCode = this.ConferenceCode, orderId = command.OrderId, orderVersion = orderVersion });
        }

        [HttpGet]
        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult SpecifyRegistrantAndPaymentDetails(Guid orderId, int orderVersion)
        {
            var pricedOrder = this.WaitUntilOrderIsPriced(orderId, orderVersion);
            if (pricedOrder == null)
            {
                return View("PricedOrderUnknown");
            }

            if (pricedOrder.ReservationExpirationDate.HasValue && pricedOrder.ReservationExpirationDate < DateTime.UtcNow)
            {
                return RedirectToAction("ShowExpiredOrder", new { conferenceCode = this.ConferenceAlias.Code, orderId = orderId });
            }

            return View(
                new RegistrationViewModel
                {
                    RegistrantDetails = new AssignRegistrantDetails { OrderId = orderId },
                    Order = pricedOrder
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

            this.commandBus.Send(command);

            return this.StartPayment(orderId, paymentType, orderVersion);
        }

        [HttpPost]
        public ActionResult StartPayment(Guid orderId, string paymentType, int orderVersion)
        {
            var order = this.WaitUntilSeatsAreConfirmed(orderId, orderVersion);
            if (order == null)
            {
                return View("ReservationUnknown");
            }

            if (order.State == DraftOrder.States.PartiallyReserved)
            {
                //TODO: have a clear message in the UI saying there was a hiccup and he actually didn't get all the seats.
                return this.RedirectToAction("StartRegistration", new { conferenceCode = this.ConferenceCode, orderId, orderVersion = order.OrderVersion });
            }

            if (order.State == DraftOrder.States.Confirmed)
            {
                return View("ShowCompletedOrder");
            }

            if (order.ReservationExpirationDate.HasValue && order.ReservationExpirationDate < DateTime.UtcNow)
            {
                return RedirectToAction("ShowExpiredOrder", new { conferenceCode = this.ConferenceAlias.Code, orderId = orderId });
            }

            var pricedOrder = this.orderDao.FindPricedOrder(orderId);
            if (pricedOrder.IsFreeOfCharge)
            {
                return CompleteRegistrationWithoutPayment(orderId);
            }

            switch (paymentType)
            {
                case ThirdPartyProcessorPayment:

                    return CompleteRegistrationWithThirdPartyProcessorPayment(pricedOrder, orderVersion);

                case InvoicePayment:
                    break;

                default:
                    break;
            }

            throw new InvalidOperationException();
        }

        [HttpGet]
        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult ShowExpiredOrder(Guid orderId)
        {
            return View();
        }

        [HttpGet]
        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult ThankYou(Guid orderId)
        {
            var order = this.orderDao.FindDraftOrder(orderId);

            return View(order);
        }

        private ActionResult CompleteRegistrationWithThirdPartyProcessorPayment(PricedOrder order, int orderVersion)
        {
            var paymentCommand = CreatePaymentCommand(order);

            this.commandBus.Send(paymentCommand);

            var paymentAcceptedUrl = this.Url.Action("ThankYou", new { conferenceCode = this.ConferenceAlias.Code, order.OrderId });
            var paymentRejectedUrl = this.Url.Action("SpecifyRegistrantAndPaymentDetails", new { conferenceCode = this.ConferenceAlias.Code, orderId = order.OrderId, orderVersion });

            return RedirectToAction(
                "ThirdPartyProcessorPayment",
                "Payment",
                new
                {
                    conferenceCode = this.ConferenceAlias.Code,
                    paymentId = paymentCommand.PaymentId,
                    paymentAcceptedUrl,
                    paymentRejectedUrl
                });
        }

        private InitiateThirdPartyProcessorPayment CreatePaymentCommand(PricedOrder order)
        {
            // TODO: should add the line items?

            var description = "Registration for " + this.ConferenceAlias.Name;
            var totalAmount = order.Total;

            var paymentCommand =
                new InitiateThirdPartyProcessorPayment
                {
                    PaymentId = Guid.NewGuid(),
                    ConferenceId = this.ConferenceAlias.Id,
                    PaymentSourceId = order.OrderId,
                    Description = description,
                    TotalAmount = totalAmount
                };

            return paymentCommand;
        }

        private ActionResult CompleteRegistrationWithoutPayment(Guid orderId)
        {
            var confirmationCommand = new ConfirmOrder { OrderId = orderId };

            this.commandBus.Send(confirmationCommand);

            return RedirectToAction("ThankYou", new { conferenceCode = this.ConferenceAlias.Code, orderId });
        }

        private OrderViewModel CreateViewModel()
        {
            var seatTypes = this.ConferenceDao.GetPublishedSeatTypes(this.ConferenceAlias.Id);
            var viewModel =
                new OrderViewModel
                {
                    ConferenceId = this.ConferenceAlias.Id,
                    ConferenceCode = this.ConferenceAlias.Code,
                    ConferenceName = this.ConferenceAlias.Name,
                    Items =
                        seatTypes.Select(
                            s =>
                                new OrderItemViewModel
                                {
                                    SeatType = s,
                                    OrderItem = new DraftOrderItem(s.Id, 0),
                                    AvailableQuantityForOrder = Math.Max(s.AvailableQuantity, 0),
                                    MaxSelectionQuantity = Math.Max(Math.Min(s.AvailableQuantity, 20), 0)
                                }).ToList(),
                };

            return viewModel;
        }

        private OrderViewModel CreateViewModel(DraftOrder order)
        {
            var viewModel = this.CreateViewModel();
            viewModel.OrderId = order.OrderId;
            viewModel.OrderVersion = order.OrderVersion;
            viewModel.ReservationExpirationDate = order.ReservationExpirationDate.ToEpochMilliseconds();

            // TODO check DTO matches view model

            foreach (var line in order.Lines)
            {
                var seat = viewModel.Items.First(s => s.SeatType.Id == line.SeatType);
                seat.OrderItem = line;
                seat.AvailableQuantityForOrder = seat.AvailableQuantityForOrder + line.ReservedSeats;
                seat.MaxSelectionQuantity = Math.Min(seat.AvailableQuantityForOrder, 20);
                seat.PartiallyFulfilled = line.RequestedSeats > line.ReservedSeats;
            }

            return viewModel;
        }

        private DraftOrder WaitUntilSeatsAreConfirmed(Guid orderId, int lastOrderVersion)
        {
            var deadline = DateTime.Now.AddSeconds(DraftOrderWaitTimeoutInSeconds);

            while (DateTime.Now < deadline)
            {
                var order = this.orderDao.FindDraftOrder(orderId);

                if (order != null && order.State != DraftOrder.States.PendingReservation && order.OrderVersion > lastOrderVersion)
                {
                    return order;
                }

                Thread.Sleep(500);
            }

            return null;
        }

        private PricedOrder WaitUntilOrderIsPriced(Guid orderId, int lastOrderVersion)
        {
            var deadline = DateTime.Now.AddSeconds(PricedOrderWaitTimeoutInSeconds);

            while (DateTime.Now < deadline)
            {
                var order = this.orderDao.FindPricedOrder(orderId);
                if (order != null && order.OrderVersion > lastOrderVersion)
                {
                    return order;
                }

                Thread.Sleep(500);
            }

            return null;
        }
    }
}
