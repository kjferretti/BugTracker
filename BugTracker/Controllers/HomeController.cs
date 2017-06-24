using BugTracker.Helper_Classes;
using BugTracker.Models;
using BugTracker.Models.CodeFirst;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BugTracker.Controllers
{
    public class HomeController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Landing()
        {
            if (User.IsInRole("Guest"))
            {
                return RedirectToAction("Index", "Guest");
            }
            return View();
        }

        public ActionResult Index()
        {
            if (User.IsInRole("Guest"))
            {
                return RedirectToAction("Index","Guest");
            }
            if (User.IsInRole("Administrator"))
            {
                return RedirectToAction("AdminDashboard");
            }
            if (User.IsInRole("Project Manager"))
            {
                return RedirectToAction("PMDashboard");
            }
            if (User.IsInRole("Developer"))
            {
                return RedirectToAction("DeveloperDashboard");
            }
            else
            {
                return RedirectToAction("SubmitterDashboard");
            }
        }

        [Authorize (Roles = "Administrator")]
        public ActionResult AdminDashboard()
        {
            var currentUserId = User.Identity.GetUserId();
            var currentUser = db.Users.Find(currentUserId);

            //Start ViewBags for Dashboard Count Values
            ViewBag.HighPriorityTicketsCount = db.Tickets.Where(t => t.TicketPriority.Name == "High").Count();

            var resolvedTickets = db.Tickets.Where(t => t.TicketStatus.Name == "Resolved");
            var resolvedCount = 0;
            foreach (var ticket in resolvedTickets.ToList())
            {
                var mostRecentHistory = ticket.TicketHistoryEvents.Reverse().FirstOrDefault(th => th.NewValue == "Resolved");
                var timeComparison = DateTimeOffset.Compare(mostRecentHistory.ChangedDate, DateTimeOffset.Now.AddDays(-7));
                if (timeComparison > 0)
                {
                    resolvedCount++;
                }
            }
            ViewBag.RecentlyResolvedCount = resolvedCount;

            var changeCount = 0;
            foreach (var ticket in db.Tickets.ToList())
            {
                changeCount += ticket.TicketHistoryEvents.Where(th => th.ChangedDate > DateTimeOffset.Now.AddDays(-7)).ToList().Count();
            }
            ViewBag.RecentChangesCount = changeCount;

            var assignedTickets = db.Tickets.Where(t => t.TicketStatus.Name == "Assigned");
            var assignCount = 0;
            foreach (var ticket in assignedTickets)
            {
                var mostRecentAssignment = ticket.TicketHistoryEvents.Reverse().FirstOrDefault(th => th.NewValue == "Assigned");
                var timeComparison = DateTimeOffset.Compare(mostRecentAssignment.ChangedDate, DateTimeOffset.Now.AddDays(-7));
                if (timeComparison > 0)
                {
                    assignCount++;
                }
            }
            ViewBag.RecentlyAssignedTicketsCount = assignCount;

            //Start ViewModel
            DashboardViewModel model = new DashboardViewModel();

            //Start Tickets for ViewModel
            model.Tickets = db.Tickets.Where(t => t.TicketStatus.Name == "Unassigned").ToList();

            //Start Projects for ViewModel
            model.Projects = currentUser.Projects.ToList();

            //Start TicketComments for ViewModel
            model.TicketComments = db.TicketComments.ToList().Where(tc => tc.Created > DateTimeOffset.Now.AddDays(-2)).ToList();

            RelativeTime relativeTime = new RelativeTime();

            if (model.TicketComments != null && model.TicketComments.Any())
            {
                foreach (var comment in model.TicketComments)
                {
                    comment.TimeSincePosted = relativeTime.TimeAgo(comment.Created);
                    db.Entry(comment).Property("TimeSincePosted").IsModified = true;
                    db.SaveChanges();
                }
            }

            //Start TicketAttachments for ViewModel
            model.TicketAttachments = db.TicketAttachments.ToList().Where(ta => ta.Created > DateTimeOffset.Now.AddDays(-2)).ToList();

            if (model.TicketAttachments != null && model.TicketAttachments.Any())
            {
                foreach (var attachment in model.TicketAttachments)
                {
                    attachment.TimeSincePosted = relativeTime.TimeAgo(attachment.Created);
                    db.Entry(attachment).Property("TimeSincePosted").IsModified = true;
                    db.SaveChanges();
                }
            }

            return View(model);
        }

        [Authorize(Roles = "Project Manager")]
        public ActionResult PMDashboard()
        {
            if (User.IsInRole("Administrator"))
            {
                return RedirectToAction("AdminDashboard");
            }

            var currentUserId = User.Identity.GetUserId();
            var currentUser = db.Users.Find(currentUserId);

            //Finding tickets this user has access to
            var projects = db.Projects.Where(p => p.InChargeOfId == currentUser.Id).Select(p => p.Id).AsEnumerable();
            var permissionTickets = db.Tickets.Where(t => (projects.Contains(t.ProjectId) || t.AssignedToId == currentUserId || t.SubmitterId == currentUserId) && t.TicketStatus.Name != "Resolved").ToList();

            //Start ViewBags for Dashboard Count Values
            ViewBag.HighPriorityTicketsCount = permissionTickets.Where(t => t.TicketPriority.Name == "High").Count();

            var changeCount = 0;
            foreach (var ticket in permissionTickets.ToList())
            {
                changeCount += ticket.TicketHistoryEvents.Where(th => th.ChangedDate > DateTimeOffset.Now.AddDays(-7)).ToList().Count();
            }
            ViewBag.RecentChangesCount = changeCount;

            var inChargeOfTickets = permissionTickets.Where(t => projects.Contains(t.ProjectId)).ToList();
            var assignedTickets = inChargeOfTickets.Where(t => t.TicketStatus.Name == "Assigned");
            var assignCount = 0;
            foreach (var ticket in assignedTickets)
            {
                var mostRecentAssignment = ticket.TicketHistoryEvents.Reverse().FirstOrDefault(th => th.NewValue == "Assigned");
                var timeComparison = DateTimeOffset.Compare(mostRecentAssignment.ChangedDate, DateTimeOffset.Now.AddDays(-7));
                if (timeComparison > 0)
                {
                    assignCount++;
                }
            }
            ViewBag.RecentlyAssignedTicketsCount = assignCount;

            //Start ViewModel
            DashboardViewModel model = new DashboardViewModel();

            //Start Tickets for ViewModel
            model.Tickets = inChargeOfTickets.Where(t => t.TicketStatus.Name == "Unassigned").ToList();

            //Start Projects for ViewModel
            model.Projects = currentUser.Projects.ToList();

            //Start TicketComments for ViewModel
            var pt = permissionTickets.Select(t => t.Id).AsEnumerable();
            var permissionComments = db.TicketComments.Where(tc => pt.Contains(tc.TicketId)).ToList();
            model.TicketComments = permissionComments.Where(tc => tc.Created > DateTimeOffset.Now.AddDays(-2)).ToList();

            RelativeTime relativeTime = new RelativeTime();

            if (model.TicketComments != null && model.TicketComments.Any())
            {
                foreach (var comment in model.TicketComments)
                {
                    comment.TimeSincePosted = relativeTime.TimeAgo(comment.Created);
                    db.Entry(comment).Property("TimeSincePosted").IsModified = true;
                    db.SaveChanges();
                }
            }

            //Start TicketAttachments for ViewModel
            var permissionAttachments = db.TicketAttachments.Where(ta => pt.Contains(ta.TicketId)).ToList();
            model.TicketAttachments = permissionAttachments.Where(ta => ta.Created > DateTimeOffset.Now.AddDays(-2)).ToList();

            if (model.TicketAttachments != null && model.TicketAttachments.Any())
            {
                foreach (var attachment in model.TicketAttachments)
                {
                    attachment.TimeSincePosted = relativeTime.TimeAgo(attachment.Created);
                    db.Entry(attachment).Property("TimeSincePosted").IsModified = true;
                    db.SaveChanges();
                }
            }

            return View(model);
        }

        [Authorize(Roles = "Developer")]
        public ActionResult DeveloperDashboard()
        {
            if (User.IsInRole("Administrator"))
            {
                return RedirectToAction("AdminDashboard");
            }
            if (User.IsInRole("Project Manager"))
            {
                return RedirectToAction("PMDashboard");
            }

            var currentUserId = User.Identity.GetUserId();
            var currentUser = db.Users.Find(currentUserId);

            //Finding tickets this user has access to
            var permissionTickets = new List<Ticket>();
            permissionTickets = db.Tickets.Where(t => (t.AssignedToId == currentUserId || t.SubmitterId == currentUserId) && t.TicketStatus.Name != "Resolved").ToList();

            //Start ViewBags for Dashboard Count Values
            ViewBag.HighPriorityTicketsCount = permissionTickets.Where(t => t.TicketPriority.Name == "High").Count();

            var changeCount = 0;
            foreach (var ticket in permissionTickets.ToList())
            {
                changeCount += ticket.TicketHistoryEvents.Where(th => th.ChangedDate > DateTimeOffset.Now.AddDays(-7)).ToList().Count();
            }
            ViewBag.RecentChangesCount = changeCount;

            //Start ViewModel
            DashboardViewModel model = new DashboardViewModel();

            //Start Tickets for ViewModel
            model.Tickets = permissionTickets.Where(t => t.TicketPriority.Name == "High").ToList();

            //Start Projects for ViewModel
            model.Projects = currentUser.Projects.ToList();

            //Start TicketComments for ViewModel
            var pt = permissionTickets.Select(t => t.Id).AsEnumerable();
            var permissionComments = db.TicketComments.Where(tc => pt.Contains(tc.TicketId)).ToList();
            model.TicketComments = permissionComments.Where(tc => tc.Created > DateTimeOffset.Now.AddDays(-2)).ToList();

            RelativeTime relativeTime = new RelativeTime();

            if (model.TicketComments != null && model.TicketComments.Any())
            {
                foreach (var comment in model.TicketComments)
                {
                    comment.TimeSincePosted = relativeTime.TimeAgo(comment.Created);
                    db.Entry(comment).Property("TimeSincePosted").IsModified = true;
                    db.SaveChanges();
                }
            }

            //Start TicketAttachments for ViewModel
            var permissionAttachments = db.TicketAttachments.Where(ta => pt.Contains(ta.TicketId)).ToList();
            model.TicketAttachments = permissionAttachments.Where(ta => ta.Created > DateTimeOffset.Now.AddDays(-2)).ToList();

            if (model.TicketAttachments != null && model.TicketAttachments.Any())
            {
                foreach (var attachment in model.TicketAttachments)
                {
                    attachment.TimeSincePosted = relativeTime.TimeAgo(attachment.Created);
                    db.Entry(attachment).Property("TimeSincePosted").IsModified = true;
                    db.SaveChanges();
                }
            }

            return View(model);
        }

        [Authorize(Roles = "Submitter")]
        public ActionResult SubmitterDashboard()
        {
            if (User.IsInRole("Administrator"))
            {
                return RedirectToAction("AdminDashboard");
            }
            if (User.IsInRole("Project Manager"))
            {
                return RedirectToAction("PMDashboard");
            }
            if (User.IsInRole("Developer"))
            {
                return RedirectToAction("DeveloperDashboard");
            }

            var currentUserId = User.Identity.GetUserId();
            var currentUser = db.Users.Find(currentUserId);

            //Finding tickets this user has access to
            var permissionTickets = new List<Ticket>();
            permissionTickets = db.Tickets.Where(t => t.SubmitterId == currentUserId && t.TicketStatus.Name != "Resolved").ToList();

            //Start ViewBags for Dashboard Count Values
            ViewBag.HighPriorityTicketsCount = permissionTickets.Where(t => t.TicketPriority.Name == "High").Count();

            var changeCount = 0;
            foreach (var ticket in permissionTickets.ToList())
            {
                changeCount += ticket.TicketHistoryEvents.Where(th => th.ChangedDate > DateTimeOffset.Now.AddDays(-7)).ToList().Count();
            }
            ViewBag.RecentChangesCount = changeCount;

            //Start ViewModel
            DashboardViewModel model = new DashboardViewModel();

            //Start Tickets for ViewModel
            model.Tickets = permissionTickets.Where(t => t.TicketPriority.Name == "High").ToList();

            //Start Projects for ViewModel
            model.Projects = currentUser.Projects.ToList();

            //Start TicketComments for ViewModel
            var pt = permissionTickets.Select(t => t.Id).AsEnumerable();
            var permissionComments = db.TicketComments.Where(tc => pt.Contains(tc.TicketId)).ToList();
            model.TicketComments = permissionComments.Where(tc => tc.Created > DateTimeOffset.Now.AddDays(-2)).ToList();

            RelativeTime relativeTime = new RelativeTime();

            if (model.TicketComments != null && model.TicketComments.Any())
            {
                foreach (var comment in model.TicketComments)
                {
                    comment.TimeSincePosted = relativeTime.TimeAgo(comment.Created);
                    db.Entry(comment).Property("TimeSincePosted").IsModified = true;
                    db.SaveChanges();
                }
            }

            //Start TicketAttachments for ViewModel
            var permissionAttachments = db.TicketAttachments.Where(ta => pt.Contains(ta.TicketId)).ToList();
            model.TicketAttachments = permissionAttachments.Where(ta => ta.Created > DateTimeOffset.Now.AddDays(-2)).ToList();

            if (model.TicketAttachments != null && model.TicketAttachments.Any())
            {
                foreach (var attachment in model.TicketAttachments)
                {
                    attachment.TimeSincePosted = relativeTime.TimeAgo(attachment.Created);
                    db.Entry(attachment).Property("TimeSincePosted").IsModified = true;
                    db.SaveChanges();
                }
            }

            return View(model);
        }

        public PartialViewResult Users()
        {
            ViewBag.Users = db.Users.Count();
            return PartialView("~/Views/Shared/_UsersCount.cshtml");
        }

        public PartialViewResult Projects()
        {
            if (User.IsInRole("Administrator"))
            {
                ViewBag.NonArchivedProjects = db.Projects.Where(p => !(p.Archived ?? false)).Count();
                ViewBag.ArchivedProjects = db.Projects.Where(p => p.Archived ?? false).Count();
                return PartialView("~/Views/Shared/_ProjectsCount.cshtml");
            }

            if (User.IsInRole("Project Manager"))
            {
                ViewBag.Projects = db.Projects.Where(p => !(p.Archived ?? false)).Count();
                return PartialView("~/Views/Shared/_ProjectsCount.cshtml");
            }

            if (User.IsInRole("Developer"))
            {
                ApplicationUser user = db.Users.Find(User.Identity.GetUserId());
                ViewBag.Projects = user.Projects.Where(p => !(p.Archived ?? false)).Count();
                return PartialView("~/Views/Shared/_ProjectsCount.cshtml");
            }

            if (User.IsInRole("Submitter"))
            {
                ApplicationUser user = db.Users.Find(User.Identity.GetUserId());
                ViewBag.Projects = user.Projects.Where(p => !(p.Archived ?? false)).Count();
                return PartialView("~/Views/Shared/_ProjectsCount.cshtml");
            }

            ViewBag.Projects = 0;
            return PartialView("~/Views/Shared/_ProjectsCount.cshtml");
        }

        public PartialViewResult Tickets()
        {
            if (User.IsInRole("Administrator"))
            {
                ViewBag.NonResolvedTickets = db.Tickets.Where(t => t.TicketStatus.Name != "Resolved").Count();
                ViewBag.ResolvedTickets = db.Tickets.Where(t => t.TicketStatus.Name == "Resolved").Count();
                return PartialView("~/Views/Shared/_TicketsCount.cshtml");
            }

            if (User.IsInRole("Project Manager"))
            {
                string userId = User.Identity.GetUserId();
                var projects = db.Projects.Where(p => p.InChargeOfId == userId).Select(p => p.Id).AsEnumerable();
                var tickets = db.Tickets.Where(t => projects.Contains(t.ProjectId) || t.AssignedToId == userId || t.SubmitterId == userId).ToList();
                ViewBag.Tickets = tickets.Where(t => t.TicketStatus.Name != "Resolved").Count();
                return PartialView("~/Views/Shared/_TicketsCount.cshtml");
            }

            if (User.IsInRole("Developer"))
            {
                string userId = User.Identity.GetUserId();
                var tickets = db.Tickets.Where(t => (t.AssignedToId == userId) || (t.SubmitterId == userId));
                ViewBag.Tickets = tickets.Where(t => t.TicketStatus.Name != "Resolved").Count();
                return PartialView("~/Views/Shared/_TicketsCount.cshtml");
            }

            if (User.IsInRole("Submitter"))
            {
                string userId = User.Identity.GetUserId();
                var tickets = db.Tickets.Where(t => t.SubmitterId == userId);
                ViewBag.Tickets = tickets.Where(t => t.TicketStatus.Name != "Resolved").Count();
                return PartialView("~/Views/Shared/_TicketsCount.cshtml");
            }

            ViewBag.Tickets = 0;
            return PartialView("~/Views/Shared/_TicketsCount.cshtml");
        }
    }
}