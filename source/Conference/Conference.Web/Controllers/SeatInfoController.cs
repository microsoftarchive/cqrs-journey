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

using System;
using System.Data;
using System.Web.Mvc;

namespace Conference.Web.Controllers
{
    public class SeatInfoController : Controller
    {
        private DomainContext db = new DomainContext();

        public ViewResult Index(Guid conferenceId)
        {
            ViewBag.ConferenceId = conferenceId;
            return View();
        }

        public ActionResult GridData(Guid conferenceId)
        {
            var conference = db.Conferences.Find(conferenceId);
            if (conference == null)
            {
                return HttpNotFound();
            }

            return PartialView(conference.SeatInfos);
        }

        public ActionResult RowData(Guid conferenceId, Guid id)
        {
            var seatinfo = db.Seats.Find(id);
            return PartialView("GridData", new SeatInfo[] { seatinfo });
        }

        public ActionResult Create(Guid conferenceId)
        {
            ViewBag.ConferenceId = conferenceId;

            return PartialView("Edit");
        }

        [HttpPost]
        public ActionResult Create(Guid conferenceId, SeatInfo seatinfo)
        {
            if (ModelState.IsValid)
            {
                seatinfo.Id = Guid.NewGuid();
                // db.Seats.Add(seatinfo);
                var conference = db.Conferences.Find(conferenceId);
                conference.SeatInfos.Add(seatinfo);
                db.SaveChanges();
                return PartialView("GridData", new SeatInfo[] { seatinfo });
            }

            return PartialView("Edit", seatinfo);
        }

        public ActionResult Edit(Guid conferenceId, Guid id)
        {
            var seatinfo = db.Seats.Find(id);
            return PartialView(seatinfo);
        }

        [HttpPost]
        public ActionResult Edit(Guid conferenceId, SeatInfo seatinfo)
        {
            if (ModelState.IsValid)
            {
                db.Entry(seatinfo).State = EntityState.Modified;
                db.SaveChanges();
                return PartialView("GridData", new SeatInfo[] { seatinfo });
            }

            return PartialView(seatinfo);
        }

        [HttpPost]
        public void Delete(Guid conferenceId, Guid id)
        {
            SeatInfo seatinfo = db.Seats.Find(id);
            db.Seats.Remove(seatinfo);
            db.SaveChanges();
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}
