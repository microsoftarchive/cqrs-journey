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
    using System.Web.Mvc;
    using Registration.ReadModel;
    using System.Collections.Generic;
    using Registration.Commands;
    using Registration;
    using AutoMapper;
    using Infrastructure.Messaging;

    public class OrderController : Controller
    {
        private readonly IOrderDao orderDao;
        private ISeatAssignmentsDao assignmentsDao;
        private ICommandBus bus;

        static OrderController()
        {
            Mapper.CreateMap<SeatAssignmentDTO, SeatAssignment>();
        }

        public OrderController(IOrderDao orderDao, ISeatAssignmentsDao assignmentsDao, ICommandBus bus)
        {
            this.orderDao = orderDao;
            this.assignmentsDao = assignmentsDao;
            this.bus = bus;
        }

        [HttpGet]
        public ActionResult Display(string conferenceCode, Guid orderId)
        {
            var order = orderDao.GetOrderDetails(orderId);
            if (order == null)
                return RedirectToAction("Find", new { conferenceCode = conferenceCode });

            var assignments = assignmentsDao.Find(orderId);

            return View(assignments);
        }

        [HttpPost]
        public ActionResult Display(string conferenceCode, Guid orderId, List<SeatAssignmentDTO> seats)
        {
            var saved = assignmentsDao.Find(orderId);
            if (saved == null)
                return RedirectToAction("Find", new { conferenceCode = conferenceCode });

            var pairs = seats
                .Select(dto => new { Saved = saved.Seats.FirstOrDefault(x => x.Id == dto.Id), New = dto })
                // Ignore posted seats that we don't have saved already.
                .Where(pair => pair.Saved != null && pair.New != null)
                // Only process those where they don't remain unassigned.
                .Where(pair => !(pair.Saved.Email == null && pair.New.Email == null))
                .ToList();

            var removed = pairs
                .Where(x => x.Saved.Email != null && x.New.Email != x.Saved.Email)
                .Select(x => Mapper.Map<SeatAssignment>(x.Saved))
                .ToList();

            var added = pairs
                .Where(x => x.Saved.Email != x.New.Email && x.New.Email != null)
                .Select(x => Mapper.Map<SeatAssignment>(x.New))
                .ToList();

            var updated = pairs
                .Where(x => x.New.Email == x.Saved.Email &&
                    (x.New.FirstName != x.Saved.FirstName || x.New.LastName != x.Saved.LastName))
                .Select(x => Mapper.Map<SeatAssignment>(x.New))
                .ToList();

            // Removed goes first so the state can be cleared.
            // NOTE: what about out of order processing? It's significant here...
            if (removed.Any())
                this.bus.Send(new ReleaseAssignedSeats { OrderId = orderId, Seats = removed });
            if (added.Any())
                this.bus.Send(new AssignSeats { OrderId = orderId, Seats = added });
            if (updated.Any())
                this.bus.Send(new UpdateAssignedSeats { OrderId = orderId, Seats = updated });

            return View(new SeatAssignmentsDTO(orderId, seats));
        }

        [HttpGet]
        public ActionResult Find(string conferenceCode)
        {
            return View();
        }

        [HttpPost]
        public ActionResult Find(string conferenceCode, string email, string accessCode)
        {
            var orderId = orderDao.LocateOrder(email, accessCode);

            if (!orderId.HasValue)
            {
                // TODO: 404?
                return RedirectToAction("Find", new { conferenceCode = conferenceCode });
            }

            return RedirectToAction("Display", new { conferenceCode = conferenceCode, orderId = orderId.Value });
        }
    }
}