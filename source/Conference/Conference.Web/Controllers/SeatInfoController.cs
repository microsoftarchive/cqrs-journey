using System;
using System.Data;
using System.Data.Entity.Infrastructure;
using System.Data.Objects;
using System.Linq;
using System.Web.Mvc;

namespace Conference.Web.Controllers
{
    public class SeatInfoController : Controller
    {
        private DomainContext db = new DomainContext();

        //
        // GET: /SeatInfo/

        public ViewResult Index(string orderBy = "Id", bool desc = false)
        {
            ViewBag.Count = db.Seats.Count();
            ViewBag.OrderBy = orderBy;
            ViewBag.Desc = desc;

            return View();
        }

        public ActionResult GridData(string orderBy = "Id", bool desc = false)
        {
            Response.AppendHeader("X-Total-Row-Count", db.Seats.Count().ToString());
            ObjectQuery<SeatInfo> seats = (db as IObjectContextAdapter).ObjectContext.CreateObjectSet<SeatInfo>();
            seats = seats.OrderBy("it." + orderBy + (desc ? " desc" : ""));

            return PartialView(seats);
        }

        //
        // GET: /Default5/RowData/5

        public ActionResult RowData(Guid id)
        {
            SeatInfo seatinfo = db.Seats.Find(id);
            return PartialView("GridData", new SeatInfo[] { seatinfo });
        }

        //
        // GET: /SeatInfo/Create

        public ActionResult Create()
        {
            return PartialView("Edit");
        }

        //
        // POST: /SeatInfo/Create

        [HttpPost]
        public ActionResult Create(SeatInfo seatinfo)
        {
            if (ModelState.IsValid)
            {
                seatinfo.Id = Guid.NewGuid();
                db.Seats.Add(seatinfo);
                db.SaveChanges();
                return PartialView("GridData", new SeatInfo[] { seatinfo });
            }

            return PartialView("Edit", seatinfo);
        }

        //
        // GET: /SeatInfo/Edit/5

        public ActionResult Edit(Guid id)
        {
            SeatInfo seatinfo = db.Seats.Find(id);
            return PartialView(seatinfo);
        }

        //
        // POST: /SeatInfo/Edit/5

        [HttpPost]
        public ActionResult Edit(SeatInfo seatinfo)
        {
            if (ModelState.IsValid)
            {
                db.Entry(seatinfo).State = EntityState.Modified;
                db.SaveChanges();
                return PartialView("GridData", new SeatInfo[] { seatinfo });
            }

            return PartialView(seatinfo);
        }

        //
        // POST: /SeatInfo/Delete/5

        [HttpPost]
        public void Delete(Guid id)
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
