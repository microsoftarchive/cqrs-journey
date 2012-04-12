using System;
using System.Data;
using System.Web.Mvc;

namespace Conference.Web.Controllers
{
    public class ConferenceController : Controller
    {
        private DomainContext db = new DomainContext();

        //
        // GET: /Conference/Details/5

        public ActionResult Details(Guid id)
        {
            ConferenceInfo conferenceinfo = db.Conferences.Find(id);
            if (conferenceinfo == null)
            {
                return HttpNotFound();
            }
            return View(conferenceinfo);
        }

        //
        // GET: /Conference/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /Conference/Create

        [HttpPost]
        public ActionResult Create(ConferenceInfo conferenceinfo)
        {
            if (ModelState.IsValid)
            {
                conferenceinfo.Id = Guid.NewGuid();
                db.Conferences.Add(conferenceinfo);
                db.SaveChanges();
                return RedirectToAction("Details", new { id = conferenceinfo.Id });
            }

            return View(conferenceinfo);
        }

        //
        // GET: /Conference/Edit/5

        public ActionResult Edit(Guid id)
        {
            ConferenceInfo conferenceinfo = db.Conferences.Find(id);
            if (conferenceinfo == null)
            {
                return HttpNotFound();
            }
            return View(conferenceinfo);
        }

        //
        // POST: /Conference/Edit/5

        [HttpPost]
        public ActionResult Edit(ConferenceInfo conferenceinfo)
        {
            if (ModelState.IsValid)
            {
                db.Entry(conferenceinfo).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(conferenceinfo);
        }

        //
        // GET: /Conference/Delete/5

        public ActionResult Delete(Guid id)
        {
            ConferenceInfo conferenceinfo = db.Conferences.Find(id);
            if (conferenceinfo == null)
            {
                return HttpNotFound();
            }
            return View(conferenceinfo);
        }

        //
        // POST: /Conference/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(Guid id)
        {
            ConferenceInfo conferenceinfo = db.Conferences.Find(id);
            db.Conferences.Remove(conferenceinfo);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}