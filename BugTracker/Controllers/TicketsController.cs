﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using BugTracker.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Threading.Tasks;
//using PagedList;
//using PagedList.Mvc;

namespace BugTracker.Controllers
{
    public class TicketsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        enum Priorities { High = 1, Medium, Low, Critical };
        enum Statuses { Waitingforsupport = 1, Waitingforcustomer, Resolved, Onhold, New };
        enum Types {  Errorreport = 1, Featurerequest, Servicerequest, Other };

        // GET: Tickets
        [Authorize]
        public ActionResult Index()
        {
            var id = User.Identity.GetUserId();
            var ticketDetailsList = new List<TicketDetailsViewModel>();

            // if admin, view all tickets
            if (User.IsInRole("Admin"))
            {
                var tickets = db.Tickets;
                ticketDetailsList = transformTickets(db.Tickets.ToList());
                ticketDetailsList = ticketDetailsList.OrderByDescending(x => x.Created).ToList();
                return View(ticketDetailsList);
            }
            //otherwise, go through each role a user can be in and add the tickets that can be viewed in each
            //The Union method is used to eliminate duplicate entries in which user both owns the ticket is 
            //assigned the ticket or is PM for the ticket - the Equals method is overriden in the TicketDetailsViewModel
            else if (User.IsInRole("Project Manager"))
            {
                var query = db.Projects.Where(x => x.ProjectUsers.Any(y => y.UserId == id));
                var projects = query.ToList();
                var ticketList = new List<Ticket>();
                if (projects.Count > 0)
                { 
                    foreach (Project p in projects)
                    {
                        var projTickets = p.Tickets;
                        ticketList.AddRange(projTickets);
                    }
                }             
                var pmTicketDetailsList = transformTickets(ticketList);
                ticketDetailsList = ticketDetailsList.Union(pmTicketDetailsList).ToList();
            }
            if (User.IsInRole("Developer"))
            {
                var tickets = db.Tickets.Where(x => x.AssignedToUserId == id);
                var devDetailsList = transformTickets(tickets.ToList());
                ticketDetailsList = ticketDetailsList.Union(devDetailsList).ToList();
            }
            if (User.IsInRole("Submitter"))
            {
                var tickets = db.Tickets.Where(x => x.OwnerUserId == id);
                var subDetailsList = transformTickets(tickets.ToList());
                ticketDetailsList = ticketDetailsList.Union(subDetailsList).ToList();
            }
            //the order gets overriden when datatables.net script is applied
            ticketDetailsList = ticketDetailsList.OrderByDescending(x => x.Created).ToList();
            return View(ticketDetailsList);
        }

        //this calls a ticket directory view where all fields of tickets can be viewed on one page
        //same code as Index view but displays differently in view
        [Authorize]
        public ActionResult All()
        {
            var id = User.Identity.GetUserId();
            var ticketDetailsList = new List<TicketDetailsViewModel>();

            // if admin, view all tickets
            if (User.IsInRole("Admin"))
            {
                var tickets = db.Tickets;
                ticketDetailsList = transformTickets(db.Tickets.ToList());
                ticketDetailsList = ticketDetailsList.OrderByDescending(x => x.Created).ToList();
                return View(ticketDetailsList);
            }
            //otherwise, go through each role a user can be in and add the tickets that can be viewed in each
            //duplicates eliminated with Union method
            else if (User.IsInRole("Project Manager"))
            {
                var query = db.Projects.Where(x => x.ProjectUsers.Any(y => y.UserId == id));
                var projects = query.ToList();
                var ticketList = new List<Ticket>();
                if (projects.Count > 0)
                {
                    foreach (Project p in projects)
                    {
                        var projTickets = p.Tickets;
                        ticketList.AddRange(projTickets);
                    }
                }
                var pmTicketDetailsList = transformTickets(ticketList);
                ticketDetailsList = ticketDetailsList.Union(pmTicketDetailsList).ToList();
            }
            if (User.IsInRole("Developer"))
            {
                var tickets = db.Tickets.Where(x => x.AssignedToUserId == id);
                var devDetailsList = transformTickets(tickets.ToList());
                ticketDetailsList = ticketDetailsList.Union(devDetailsList).ToList();
            }
            if (User.IsInRole("Submitter"))
            {
                var tickets = db.Tickets.Where(x => x.OwnerUserId == id);
                var subDetailsList = transformTickets(tickets.ToList());
                ticketDetailsList = ticketDetailsList.Union(subDetailsList).ToList();
            }
            ticketDetailsList = ticketDetailsList.OrderByDescending(x => x.Created).ToList();
            return View(ticketDetailsList);
        }

        // GET: Tickets/Details/5
        [Authorize]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ticket ticket = db.Tickets.Find(id);
            if (ticket == null)
            {
                return HttpNotFound();
            }

            var userId = User.Identity.GetUserId();
            var helper = new ProjectUserHelper();

