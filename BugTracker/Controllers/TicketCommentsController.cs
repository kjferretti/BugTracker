using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using BugTracker.Models;
using BugTracker.Models.CodeFirst;
using Microsoft.AspNet.Identity;
using BugTracker.Helper_Classes;
using System.Net.Mail;
using System.Threading.Tasks;

namespace BugTracker.Controllers
{
    [Authorize(Roles = "Administrator, Project Manager, Developer, Submitter")]
    public class TicketCommentsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private UserRolesManager URManager = new UserRolesManager();

        // POST: TicketComments/Create
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Id,Comment")] TicketComment ticketComment, int ticketId)
        {
            var ticket = db.Tickets.Find(ticketId);

            if (ModelState.IsValid && ticket.TicketStatus.Name != "Resolved")
            {
                ticketComment.Created = new DateTimeOffset(DateTime.Now, TimeZoneInfo.Local.GetUtcOffset(DateTime.Now));
                ticketComment.TicketId = ticketId;
                ticketComment.UserId = User.Identity.GetUserId();
                db.TicketComments.Add(ticketComment);
                db.SaveChanges();

                var currentUser = db.Users.Find(User.Identity.GetUserId());

                //make sure ticket is assigned
                if (!(ticket.AssignedToId == null || ticket.AssignedToId == "" || ticket.AssignedToId == "N/A"))
                {
                    //email notification
                    try
                    {
                        var from = "ReSolve Bugtracker<kjferretti@gmail.com>";
                        var to = db.Users.Find(ticket.AssignedToId).Email;
                        var email = new MailMessage(from, to)
                        {
                            Subject = "Comment added to your ticket",
                            Body = $"{currentUser.FirstName} {currentUser.LastName} has added a comment to a ticket that you are assigned to (<strong>{ticket.Title}</strong>). Ticket Details can be found <a href='http://kferretti-bugtracker.azurewebsites.net/Tickets/Details/{ticket.Id}'>here</a>.",
                            IsBodyHtml = true
                        };
                        var svc = new PersonalEmail();
                        await svc.SendAsync(email);
                        ViewBag.Message = "Email has been sent";
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        await Task.FromResult(0);
                    }
                }

                return RedirectToAction("Details", "Tickets", new { id = ticketId });
            }

            return RedirectToAction("Details", "Tickets", new { id = ticketId });
        }

        // GET: TicketComments/Edit/5
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

            var userId = User.Identity.GetUserId();
            if (!URManager.UserIsInRole(userId, "Administrator"))
            {
                if (!(ticketComment.Ticket.Project.InChargeOfId == userId))
                {
                    if (!(ticketComment.UserId == userId))
                    {
                        return RedirectToAction("Login", "Account");
                    }
                }
            }

            var ticket = db.Tickets.Find(ticketComment.TicketId);
            if (ticket.TicketStatus.Name == "Resolved")
            {
                return RedirectToAction("Details", "Tickets", new { id = ticketComment.TicketId });
            }

            return View(ticketComment);
        }

        // POST: TicketComments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,Comment,Created,TicketId,UserId")] TicketComment ticketComment)
        {
            if (ModelState.IsValid)
            {
                db.Entry(ticketComment).State = EntityState.Modified;
                db.SaveChanges();

                var ticket = db.Tickets.Find(ticketComment.TicketId);
                var currentUser = db.Users.Find(User.Identity.GetUserId());

                //make sure ticket is assigned
                if (!(ticket.AssignedToId == null || ticket.AssignedToId == "" || ticket.AssignedToId == "N/A"))
                {
                    //email notification
                    try
                    {
                        var from = "ReSolve Bugtracker<kjferretti@gmail.com>";
                        var to = db.Users.Find(ticket.AssignedToId).Email;
                        var email = new MailMessage(from, to)
                        {
                            Subject = "Comment added to your ticket",
                            Body = $"{currentUser.FirstName} {currentUser.LastName} has edited a comment on a ticket that you are assigned to (<strong>{ticket.Title}</strong>). Ticket Details can be found <a href='http://kferretti-bugtracker.azurewebsites.net/Tickets/Details/{ticket.Id}'>here</a>.",
                            IsBodyHtml = true
                        };
                        var svc = new PersonalEmail();
                        await svc.SendAsync(email);
                        ViewBag.Message = "Email has been sent";
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        await Task.FromResult(0);
                    }
                }

                return RedirectToAction("Details", "Tickets", new { id = ticketComment.TicketId });
            }
            return View(ticketComment);
        }

        // POST: TicketComments/Delete/5
        public async Task<ActionResult> Delete(int? id)
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

            var userId = User.Identity.GetUserId();
            if (!URManager.UserIsInRole(userId, "Administrator"))
            {
                if (!(ticketComment.Ticket.Project.InChargeOfId == userId))
                {
                    if (!(ticketComment.UserId == userId))
                    {
                        return RedirectToAction("Login", "Account");
                    }
                }
            }

            var ticket = db.Tickets.Find(ticketComment.TicketId);
            if (ticket.TicketStatus.Name == "Resolved")
            {
                return RedirectToAction("Details", "Tickets", new { id = ticketComment.TicketId });
            }

            db.TicketComments.Remove(ticketComment);
            db.SaveChanges();

            var currentUser = db.Users.Find(User.Identity.GetUserId());

            //make sure ticket is assigned
            if (!(ticket.AssignedToId == null || ticket.AssignedToId == "" || ticket.AssignedToId == "N/A"))
            {
                //email notification
                try
                {
                    var from = "ReSolve Bugtracker<kjferretti@gmail.com>";
                    var to = db.Users.Find(ticket.AssignedToId).Email;
                    var email = new MailMessage(from, to)
                    {
                        Subject = "Comment deleted from your ticket",
                        Body = $"{currentUser.FirstName} {currentUser.LastName} has deleted a comment from a ticket that you are assigned to (<strong>{ticket.Title}</strong>). Ticket Details can be found <a href='http://kferretti-bugtracker.azurewebsites.net/Tickets/Details/{ticket.Id}'>here</a>.",
                        IsBodyHtml = true
                    };
                    var svc = new PersonalEmail();
                    await svc.SendAsync(email);
                    ViewBag.Message = "Email has been sent";
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    await Task.FromResult(0);
                }
            }
            return RedirectToAction("Details", "Tickets", new { id = ticketComment.TicketId });
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
