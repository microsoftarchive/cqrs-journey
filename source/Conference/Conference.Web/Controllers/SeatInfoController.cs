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
