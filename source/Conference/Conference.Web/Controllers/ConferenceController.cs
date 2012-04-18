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

namespace Conference.Web.Admin.Controllers
{
    using System;
    using System.Data;
    using System.Linq;
    using System.Web.Mvc;

    public class ConferenceController : Controller
    {
        private DomainContext db = new DomainContext();

        public ConferenceInfo Conference { get; private set; }

        // TODO: Locate and Create are the ONLY methods that don't require authentication/location info.

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var slug = (string)this.ControllerContext.RequestContext.RouteData.Values["slug"];
            if (!string.IsNullOrEmpty(slug))
            {
                this.ViewBag.Slug = slug;
                this.Conference = db.Conferences.FirstOrDefault(x => x.Slug == slug);
                if (this.Conference != null)
                    this.ViewBag.OwnerName = this.Conference.OwnerName;
            }

            base.OnActionExecuting(filterContext);
        }

        #region Conference Details

        public ActionResult Locate()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Locate(string email, string accessCode)
        {
            var conference = db.Conferences.FirstOrDefault(c => c.OwnerEmail == email && c.AccessCode == accessCode);
            if (conference == null)
            {
                ViewBag.NotFound = true;
                // Preserve input so the user doesn't have to type email again.
                ViewBag.Email = email;

                return Locate();
            }

            // TODO: not very secure ;).
            return RedirectToAction("Index", new { slug = conference.Slug });
        }

        public ActionResult Index(string slug)
        {
            if (this.Conference == null)
            {
                return HttpNotFound();
            }
            return View(this.Conference);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(ConferenceInfo conference)
        {
            if (ModelState.IsValid)
            {
                var existingSlug = db.Conferences
                    .Where(c => c.Slug == conference.Slug)
                    .Select(c => c.Slug)
                    .Any();

                if (existingSlug)
                {
                    ModelState.AddModelError("Slug", "The chosen conference slug is already taken.");
                    return View(conference);
                }

                conference.Id = Guid.NewGuid();
                db.Conferences.Add(conference);
                db.SaveChanges();
                return RedirectToAction("Index", new { slug = conference.Slug });
            }

            return View(conference);
        }

        public ActionResult Edit(string slug)
        {
            if (this.Conference == null)
            {
                return HttpNotFound();
            }
            return View(this.Conference);
        }

        [HttpPost]
        public ActionResult Edit(ConferenceInfo conference)
        {
            if (this.Conference == null)
            {
                return HttpNotFound();
            }
            if (ModelState.IsValid)
            {
                // Update the already loaded conference with the 
                // new incoming values.
                db.Entry(this.Conference).CurrentValues.SetValues(conference);
                db.SaveChanges();
                return RedirectToAction("Index", new { slug = conference.Slug });
            }

            return View(conference);
        }

        public ActionResult Delete(string slug)
        {
            if (this.Conference == null)
            {
                return HttpNotFound();
            }
            return View(this.Conference);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(string slug)
        {
            if (this.Conference == null)
            {
                return HttpNotFound();
            }

            db.Conferences.Remove(this.Conference);
            db.SaveChanges();
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public ActionResult Publish(string slug)
        {
            return SetPublished(slug, true);
        }

        [HttpPost]
        public ActionResult Unpublish(string slug)
        {
            return SetPublished(slug, false);
        }

        private ActionResult SetPublished(string slug, bool isPublished)
        {
            if (this.Conference == null)
            {
                return HttpNotFound();
            }

            this.Conference.IsPublished = isPublished;
            db.SaveChanges();

            // TODO: not very secure ;).
            return RedirectToAction("Index", new { slug = slug });
        }

        #endregion

        #region Seat Types

        public ViewResult Seats(string slug)
        {
            ViewBag.Slug = slug;
            return View();
        }

        public ActionResult SeatGrid(string slug)
        {
            if (this.Conference == null)
            {
                return HttpNotFound();
            }

            return PartialView(this.Conference.Seats);
        }

        public ActionResult SeatRow(string slug, Guid id)
        {
            var seatinfo = db.Seats.Find(id);
            return PartialView("SeatGrid", new SeatInfo[] { seatinfo });
        }

        public ActionResult CreateSeat(string slug)
        {
            ViewBag.Slug = slug;

            return PartialView("EditSeat");
        }

        [HttpPost]
        public ActionResult CreateSeat(string slug, SeatInfo seat)
        {
            if (this.Conference == null)
            {
                return HttpNotFound();
            }

            if (ModelState.IsValid)
            {
                seat.Id = Guid.NewGuid();
                this.Conference.Seats.Add(seat);
                db.SaveChanges();
                return PartialView("SeatGrid", new SeatInfo[] { seat });
            }

            return PartialView("EditSeat", seat);
        }

        public ActionResult EditSeat(string slug, Guid id)
        {
            var seat = db.Seats.Find(id);
            return PartialView(seat);
        }

        [HttpPost]
        public ActionResult EditSeat(string slug, SeatInfo seat)
        {
            if (ModelState.IsValid)
            {
                db.Entry(seat).State = EntityState.Modified;
                db.SaveChanges();
                return PartialView("SeatGrid", new SeatInfo[] { seat });
            }

            return PartialView(seat);
        }

        [HttpPost]
        public void DeleteSeat(string slug, Guid id)
        {
            // TODO: Do we have Delete at all?
            var seat = db.Seats.Find(id);
            db.Seats.Remove(seat);
            db.SaveChanges();
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}