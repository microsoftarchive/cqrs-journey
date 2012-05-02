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
            Mapper.CreateMap<SeatAssignmentDTO, AssignSeat>()
                .ForMember(x => x.AssignmentId, x=> x.MapFrom(i => i.Id));
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
                .Where(pair => pair.Saved.Email != null || pair.New.Email != null)
                .ToList();

            var unassigned = pairs
                .Where(x => !string.IsNullOrWhiteSpace(x.Saved.Email) && string.IsNullOrWhiteSpace(x.New.Email))
                .Select(x => (ICommand)new UnassignSeat { SeatAssignmentsId = orderId, AssignmentId = x.Saved.Id });

            var changed = pairs
                .Where(x => !string.Equals(x.Saved.Email, x.New.Email, StringComparison.InvariantCultureIgnoreCase)
                            || !string.Equals(x.Saved.FirstName, x.New.FirstName, StringComparison.CurrentCulture)
                            || !string.Equals(x.Saved.LastName, x.New.LastName, StringComparison.CurrentCulture))
                .Select(x => (ICommand)Mapper.Map(x.New, new AssignSeat { SeatAssignmentsId = orderId }));

            var commands = unassigned.Union(changed).ToList();
            if (commands.Count > 0)
            {
                this.bus.Send(commands);
            }

            // TODO: Will have a display page, so we can redirect to a different one. Right now, because this is eventually consistent,
            // the user will not see his changes.
            Thread.Sleep(1000);
            return RedirectToAction("Display");
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