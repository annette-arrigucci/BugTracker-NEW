using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using BugTracker.Models;
using Microsoft.AspNet.Identity;

namespace BugTracker.Controllers
{
    public class TicketCommentsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: TicketComments
        [Authorize(Roles ="Admin")]
        public ActionResult Index()
        {
            return View(db.TicketComments.ToList());
        }

        // GET: TicketComments/Details/5
        [Authorize(Roles = "Admin")]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TicketComment ticketComment = db.TicketComments.Find(id);
            if (ticketComment == null)
            {
                return HttpNotFound();
            }
            return View(ticketComment);
        }

        // GET: TicketComments/Create
        [Authorize]
        public ActionResult Create(int ticketId)
        {
            //Make sure the user is authorized to comment on this ticket
            var helper = new ProjectUserHelper();
            var userId = User.Identity.GetUserId();
            var ticket = db.Tickets.Find(ticketId);

            //if user is not an admin, who is able to comment on all tickets, check if they are a project manager, developer or submitter 
            //and allowed to post a comment. If not, redirect them to a "bad request" page
            if (!User.IsInRole("Admin"))
            {
                //for PM, verify it is in one of their assigned projects
                if (User.IsInRole("Project Manager"))
                {
                    if (!helper.IsUserInProject(userId, ticket.ProjectId))
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                    }
                }
                //for developer - verify that they have been assigned this ticket
                else if (User.IsInRole("Developer"))
                {
                    //if the ticket is unassigned, return a bad request
                    if (string.IsNullOrEmpty(ticket.AssignedToUserId))
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                    }
                    else if (!ticket.AssignedToUserId.Equals(userId))
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                    }
                }
                //for submitter - verify that they created this ticket
                else if (User.IsInRole("Submitter"))
                {
                    if (!ticket.OwnerUserId.Equals(userId))
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                    }
                }
                //if the user is not a PM, developer or submitter, then they are unassigned and not authorized to comment
                else
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
            }

            var model = new TicketComment();
            model.TicketId = ticketId;
            //var query = from p in db.Tickets
            //            where p.Id == ticketId
            //            select p.Title;
            ViewBag.ticketTitle = ticket.Title;
                //query.FirstOrDefault();
            return View(model);
        }

        // POST: TicketComments/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Comment,TicketId")] TicketComment ticketComment)
        {
            if (ModelState.IsValid)
            {
                ticketComment.Created = DateTimeOffset.Now;
                ticketComment.UserId = User.Identity.GetUserId();
                db.TicketComments.Add(ticketComment);
                db.SaveChanges();
                return RedirectToAction("Details","Tickets", new { id = ticketComment.TicketId });
            }
            return View(ticketComment);
        }

        // GET: TicketComments/Edit/5
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TicketComment ticketComment = db.TicketComments.Find(id);
            if (ticketComment == null)
            {
                return HttpNotFound();
            }
            return View(ticketComment);
        }

        // POST: TicketComments/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Comment,Created,TicketId,UserId")] TicketComment ticketComment)
        {
            if (ModelState.IsValid)
            {
                db.Entry(ticketComment).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(ticketComment);
        }

        // GET: TicketComments/Delete/5
        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TicketComment ticketComment = db.TicketComments.Find(id);
            if (ticketComment == null)
            {
                return HttpNotFound();
            }
            return View(ticketComment);
        }

        // POST: TicketComments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            TicketComment ticketComment = db.TicketComments.Find(id);
            db.TicketComments.Remove(ticketComment);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