            //if user is not an admin, who is able to view all tickets, check if they are a project manager, developer or submitter 
            //and allowed to view the ticket. If not, redirect them to a "bad request" page
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
                //if the user is not a PM, developer or submitter, then they are unassigned and not authorized to view any tickets
                else
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
            }
            var model = new TicketDetailsViewModel(ticket);

            //pass in the TicketComments with the ticket details
            ViewBag.Comments = ticket.TicketComments;
            ViewBag.Attachments = ticket.TicketAttachments;
            return View(model);
        }


        // GET: Tickets/AssignUser/5
        [Authorize(Roles ="Project Manager")]
        public ActionResult AssignUser(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            //check that project manager is assigning a ticket that is in one of their projects
            var helper = new ProjectUserHelper();
            var userId = User.Identity.GetUserId();
            Ticket ticket = db.Tickets.Find(id);

            if (!helper.IsUserInProject(userId, ticket.ProjectId))
            {
               return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var model = new TicketAssignViewModel();
            
            model.TicketDetails = new TicketDetailsViewModel(ticket);
            
            if(!string.IsNullOrEmpty(ticket.AssignedToUserId))
            {
                model.SelectedUser = ticket.AssignedToUserId;
            }
            //find only the developers assigned to a project - only developers can be assigned a ticket        
            var developerIDList = helper.DevelopersInProject(ticket.ProjectId);
            var developerInfoList = helper.getUserInfo(developerIDList);
            if (!string.IsNullOrEmpty(model.SelectedUser))
            {
                model.ProjUsersList = new SelectList(developerInfoList, "UserId", "UserName", model.SelectedUser);
            }
            else
            {
                model.ProjUsersList = new SelectList(developerInfoList, "UserId", "UserName");
            }
            if (ticket == null)
            {
                return HttpNotFound();
            }
            return View(model);
        }

        // POST: Tickets/AssignUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AssignUser(int tId, string SelectedUser)
        {
            if (ModelState.IsValid)
            {
                var ticket = db.Tickets.Find(tId);
                //if there is already a user assigned, check if it is the same user
                //if it is, we won't create another ticket notification
                if (ticket.AssignedToUserId != null && ticket.AssignedToUserId.Equals("Selected User"))
                {
                        return RedirectToAction("Index");
                }
                //double check that user is a developer; if not redirect back to form
                var helper = new UserRolesHelper();
                if (!helper.IsUserInRole(SelectedUser, "Developer"))
                {
                    return RedirectToAction("AssignUser", new { id = tId });
                }
                //otherwise, 
                //- update the ticket
                //- create an entry in ticket notification table
                //- send an email to the developer who has been assigned the ticket
                //- if status is New, change to "Waiting for Support"
                else
                { 
                    ticket.AssignedToUserId = SelectedUser;
                    var tn = new TicketNotification { TicketId = tId, UserId = SelectedUser };
                    db.TicketNotifications.Add(tn);
              
                    UpdateTicketStatusIfNew(tId);

                    db.Entry(ticket).State = EntityState.Modified;
                    db.SaveChanges();

                    var ticketTitle = ticket.Title;
                    SendNotificationEmail(SelectedUser, ticketTitle);

                    return RedirectToAction("ConfirmAssignment");
                } 
            }
            else
            {
                return RedirectToAction("AssignUser", new { id = tId });
            }
        }
        
        //Email a user that he/she has been assigned a ticket
        public async Task SendNotificationEmail(string userId, string ticketTitle)
        {
            var callbackUrl = "http://aarrigucci-bugtracker.azurewebsites.net/tickets";           
            ApplicationUser user = db.Users.Find(userId);
            var es = new EmailService();
            es.SendAsync(new IdentityMessage
            {
                Destination = user.Email,
                Subject = "New Ticket - " + ticketTitle,
                Body = "You have been assigned a new ticket. Click <a href=\"" + callbackUrl + "\">here</a> to view ticket details."
            });
        }

        public void UpdateTicketStatusIfNew(int tId)
        {
            var ticket = db.Tickets.Find(tId);
            var status = db.TicketStatuses.Find(ticket.TicketStatusId);
            if(status.Name == "New")
            {
                var newStatus = db.TicketStatuses.FirstOrDefault(x => x.Name == "Waiting for support");
                ticket.TicketStatusId = newStatus.Id;
            }
        }

        public ActionResult ConfirmAssignment()
        {
            return View();
        }

        // GET: Tickets/Create
        [Authorize(Roles = "Submitter")]
        public ActionResult Create()
        {
            var ticketView = new TicketCreateViewModel();
            ticketView.Projects = new SelectList(db.Projects, "Id", "Name");
            ticketView.TicketTypes = new SelectList(db.TicketTypes, "Id", "Name");
            ticketView.TicketPriorities = new SelectList(db.TicketPriorities, "Id", "Name");
            return View(ticketView);
        }

        // POST: Tickets/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Title,Description,SelectedProject,SelectedType,SelectedPriority")] TicketCreateViewModel tvm)
        {
            if (ModelState.IsValid)
            {
                var ticket = new Ticket();
                ticket.Title = tvm.Title;
                ticket.Description = tvm.Description;
                ticket.Created = DateTimeOffset.Now;
                ticket.ProjectId = tvm.SelectedProject;
                ticket.TicketTypeId = tvm.SelectedType;
                ticket.TicketPriorityId = tvm.SelectedPriority;
                var query = from p in db.TicketStatuses
                            where p.Name == "New"
                            select p.Id;
                ticket.TicketStatusId = query.First();
                ticket.OwnerUserId = User.Identity.GetUserId();
                db.Tickets.Add(ticket);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(tvm);
        }

        // GET: Tickets/Edit/5
        [Authorize( Roles = "Developer, Project Manager, Admin")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Ticket ticket = db.Tickets.Find(id);
            var userId = User.Identity.GetUserId();
            var helper = new ProjectUserHelper();

            if (ticket == null)
            {
                return HttpNotFound();
            }

            //verify that the user can edit this ticket - it is a ticket in one of their assigned projects
            if(User.IsInRole("Project Manager"))
            {
                if(!helper.IsUserInProject(userId, ticket.ProjectId))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
            }
            //same for developer - verify that they have been assigned this ticket and can edit it
            else if (User.IsInRole("Developer"))
            {
                //if string is not assigned, return a bad request
                if (string.IsNullOrEmpty(ticket.AssignedToUserId))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                else if (!ticket.AssignedToUserId.Equals(userId))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
            }

            var ticketEdit = new TicketEditViewModel();
            ticketEdit.Id = ticket.Id;
            ticketEdit.Title = ticket.Title;
            ticketEdit.Created = ticket.Created;
            ticketEdit.Updated = ticket.Updated;
            ticketEdit.Description = ticket.Description;

            //setting the default selected values of the TicketEditViewModel to the current values in the ticket
            //ticketEdit.ProjectId = ticket.ProjectId;
            //the project field is not editable
            var project = db.Projects.Find(ticket.ProjectId);
            ticketEdit.ProjectName = project.Name;
            ticketEdit.SelectedType = ticket.TicketTypeId;
            ticketEdit.SelectedPriority = ticket.TicketPriorityId;
            ticketEdit.SelectedStatus = ticket.TicketStatusId;
            //ticketEdit.OwnerUserId = ticket.OwnerUserId;
            if (!string.IsNullOrEmpty(ticket.AssignedToUserId))
            {
               var assignedTo = db.Users.Find(ticket.AssignedToUserId);
               ticketEdit.AssignedToUserName = assignedTo.FirstName + " " + assignedTo.LastName;
            }
            else
            {
               ticketEdit.AssignedToUserName = "Unassigned";
            }

            
            ticketEdit.TicketTypes = new SelectList(db.TicketTypes, "Id", "Name", ticketEdit.SelectedType);//ticket.TicketTypeId
            ticketEdit.TicketPriorities = new SelectList(db.TicketPriorities, "Id", "Name", ticketEdit.SelectedPriority);//ticket.TicketPriorityId
            ticketEdit.TicketStatuses = new SelectList(db.TicketStatuses, "Id", "Name", ticketEdit.SelectedStatus);//ticket.TicketStatusId

            return View(ticketEdit);         
        }

        // POST: Tickets/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Title,Description,Created,Updated,SelectedType,SelectedPriority,SelectedStatus")] TicketEditViewModel tevModel)
        {
            if (ModelState.IsValid)
            {
                Ticket ticket = db.Tickets.Find(tevModel.Id);
                ticket.Title = tevModel.Title;
                ticket.Description = tevModel.Description;
                ticket.Created = tevModel.Created;
                ticket.Updated = DateTimeOffset.Now;
                ticket.TicketTypeId = tevModel.SelectedType;
                ticket.TicketPriorityId = tevModel.SelectedPriority;
                ticket.TicketStatusId = tevModel.SelectedStatus;

                db.Entry(ticket).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(tevModel);
        }

        // GET: Tickets/Delete/5
        [Authorize(Roles = "Admin, Project Manager")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ticket ticket = db.Tickets.Find(id);
            if (ticket == null)
            {
                return HttpNotFound();
            }
            return View(ticket);
        }

        // POST: Tickets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Ticket ticket = db.Tickets.Find(id);
            db.Tickets.Remove(ticket);
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

        public List<TicketDetailsViewModel> transformTickets(List<Ticket> ticketList)
        {
            var ticketDetailsList = new List<TicketDetailsViewModel>();
            foreach (var ticket in ticketList)
            {
                var tdTicket = new TicketDetailsViewModel(ticket);
                ticketDetailsList.Add(tdTicket);
            }
            return ticketDetailsList;
        }
    }
}
