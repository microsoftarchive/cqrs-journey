// ==============================================================================================================
// Microsoft patterns & practices
// CQRS Journey project
// ==============================================================================================================
// ©2012 Microsoft. All rights reserved. Certain content used with permission from contributors
// http://go.microsoft.com/fwlink/p/?LinkID=258575
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
// with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and limitations under the License.
// ==============================================================================================================

namespace Conference.Web.Public.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Mvc;
    using AutoMapper;
    using Infrastructure.Messaging;
    using Registration.Commands;
    using Registration.ReadModel;

    public class OrderController : ConferenceTenantController
    {
        private readonly IOrderDao orderDao;
        private readonly ICommandBus bus;

        static OrderController()
        {
            Mapper.CreateMap<OrderSeat, AssignSeat>();
        }

        public OrderController(IConferenceDao conferenceDao, IOrderDao orderDao, ICommandBus bus)
            : base(conferenceDao)
        {
            this.orderDao = orderDao;
            this.bus = bus;
        }

        [HttpGet]
        public ActionResult Display(Guid orderId)
        {
            var order = orderDao.FindPricedOrder(orderId);
            if (order == null)
                return RedirectToAction("Find", new { conferenceCode = this.ConferenceCode });

            return View(order);
        }

        [HttpGet]
        [OutputCache(Duration = 0, NoStore = true)]
        public RedirectToRouteResult AssignSeatsForOrder(Guid orderId)
        {
            var order = orderDao.FindPricedOrder(orderId);
            if (order == null || !order.AssignmentsId.HasValue)
            {
                return RedirectToAction("Display", new { orderId });
            }

            return RedirectToAction("AssignSeats", new { assignmentsId = order.AssignmentsId });
        }

        [HttpGet]
        [OutputCache(Duration = 0, NoStore = true)]
        public ActionResult AssignSeats(Guid assignmentsId)
        {
            var assignments = this.orderDao.FindOrderSeats(assignmentsId);
            if (assignments == null)
                return RedirectToAction("Find", new { conferenceCode = this.ConferenceCode });

            return View(assignments);
        }

        [HttpPost]
        public ActionResult AssignSeats(Guid assignmentsId, List<OrderSeat> seats)
        {
            var saved = this.orderDao.FindOrderSeats(assignmentsId);
            if (saved == null)
                return RedirectToAction("Find", new { conferenceCode = this.ConferenceCode });

            var pairs = seats
                // If a seat is null, it's because it's an invalid null entry 
                // in the list of seats, so we ignore it.
                .Where(seat => seat != null)
                .Select(seat => new { Saved = saved.Seats.FirstOrDefault(x => x.Position == seat.Position), New = seat })
                // Ignore posted seats that we don't have saved already: pair.Saved == null
                // This may be because the client sent mangled or incorrect data so we couldn't 
                // find a matching saved seat.
                .Where(pair => pair.Saved != null)
                // Only process those that have an email (i.e. they are or were assigned)
                .Where(pair => pair.Saved.Attendee.Email != null || pair.New.Attendee.Email != null)
                .ToList();

            // NOTE: in the read model, we care about the OrderId, 
            // but the write side uses a different aggregate root id for the seat 
            // assignments, so we pass that on when we issue commands.

            var unassigned = pairs
                .Where(x => !string.IsNullOrWhiteSpace(x.Saved.Attendee.Email) && string.IsNullOrWhiteSpace(x.New.Attendee.Email))
                .Select(x => (ICommand)new UnassignSeat { SeatAssignmentsId = saved.AssignmentsId, Position = x.Saved.Position });

            var changed = pairs
                .Where(x => x.Saved.Attendee != x.New.Attendee && x.New.Attendee.Email != null)
                .Select(x => (ICommand)Mapper.Map(x.New, new AssignSeat { SeatAssignmentsId = saved.AssignmentsId }));

            var commands = unassigned.Union(changed).ToList();
            if (commands.Count > 0)
            {
                this.bus.Send(commands);
            }

            return RedirectToAction("Display", new { orderId = saved.OrderId });
        }

        [HttpGet]
        public ActionResult Find()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Find(string email, string accessCode)
        {
            var orderId = orderDao.LocateOrder(email, accessCode);

            if (!orderId.HasValue)
            {
                // TODO: 404?
                return RedirectToAction("Find", new { conferenceCode = this.ConferenceCode });
            }

            return RedirectToAction("Display", new { conferenceCode = this.ConferenceCode, orderId = orderId.Value });
        }
    }
}