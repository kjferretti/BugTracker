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
using BugTracker.Helper_Classes;
using System.IO;
using Microsoft.AspNet.Identity;
using System.Net.Mail;
using System.Threading.Tasks;

namespace BugTracker.Controllers
{
    [Authorize(Roles = "Administrator, Project Manager, Developer, Submitter")]
    public class TicketAttachmentsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private UserRolesManager URManager = new UserRolesManager();

        // POST: TicketAttachments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Id,Description")] TicketAttachment ticketAttachment, int ticketId, HttpPostedFileBase file)
        {
            var ticket = db.Tickets.Find(ticketId);
            if (ModelState.IsValid && ticket.TicketStatus.Name != "Resolved")
            {
                if (FileUploadValidator.IsWebFriendlyFile(file))
                {
                    var filename = Path.GetFileName(file.FileName);
                    var customName = string.Format(Guid.NewGuid() + filename);
                    file.SaveAs(Path.Combine(Server.MapPath("~/app/uploads/"), customName));
                    ticketAttachment.FilePath = "/app/uploads/" + customName;
                    ticketAttachment.FileUrl = filename;
                    var fileExtensions = new[] { ".txt", ".doc", ".pdf" };
                    var extension = Path.GetExtension(file.FileName);
                    if (fileExtensions.Contains(extension))
                    {
                        ticketAttachment.FileType = "document";
                    }
                    else
                    {
                        ticketAttachment.FileType = "upload";
                    }
                }
                else
                {
                    ViewBag.Message = "Please select a valid format image";
                }
                ticketAttachment.Created = new DateTimeOffset(DateTime.Now, TimeZoneInfo.Local.GetUtcOffset(DateTime.Now));
                ticketAttachment.TicketId = ticketId;
                ticketAttachment.UserId = User.Identity.GetUserId();
                db.TicketAttachments.Add(ticketAttachment);
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
                            Subject = "Attachment added to your ticket",
                            Body = $"{currentUser.FirstName} {currentUser.LastName} has added an attachment to a ticket that you are assigned to (<strong>{ticket.Title}</strong>). Ticket Details can be found <a href='http://kferretti-bugtracker.azurewebsites.net/Tickets/Details/{ticket.Id}'>here</a>.",
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

        // POST: TicketAttachments/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TicketAttachment ticketAttachment = db.TicketAttachments.Find(id);
            if (ticketAttachment == null)
            {
                return HttpNotFound();
            }

            var ticket = db.Tickets.Find(ticketAttachment.TicketId);
            if (ticket.TicketStatus.Name != "Resolved")
            {
                var userId = User.Identity.GetUserId();
                if (!URManager.UserIsInRole(userId, "Administrator"))
                {
                    if (!(ticketAttachment.Ticket.Project.InChargeOfId == userId))
                    {
                        if (!(ticketAttachment.UserId == userId))
                        {
                            return RedirectToAction("Login", "Account");
                        }
                    }
                }

                db.TicketAttachments.Remove(ticketAttachment);
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
                            Subject = "Attachment deleted from your ticket",
                            Body = $"{currentUser.FirstName} {currentUser.LastName} has deleted an attachment from a ticket that you are assigned to (<strong>{ticket.Title}</strong>). Ticket Details can be found <a href='http://kferretti-bugtracker.azurewebsites.net/Tickets/Details/{ticket.Id}'>here</a>.",
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

                return RedirectToAction("Details", "Tickets", new { id = ticketAttachment.TicketId });
            }
            //error you cant make changes to resolved ticket
            return RedirectToAction("Details", "Tickets", new { id = ticketAttachment.TicketId });
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
