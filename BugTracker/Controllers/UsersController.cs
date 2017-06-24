using BugTracker.Helper_Classes;
using BugTracker.Models;
using BugTracker.Models.CodeFirst;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BugTracker.Controllers
{
    public class UsersController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private UserRolesManager URManager = new UserRolesManager();
        private ProjectAssignManager PAManager = new ProjectAssignManager();

        // GET: Users/Index
        [Authorize(Roles = "Administrator")]
        public ActionResult Index()
        {
            UsersViewModel model = new UsersViewModel();

            model.Users = db.Users.ToList();

            IList<IdentityRole> individualUserRoles = new List<IdentityRole>();
            IList<IdentityRole> individualNonUserRoles = new List<IdentityRole>();
            IList<Project> individualUserProjects = new List<Project>();
            IList<Ticket> individualUserTickets = new List<Ticket>();

            //for each user, need to get a list of unnasigned roles, assigned roles, unassigned projects, and unassigned tickets
            foreach (var user in model.Users)
            {
                var roles = user.Roles.Select(r => r.RoleId).AsEnumerable();
                individualUserRoles = db.Roles.Where(r => roles.Contains(r.Id) && r.Name != "Guest").ToList();
                model.UserRoles.Add(individualUserRoles);
                individualNonUserRoles = db.Roles.Where(r => !roles.Contains(r.Id) && r.Name != "Guest").ToList();
                model.NonUserRoles.Add(individualNonUserRoles);

                if (URManager.UserIsInRole(user.Id, "Administrator") || URManager.UserIsInRole(user.Id, "Project Manager") || URManager.UserIsInRole(user.Id, "Developer") || URManager.UserIsInRole(user.Id, "Submitter"))
                {
                    individualUserProjects = db.Projects.Where(p => !p.Users.Select(u => u.Id).Contains(user.Id)).ToList();
                    model.ProjectsForAssigning.Add(individualUserProjects);
                }
                else
                {
                    individualUserProjects = new List<Project>();
                    model.ProjectsForAssigning.Add(individualUserProjects);
                }
                if (URManager.UserIsInRole(user.Id, "Developer"))
                {
                    var projects = user.Projects.Select(p => p.Id).AsEnumerable();
                    individualUserTickets = db.Tickets.Where(t => projects.Contains(t.ProjectId) && t.AssignedToId != user.Id).ToList();
                    model.TicketsForAssigning.Add(individualUserTickets);
                }
                else
                {
                    individualUserTickets = new List<Ticket>();
                    model.TicketsForAssigning.Add(individualUserTickets);
                }
            }

            string adminId = db.Roles.FirstOrDefault(r => r.Name == "Administrator").Id;
            string pmId = db.Roles.FirstOrDefault(r => r.Name == "Project Manager").Id;
            string devId = db.Roles.FirstOrDefault(r => r.Name == "Developer").Id;
            string subId = db.Roles.FirstOrDefault(r => r.Name == "Submitter").Id;

            ViewBag.AdministratorsCount = db.Users.Where(u => u.Roles.Select(r1 => r1.RoleId).Contains(adminId)).Count();
            ViewBag.ProjectManagersCount = db.Users.Where(u => u.Roles.Select(r1 => r1.RoleId).Contains(pmId)).Count();
            ViewBag.DevelopersCount = db.Users.Where(u => u.Roles.Select(r1 => r1.RoleId).Contains(devId)).Count();
            ViewBag.SubmittersCount = db.Users.Where(u => u.Roles.Select(r1 => r1.RoleId).Contains(subId)).Count();
            return View(model);
        }

        // POST: Users/RoleAssign
        [Authorize (Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        public ActionResult RoleAssign([Bind (Include="Roles,UserId")] UsersViewModel model)
        {
            if (ModelState.IsValid && model.Roles != null && model.Roles.Any())
            {
                // need to maintain the format where everyone is submitter, PMs are also devs
                foreach(var role in model.Roles)
                {
                    if (!URManager.UserIsInRole(model.UserId, db.Roles.Find(role).Name))
                    {
                        if (db.Roles.Find(role).Name == "Administrator")
                        {
                            URManager.AddUserToRole(model.UserId, db.Roles.Find(role).Name);
                            if (!URManager.UserIsInRole(model.UserId, "Submitter"))
                            {
                                URManager.AddUserToRole(model.UserId, "Submitter");
                            }
                        }
                        if (db.Roles.Find(role).Name == "Project Manager")
                        {
                            URManager.AddUserToRole(model.UserId, db.Roles.Find(role).Name);
                            if (!URManager.UserIsInRole(model.UserId, "Developer"))
                            {
                                URManager.AddUserToRole(model.UserId, "Developer");
                            }
                            if (!URManager.UserIsInRole(model.UserId, "Submitter"))
                            {
                                URManager.AddUserToRole(model.UserId, "Submitter");
                            }
                        }
                        if (db.Roles.Find(role).Name == "Developer")
                        {
                            URManager.AddUserToRole(model.UserId, db.Roles.Find(role).Name);
                            if (!URManager.UserIsInRole(model.UserId, "Submitter"))
                            {
                                URManager.AddUserToRole(model.UserId, "Submitter");
                            }
                        }
                        if (db.Roles.Find(role).Name == "Submitter")
                        {
                            URManager.AddUserToRole(model.UserId, db.Roles.Find(role).Name);
                        }
                    }
                }

                if (URManager.UserIsInRole(model.UserId, "Administrator") || URManager.UserIsInRole(model.UserId, "Project Manager") || URManager.UserIsInRole(model.UserId, "Developer") || URManager.UserIsInRole(model.UserId, "Submitter"))
                {
                    URManager.RemoveUserFromRole(model.UserId, "Guest");
                }

                ViewBag.Message = "Role Assignments have been saved";

                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }

        // POST: Users/RoleUnassign
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        public ActionResult RoleUnassign([Bind(Include = "Roles,UserId")] UsersViewModel model)
        {
            if (ModelState.IsValid && model.Roles != null && model.Roles.Any())
            {
                // need to maintain the format where everyone is submitter, PMs are also devs
                foreach (var role in model.Roles)
                {
                    if (URManager.UserIsInRole(model.UserId, db.Roles.Find(role).Name))
                    {
                        if (db.Roles.Find(role).Name == "Administrator")
                        {
                            URManager.RemoveUserFromRole(model.UserId, db.Roles.Find(role).Name);
                        }
                        if (db.Roles.Find(role).Name == "Project Manager")
                        {
                            URManager.RemoveUserFromRole(model.UserId, db.Roles.Find(role).Name);
                        }
                        if (db.Roles.Find(role).Name == "Developer")
                        {
                            URManager.RemoveUserFromRole(model.UserId, db.Roles.Find(role).Name);
                            if (URManager.UserIsInRole(model.UserId, "Project Manager"))
                            {
                                URManager.RemoveUserFromRole(model.UserId, "Project Manager");
                            }
                        }
                        if (db.Roles.Find(role).Name == "Submitter")
                        {
                            URManager.RemoveUserFromRole(model.UserId, db.Roles.Find(role).Name);
                            if (URManager.UserIsInRole(model.UserId, "Administrator"))
                            {
                                URManager.RemoveUserFromRole(model.UserId, "Administrator");
                            }
                            if (URManager.UserIsInRole(model.UserId, "Project Manager"))
                            {
                                URManager.RemoveUserFromRole(model.UserId, "Project Manager");
                            }
                            if (URManager.UserIsInRole(model.UserId, "Developer"))
                            {
                                URManager.RemoveUserFromRole(model.UserId, "Developer");
                            }
                        }
                    }
                }

                if (!(URManager.UserIsInRole(model.UserId, "Administrator") || URManager.UserIsInRole(model.UserId, "Project Manager") || URManager.UserIsInRole(model.UserId, "Developer") || URManager.UserIsInRole(model.UserId, "Submitter")))
                {
                    URManager.AddUserToRole(model.UserId, "Guest");
                }

                ViewBag.Message = "Role Assignments have been saved";

                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }

        // POST: Users/ProjectAssign
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        public ActionResult ProjectAssign([Bind(Include = "Projects,UserId")] UsersViewModel model)
        {
            var user = db.Users.Find(model.UserId);

            if (ModelState.IsValid && model.Projects != null && model.Projects.Any())
            {
                foreach (var project in model.Projects)
                {
                    if (!user.Projects.Select(p => p.Id).Contains(Convert.ToInt32(project)))
                    {
                        PAManager.AddUserToProject(model.UserId, Convert.ToInt32(project));
                    }     
                }

                ViewBag.Message = "Project Assignments have been saved";
    
                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }

        // POST: Users/TicketAssign
        [Authorize(Roles = "Administrator")]
        [ValidateAntiForgeryToken]
        public ActionResult TicketAssign([Bind(Include = "Tickets,UserId")] UsersViewModel model)
        {
            var user = db.Users.Find(model.UserId);

            if (ModelState.IsValid && model.Tickets != null && model.Tickets.Any())
            {
                foreach (var ticket in model.Tickets)
                {
                    if (URManager.UserIsInRole(model.UserId, "Developer"))
                    {
                        Ticket Ticket = db.Tickets.Find(Convert.ToInt32(ticket));
                        if (Ticket.AssignedToId != model.UserId)
                        {
                            if ("Assigned" != db.TicketStatuses.Find(Ticket.TicketStatusId).Name)
                            {
                                TicketHistoryEvent th = new TicketHistoryEvent
                                {
                                    TicketId = Ticket.Id,
                                    Property = "Status",
                                    NewValue = "Assigned",
                                    OldValue = db.TicketStatuses.Find(Ticket.TicketStatusId).Name,
                                    ChangedDate = new DateTimeOffset(DateTime.Now, TimeZoneInfo.Local.GetUtcOffset(DateTime.Now)),
                                    UserId = User.Identity.GetUserId()
                                };
                                db.TicketHistoryEvents.Add(th);
                                db.SaveChanges();
                            }

                            if (Ticket.AssignedToId == null || Ticket.AssignedToId == "" || Ticket.AssignedToId == "N/A")
                            {
                                TicketHistoryEvent th1 = new TicketHistoryEvent
                                {
                                    TicketId = Ticket.Id,
                                    Property = "Assigned To",
                                    NewValue = user.FirstName + " " + user.LastName,
                                    OldValue = "N/A",
                                    ChangedDate = new DateTimeOffset(DateTime.Now, TimeZoneInfo.Local.GetUtcOffset(DateTime.Now)),
                                    UserId = User.Identity.GetUserId()
                                };
                                db.TicketHistoryEvents.Add(th1);
                                db.SaveChanges();
                            }

                            else if (user.FirstName + " " + user.LastName != db.Users.Find(Ticket.AssignedToId).FirstName + " " + db.Users.Find(Ticket.AssignedToId).LastName)
                            {
                                TicketHistoryEvent th1 = new TicketHistoryEvent
                                {
                                    TicketId = Ticket.Id,
                                    Property = "Assigned To",
                                    NewValue = user.FirstName + " " + user.LastName,
                                    OldValue = db.Users.Find(Ticket.AssignedToId).FirstName + " " + db.Users.Find(Ticket.AssignedToId).LastName,
                                    ChangedDate = new DateTimeOffset(DateTime.Now, TimeZoneInfo.Local.GetUtcOffset(DateTime.Now)),
                                    UserId = User.Identity.GetUserId()
                                };
                                db.TicketHistoryEvents.Add(th1);
                                db.SaveChanges();
                            }

                            Ticket.AssignedToId = model.UserId;
                            db.Entry(Ticket).Property("AssignedToId").IsModified = true;
                            db.SaveChanges();
                            int ticketStatusId = db.TicketStatuses.FirstOrDefault(ts => ts.Name == "Assigned").Id;
                            Ticket.TicketStatusId = ticketStatusId;
                            db.Entry(Ticket).Property("TicketStatusId").IsModified = true;
                            db.SaveChanges();
                        }  
                    }
                }

                ViewBag.Message = "Project Assignments have been saved";

                return RedirectToAction("Index");
            }

            return RedirectToAction("Index");
        }

        [ChildActionOnly]
        [Authorize (Roles = "Administrator, Project Manager, Developer, Submitter, Guest")]
        public PartialViewResult UserPartial()
        {
            var userId = User.Identity.GetUserId();
            ApplicationUser user = db.Users.Find(userId);
            ViewBag.Role = URManager.PriorityRole(userId);
            return PartialView("~/Views/Shared/_UserPartial.cshtml", user);
        }
    }
}