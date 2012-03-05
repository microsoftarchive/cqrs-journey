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
    using System.Web.Mvc;
    using Conference.Web.Public.Models;

    public class RegistrationController : Controller
    {
        [HttpGet]
        public ActionResult ChooseSeats(string conferenceId)
        {
            var reservation = CreateRegistration(conferenceId);

            return View(reservation);
        }

        [HttpPost]
        public ActionResult ChooseSeats(string conferenceId, Registration contentModel)
        {
            return View(contentModel);
        }

        [HttpPost]
        public ActionResult ChoosePayment(string conferenceId, Registration contentModel)
        {
            var reservation = this.CreateRegistration(conferenceId);
            bool hasSeats = false;

            for (int i = 0; i < reservation.Seats.Count; i++)
            {
                var quantity = contentModel.Seats[i].Quantity;
                reservation.Seats[i].Quantity = quantity;
                hasSeats |= quantity > 0;
            }

            if (!hasSeats)
            {
                return View("ChooseSeats");
            }

            return View(reservation);
        }

        [HttpPost]
        public ActionResult ConfirmRegistration(string conferenceId)
        {
            return View();
        }

        private Registration CreateRegistration(string conferenceId)
        {
            var reservation =
                new Registration
                {
                    ConferenceId = conferenceId,
                    Seats = { new Seat { SeatId = "testSeat", SeatDescription = "Test seat", Price = 100f } }
                };

            return reservation;
        }
    }
}
