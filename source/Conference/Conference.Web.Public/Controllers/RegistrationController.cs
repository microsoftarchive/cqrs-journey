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
    using System.Web.Mvc;
    using Common;
    using Conference.Web.Public.Models;
    using Registration.Commands;

    public class RegistrationController : Controller
    {
        private ICommandBus commandBus;
        private object registrationReadService;

        public RegistrationController()
            : this(GetCommandBus(), GetRegistrationReadService())
        {
        }

        public RegistrationController(ICommandBus commandBus, object registrationReadService)
        {
            this.commandBus = commandBus;
            this.registrationReadService = registrationReadService;
        }

        private static ICommandBus GetCommandBus()
        {
            return null;
        }

        private static object GetRegistrationReadService()
        {
            return null;
        }

        [HttpGet]
        public ActionResult ChooseSeats(string conferenceName)
        {
            var registration = CreateRegistration(conferenceName);
            registration.Id = Guid.NewGuid();

            return View(registration);
        }

        [HttpPost]
        public ActionResult ChoosePayment(string conferenceName, Registration contentModel)
        {
            var registration = UpdateRegistration(conferenceName, contentModel);

            // TODO send create registration command
            var command =
                new RegisterToConference
                {
                    Id = registration.Id,
                    ConferenceId = registration.ConferenceId,
                    NumberOfSeats = registration.Seats[0].Quantity
                };



            // Wait until updated

            return View(registration);
        }

        private Registration UpdateRegistration(string conferenceName, Registration contentModel)
        {
            var reservation = this.CreateRegistration(conferenceName);

            for (int i = 0; i < reservation.Seats.Count; i++)
            {
                var quantity = contentModel.Seats[i].Quantity;
                reservation.Seats[i].Quantity = quantity;
            }
            return reservation;
        }

        [HttpPost]
        public ActionResult ConfirmRegistration(string conferenceName)
        {
            return View();
        }

        private Registration CreateRegistration(string conferenceName)
        {
            var registration =
                new Registration
                {
                    ConferenceName = conferenceName,
                    Seats = { new Seat { SeatId = "testSeat", SeatDescription = "Test seat", Price = 100f } }
                };

            return registration;
        }
    }
}
