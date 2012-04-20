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
    using System.Web.Mvc;

    public class ConferenceController : Controller
    {
        private ConferenceService service;

        private ConferenceService Service
        {
            get { return service ?? (service = new ConferenceService(MvcApplication.EventBus)); }
        }

        public ConferenceInfo Conference { get; private set; }

        // TODO: Locate and Create are the ONLY methods that don't require authentication/location info.

        /// <summary>
        /// We receive the slug value as a kind of cross-cutting value that 
        /// all methods need and use, so we catch and load the conference here, 
        /// so it's available for all. Each method doesn't need the slug parameter.
        /// </summary>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var slug = (string)this.ControllerContext.RequestContext.RouteData.Values["slug"];
            if (!string.IsNullOrEmpty(slug))
            {
                this.ViewBag.Slug = slug;
                this.Conference = this.Service.FindConference(slug);
                if (this.Conference != null)
                {
                    this.ViewBag.OwnerName = this.Conference.OwnerName;
                    this.ViewBag.WasEverPublished = this.Conference.WasEverPublished;
                }
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
            var conference = this.Service.FindConference(email, accessCode);
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

        public ActionResult Index()
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
                try
                {
                    this.Service.CreateConference(conference);
                }
                catch (DuplicateNameException e)
                {
                    ModelState.AddModelError("Slug", e.Message);
                    return View(conference);
                }

                return RedirectToAction("Index", new { slug = conference.Slug });
            }

            return View(conference);
        }

        public ActionResult Edit()
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
                this.Service.UpdateConference(conference);
                return RedirectToAction("Index", new { slug = conference.Slug });
            }

            return View(conference);
        }

        [HttpPost]
        public ActionResult Publish()
        {
            if (this.Conference == null)
            {
                return HttpNotFound();
            }

            this.Service.Publish(this.Conference.Id);

            return RedirectToAction("Index", new { slug = this.Conference.Slug });
        }

        [HttpPost]
        public ActionResult Unpublish()
        {
            if (this.Conference == null)
            {
                return HttpNotFound();
            }

            this.Service.Unpublish(this.Conference.Id);

            return RedirectToAction("Index", new { slug = this.Conference.Slug });
        }

        #endregion

        #region Seat Types

        public ViewResult Seats()
        {
            return View();
        }

        public ActionResult SeatGrid()
        {
            if (this.Conference == null)
            {
                return HttpNotFound();
            }

            return PartialView(this.Service.FindSeats(this.Conference.Id));
        }

        public ActionResult SeatRow(Guid id)
        {
            return PartialView("SeatGrid", new SeatInfo[] { this.Service.FindSeat(id) });
        }

        public ActionResult CreateSeat()
        {
            return PartialView("EditSeat");
        }

        [HttpPost]
        public ActionResult CreateSeat(SeatInfo seat)
        {
            if (this.Conference == null)
            {
                return HttpNotFound();
            }

            if (ModelState.IsValid)
            {
                this.Service.CreateSeat(this.Conference.Id, seat);

                return PartialView("SeatGrid", new SeatInfo[] { seat });
            }

            return PartialView("EditSeat", seat);
        }

        public ActionResult EditSeat(Guid id)
        {
            if (this.Conference == null)
            {
                return HttpNotFound();
            }

            return PartialView(this.Service.FindSeat(id));
        }

        [HttpPost]
        public ActionResult EditSeat(SeatInfo seat)
        {
            if (this.Conference == null)
            {
                return HttpNotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    this.Service.UpdateSeat(this.Conference.Id, seat);
                }
                catch (ObjectNotFoundException)
                {
                    return HttpNotFound();
                }

                return PartialView("SeatGrid", new SeatInfo[] { seat });
            }

            return PartialView(seat);
        }

        [HttpPost]
        public void DeleteSeat(Guid id)
        {
            this.Service.DeleteSeat(id);
        }

        #endregion
    }
}