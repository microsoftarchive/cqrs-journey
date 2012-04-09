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
            ViewBag.OrderId = Guid.NewGuid();

            var repo = this.repositoryFactory.Invoke();
            using (repo as IDisposable)
            {
                // NOTE: If the ConferenceSeatTypeDTO had the ConferenceId property exposed, this query should be simpler. Why do we need to hide the FKs in the read model?
                var seatTypes = repo.Query<ConferenceDTO>().Where(c => c.Code == conferenceCode).Select(c => c.Seats).FirstOrDefault().ToList();
                return View(seatTypes);
            }
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
            var orderDTO = this.WaitUntilUpdated(orderId);
            if (orderDTO == null)
            {
                return View("ReservationUnknown");
            }

            if (orderDTO.State == Registration.Order.States.Rejected)
            {
                return View("ReservationRejected");
            }

            // NOTE: we use the view bag to pass out of band details needed for the UI.
            this.ViewBag.ExpirationDateUTC = orderDTO.ReservationExpirationDate;

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
            var repo = this.repositoryFactory();
            using (repo as IDisposable)
            {
                var orderDTO = repo.Find<OrderDTO>(orderId);
                var viewModel = this.CreateViewModel(conferenceCode, orderDTO);

                this.ViewBag.ExpirationDateUTC = orderDTO.ReservationExpirationDate;

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
                // TODO: how to do .Include("Seats") with this generic repo?
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

        private ConferenceAliasDTO conference;
        protected ConferenceAliasDTO Conference
        {
            get
            {
                if (this.conference == null)
                {
                    var conferenceCode = ControllerContext.RouteData.Values["conferenceCode"];
                    var repo = this.repositoryFactory();
                    using (repo as IDisposable)
                    {
                        this.conference = repo.Query<ConferenceAliasDTO>()
                            .Where(c => c.Code == conferenceCode)
                            .FirstOrDefault();
                    }
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
