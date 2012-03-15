// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// Copyright (c) Microsoft Corporation and contributors http://cqrsjourney.github.com/contributors/members
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
                    Seats = viewModel.Items.Select(x => new RegisterToConference.Seat { SeatTypeId = x.SeatTypeId, Quantity = x.Quantity }).ToList()
                };

            this.commandBus.Send(command);

            var orderDTO = this.WaitUntilUpdated(viewModel.Id);

            if (orderDTO != null)
            {
                if (orderDTO.State == "Booked")
                {
                    return RedirectToAction("SpecifyPaymentDetails", new { conferenceCode = conferenceCode, orderId = viewModel.Id });
                }
                else if (orderDTO.State == "Rejected")
                {
                    return View("ReservationRejected", viewModel);
                }
            }

            return View("ReservationUnknown", viewModel);
        }

        [HttpGet]
        public ActionResult SpecifyPaymentDetails(string conferenceCode, Guid orderId)
        {
            var orderDTO = this.repositoryFactory().Find<OrderDTO>(orderId);
            var viewModel = this.CreateViewModel(conferenceCode, orderDTO);

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
                return RedirectToAction("Display", "Conference", new { conferenceCode = conferenceCode });
            }
        }

        [HttpGet]
        public ActionResult ThankYou(string conferenceCode, Guid orderId)
        {
            return View();
        }

        private OrderViewModel CreateViewModel(string conferenceCode)
        {
            var conference = this.repositoryFactory().Query<ConferenceDTO>().FirstOrDefault(c => c.Code == conferenceCode);

            //// TODO check null case

            var viewModel =
                new OrderViewModel
                {
                    ConferenceId = conference.Id,
                    ConferenceCode = conference.Code,
                    ConferenceName = conference.Name,
                    Items = conference.Seats.Select(s => new OrderItemViewModel { SeatTypeId = s.Id, SeatTypeDescription = s.Description, Price = s.Price }).ToList()
                };

            return viewModel;
        }

        private OrderViewModel CreateViewModel(string conferenceCode, OrderDTO orderDTO)
        {
            var viewModel = this.CreateViewModel(conferenceCode);
            viewModel.Id = orderDTO.OrderId;

            foreach (var line in orderDTO.Lines)
            {
                var seat = viewModel.Items.FirstOrDefault(s => s.SeatTypeId == line.SeatTypeId);
                seat.Quantity = line.Quantity;
            }

            return viewModel;
        }

        private OrderViewModel UpdateViewModel(string conferenceCode, OrderViewModel viewModel)
        {
            var reservation = this.CreateViewModel(conferenceCode);
            reservation.Id = viewModel.Id;

            for (int i = 0; i < reservation.Items.Count; i++)
            {
                var quantity = viewModel.Items[i].Quantity;
                reservation.Items[i].Quantity = quantity;
            }

            return reservation;
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

                    if (orderDTO != null && orderDTO.State != "Created")
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
