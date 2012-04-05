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
    using Registration;
    using Registration.Commands;
    using Registration.ReadModel;

    public class RegistrationController : Controller
    {
        private const int WaitTimeoutInSeconds = 5;
        private static readonly long EpochTicks = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;

        private ICommandBus commandBus;
        private Func<IViewRepository> repositoryFactory;

        public RegistrationController()
            : this(MvcApplication.GetService<ICommandBus>(), MvcApplication.GetService<Func<IViewRepository>>())
        {
        }

        public RegistrationController(ICommandBus commandBus, Func<IViewRepository> repositoryFactory)
        {
            this.commandBus = commandBus;
            this.repositoryFactory = repositoryFactory;
        }

        [HttpGet]
        public ActionResult StartRegistration(string conferenceCode)
        {
            var viewModel = this.CreateViewModel(conferenceCode);
            viewModel.Id = Guid.NewGuid();

            return View(viewModel);
        }

        [HttpPost]
        public ActionResult StartRegistration(string conferenceCode, OrderViewModel contentModel)
        {
            var viewModel = this.UpdateViewModel(conferenceCode, contentModel);

            var command =
                new RegisterToConference
                {
                    OrderId = viewModel.Id,
                    ConferenceId = viewModel.ConferenceId,
                    Seats = viewModel.Items.Select(x => new SeatQuantity { SeatType = x.SeatTypeId, Quantity = x.Quantity }).ToList()
                };

            this.commandBus.Send(command);

            return RedirectToAction("SpecifyRegistrantDetails", new { conferenceCode = conferenceCode, orderId = viewModel.Id });
        }

        [HttpGet]
        public ActionResult SpecifyRegistrantDetails(string conferenceCode, Guid orderId)
        {
            var orderDTO = this.WaitUntilUpdated(orderId);
            if (orderDTO == null)
            {
                return View("ReservationUnknown");
            }

            ConferenceDTO conferenceDTO;
            var repo = this.repositoryFactory();
            using (repo as IDisposable)
            {
                conferenceDTO = repo.Query<ConferenceDTO>()
                    .Where(c => c.Code == conferenceCode)
                    .FirstOrDefault();
            }

            if (orderDTO.State == Registration.Order.States.Rejected)
            {
                return View("ReservationRejected", conferenceDTO);
            }


            // TODO: check for nulls.

            // NOTE: we use the view bag to pass out of band details needed for the UI.
            this.ViewBag.ConferenceName = conferenceDTO.Name;
            this.ViewBag.ConferenceCode = conferenceDTO.Code;
            this.ViewBag.ExpirationDateUTCMilliseconds = orderDTO.ReservationExpirationDate.HasValue ? ((orderDTO.ReservationExpirationDate.Value.Ticks - EpochTicks) / 10000L) : 0L;
            this.ViewBag.OrderId = orderId;

            // We just render the command which is later posted back.
            return View(new AssignRegistrantDetails { OrderId = orderId });
        }

        [HttpPost]
        public ActionResult SpecifyRegistrantDetails(string conferenceCode, Guid orderId, AssignRegistrantDetails command)
        {
            if (!ModelState.IsValid)
            {
                return SpecifyRegistrantDetails(conferenceCode, orderId);
            }

            this.commandBus.Send(command);

            return RedirectToAction("SpecifyPaymentDetails", new { conferenceCode = conferenceCode, orderId = orderId });
        }

        [HttpGet]
        public ActionResult SpecifyPaymentDetails(string conferenceCode, Guid orderId)
        {
            var repo = this.repositoryFactory();
            using (repo as IDisposable)
            {
                var orderDTO = repo.Find<OrderDTO>(orderId);
                var viewModel = this.CreateViewModel(conferenceCode, orderDTO);

                this.ViewBag.ConferenceCode = conferenceCode;
                this.ViewBag.ExpirationDateUTCMilliseconds = orderDTO.ReservationExpirationDate.HasValue ? ((orderDTO.ReservationExpirationDate.Value.Ticks - EpochTicks) / 10000L) : 0L;
                this.ViewBag.OrderId = orderId;

                return View(viewModel);
            }
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
            return View();
        }

        [HttpGet]
        public ActionResult ThankYou(string conferenceCode, Guid orderId)
        {
            OrderDTO order;
            var repo = this.repositoryFactory();
            using (repo as IDisposable)
            {
                order = repo.Query<OrderDTO>().FirstOrDefault(x => x.OrderId == orderId);
            }

            return View(order);
        }

        private OrderViewModel CreateViewModel(string conferenceCode)
        {
            var repo = this.repositoryFactory();

            using (repo as IDisposable)
            {
                var conference = repo.Query<ConferenceDTO>().FirstOrDefault(c => c.Code == conferenceCode);

                //// TODO check null case

                var viewModel =
                    new OrderViewModel
                    {
                        ConferenceId = conference.Id,
                        ConferenceCode = conference.Code,
                        ConferenceName = conference.Name,
                        Items = conference.Seats.Select(s => new OrderItemViewModel { SeatTypeId = s.Id, SeatTypeDescription = s.Description, Price = s.Price }).ToList(),
                    };

                return viewModel;
            }
        }

        private OrderViewModel CreateViewModel(string conferenceCode, OrderDTO orderDTO)
        {
            var viewModel = this.CreateViewModel(conferenceCode);
            viewModel.Id = orderDTO.OrderId;

            // TODO check DTO matches view model


            foreach (var line in orderDTO.Lines)
            {
                var seat = viewModel.Items.First(s => s.SeatTypeId == line.SeatType);
                seat.Quantity = line.ReservedSeats;
            }

            return viewModel;
        }

        private OrderViewModel UpdateViewModel(string conferenceCode, OrderViewModel incomingModel)
        {
            var viewModel = this.CreateViewModel(conferenceCode);
            viewModel.Id = incomingModel.Id;

            // TODO check incoming matches view model

            for (int i = 0; i < viewModel.Items.Count; i++)
            {
                var quantity = incomingModel.Items[i].Quantity;
                viewModel.Items[i].Quantity = quantity;
            }

            return viewModel;
        }

        private OrderDTO WaitUntilUpdated(Guid orderId)
        {
            var deadline = DateTime.Now.AddSeconds(WaitTimeoutInSeconds);

            while (DateTime.Now < deadline)
            {
                var repo = this.repositoryFactory();
                using (repo as IDisposable)
                {
                    var orderDTO = repo.Find<OrderDTO>(orderId);

                    if (orderDTO != null && orderDTO.State != Registration.Order.States.Created)
                    {
                        return orderDTO;
                    }
                }

                Thread.Sleep(500);
            }

            return null;
        }
    }
}
