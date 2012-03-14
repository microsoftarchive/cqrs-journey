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
            var registration = this.CreateRegistration(conferenceCode);
            registration.Id = Guid.NewGuid();

            return View(registration);
        }

        [HttpPost]
        public ActionResult StartRegistration(string conferenceCode, Registration contentModel)
        {
            var registration = this.UpdateRegistration(conferenceCode, contentModel);

            var command =
                new RegisterToConference
                {
                    RegistrationId = registration.Id,
                    ConferenceId = registration.ConferenceId,
                    Tickets = registration.Seats.Select(x => new RegisterToConference.Ticket { TicketTypeId = x.SeatId, Quantity = x.Quantity }).ToList()
                };

            this.commandBus.Send(command);

            var orderDTO = this.WaitUntilBooked(registration);

            if (orderDTO != null)
            {
                if (orderDTO.State == "Booked")
                {
                    return RedirectToAction("SpecifyPaymentDetails", new { conferenceCode = conferenceCode, registrationId = registration.Id });
                }
                else if (orderDTO.State == "Rejected")
                {
                    return View("ReservationRejected", registration);
                }
            }

            return View("ReservationUnknown", registration);
        }

        [HttpGet]
        public ActionResult SpecifyPaymentDetails(string conferenceCode, Guid registrationId)
        {
            var orderDTO = this.repositoryFactory().Find<OrderDTO>(registrationId);
            var registration = this.UpdateRegistration(conferenceCode, orderDTO);

            return View(registration);
        }

        [HttpPost]
        public ActionResult SpecifyPaymentDetails(string conferenceCode, Guid registrationId, PaymentDetails paymentDetails)
        {
            return RedirectToAction("Display", "Payment", new { conferenceCode = conferenceCode, registrationId = registrationId });
        }

        [HttpGet]
        public ActionResult TransactionCompleted(string conferenceCode, Guid registrationId, string transactionResult)
        {
            if (transactionResult == "accepted")
            {
                return RedirectToAction("ThankYou", new { conferenceCode = conferenceCode, registrationId = registrationId });
            }
            else
            {
                return RedirectToAction("Display", "Conference", new { conferenceCode = conferenceCode });
            }
        }

        [HttpGet]
        public ActionResult ThankYou(string conferenceCode, Guid registrationId)
        {
            return View();
        }

        private Registration CreateRegistration(string conferenceCode)
        {
            var conference =
                this.repositoryFactory().Query<ConferenceDTO>().FirstOrDefault(c => c.Code == conferenceCode);

            //// TODO check null case

            var registration =
                new Registration
                {
                    ConferenceId = conference.Id,
                    ConferenceCode = conference.Code,
                    ConferenceName = conference.Name,
                    Seats = conference.Seats.Select(s => new Seat { SeatId = s.Id, SeatDescription = s.Description, Price = s.Price }).ToList()
                };

            return registration;
        }

        private Registration UpdateRegistration(string conferenceCode, Registration contentModel)
        {
            var reservation = this.CreateRegistration(conferenceCode);
            reservation.Id = contentModel.Id;

            for (int i = 0; i < reservation.Seats.Count; i++)
            {
                var quantity = contentModel.Seats[i].Quantity;
                reservation.Seats[i].Quantity = quantity;
            }

            return reservation;
        }

        private Registration UpdateRegistration(string conferenceCode, OrderDTO orderDTO)
        {
            var reservation = this.CreateRegistration(conferenceCode);
            reservation.Id = orderDTO.OrderId;

            foreach (var line in orderDTO.Lines)
            {
                var seat = reservation.Seats.FirstOrDefault(s => s.SeatId == line.SeatTypeId);
                seat.Quantity = line.Quantity;
            }

            return reservation;
        }

        private OrderDTO WaitUntilBooked(Registration registration)
        {
            var deadline = DateTime.Now.AddSeconds(WaitTimeoutInSeconds);

            while (DateTime.Now < deadline)
            {
                var repo = this.repositoryFactory();
                using (repo as IDisposable)
                {
                    var orderDTO = repo.Find<OrderDTO>(registration.Id);

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
