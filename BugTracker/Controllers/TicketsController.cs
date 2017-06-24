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
using System.Threading.Tasks;
using System.Net.Mail;
using System.Configuration;

namespace BugTracker.Controllers
{
    [Authorize (Roles = "Administrator, Project Manager, Developer, Submitter")]
    public class TicketsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private UserRolesManager URManager = new UserRolesManager();

        // GET: Tickets
        public ActionResult Index()
        {
            string userId = User.Identity.GetUserId();
            var user = db.Users.Find(userId);
            var ticketsInclude = db.Tickets.Include(t => t.AssignedTo).Include(t => t.Project).Include(t => t.Submitter).Include(t => t.TicketPriority).Include(t => t.TicketStatus).Include(t => t.TicketType);

            if (URManager.UserIsInRole(userId, "Administrator"))
            {
                //all tickets
                var tickets = ticketsInclude.ToList();
                ViewBag.IssuesCount = tickets.Where(t => t.TicketType.Name == "Issue").Count();
                ViewBag.BugsCount = tickets.Where(t => t.TicketType.Name == "Bug").Count();
                return View(tickets);
            }

            else if (URManager.UserIsInRole(userId, "Project Manager"))
            {
                //tickets for projects PM is in charge of && accounting for if submitter or developer roles also apply
                var projects = db.Projects.Where(p => p.InChargeOfId == user.Id).Select(p => p.Id).AsEnumerable();
                var tickets = ticketsInclude.Where(t => (projects.Contains(t.ProjectId) || t.AssignedToId == userId || t.SubmitterId == userId) && t.TicketStatus.Name != "Resolved").ToList();
                ViewBag.IssuesCount = tickets.Where(t => t.TicketType.Name == "Issue").Count();
                ViewBag.BugsCount = tickets.Where(t => t.TicketType.Name == "Bug").Count();
                return View(tickets);
            }

            else if (URManager.UserIsInRole(userId, "Developer"))
            {
                //tickets dev is assigned to & account for possibility of submitter
                var tickets = ticketsInclude.Where(t => (t.AssignedToId == userId || t.SubmitterId == userId) && t.TicketStatus.Name != "Resolved");
                ViewBag.IssuesCount = tickets.Where(t => t.TicketType.Name == "Issue").Count();
                ViewBag.BugsCount = tickets.Where(t => t.TicketType.Name == "Bug").Count();
                return View(tickets);
            }
            else if (URManager.UserIsInRole(userId, "Submitter"))
            {
                //tickets submitter has submitted
                var tickets = ticketsInclude.Where(t => t.SubmitterId == userId && t.TicketStatus.Name != "Resolved");
                ViewBag.IssuesCount = tickets.Where(t => t.TicketType.Name == "Issue").Count();
                ViewBag.BugsCount = tickets.Where(t => t.TicketType.Name == "Bug").Count();
                return View(tickets);
            }
            else
            {
                List<Ticket> tickets = new List<Ticket>();
                ViewBag.IssuesCount = tickets.Where(t => t.TicketType.Name == "Issue").Count();
                ViewBag.BugsCount = tickets.Where(t => t.TicketType.Name == "Bug").Count();
                return View(tickets);
            }
        }

        // GET: Tickets/Details/5
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
            if (!URManager.UserIsInRole(userId, "Administrator"))
            {
                if(!(ticket.Project.InChargeOfId == userId))
                {
                    if(!(ticket.AssignedToId == userId))
                    {
                        if(!(ticket.SubmitterId == userId))
                        {
                            return RedirectToAction("Login", "Account");
                        }
                    }
                }
            }

            if(ticket.TicketStatus.Name == "Resolved" && !User.IsInRole("Administrator"))
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.NumberOfComments = ticket.TicketComments.Count;

            //definitely a better way to do this part. doesn't need to be stored in a database
            RelativeTime relativeTime = new RelativeTime();

            if (ticket.TicketComments != null && ticket.TicketComments.Any())
            {
                foreach (var comment in ticket.TicketComments)
                {
                    comment.TimeSincePosted = relativeTime.TimeAgo(comment.Created);
                    db.Entry(comment).Property("TimeSincePosted").IsModified = true;
                    db.SaveChanges();
                }
            }

            ViewBag.NumberOfAttachments = ticket.TicketAttachments.Count;

            if (ticket.TicketAttachments != null && ticket.TicketAttachments.Any())
            {
                foreach (var attachment in ticket.TicketAttachments)
                {
                    attachment.TimeSincePosted = relativeTime.TimeAgo(attachment.Created);
                    db.Entry(attachment).Property("TimeSincePosted").IsModified = true;
                    db.SaveChanges();
                }
            }

            ViewBag.NumberOfChanges = ticket.TicketHistoryEvents.Count - 4;

            TicketHistoryViewModel vm = new TicketHistoryViewModel();
            vm.Ticket = ticket;
            vm.CreatedBy = db.Users.Find(ticket.TicketHistoryEvents.ToList()[0].UserId);
            vm.CreatedId = ticket.TicketHistoryEvents.ToList()[0].NewValue;
            vm.CreatedDescription = ticket.TicketHistoryEvents.ToList()[1].NewValue;
            vm.CreatedPriority = ticket.TicketHistoryEvents.ToList()[2].NewValue;
            vm.CreatedStatus = ticket.TicketHistoryEvents.ToList()[3].NewValue;

            return View(vm);
        }

        // GET: Tickets/Create
        [Authorize(Roles = "Submitter")]
        public ActionResult Create(int? projectId)
        {
            var userId = User.Identity.GetUserId();
            var user = db.Users.Find(userId);

            if (projectId == null)
            {
                ViewBag.ProjectId = new SelectList(user.Projects, "Id", "Name");
                ViewBag.TicketPriorityId = new SelectList(db.TicketPriorities, "Id", "Name");
                ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Name");
                ViewBag.ProjectKnown = false;
                return View();
            }
            projectId = projectId ?? default(int);
            ViewBag.TicketPriorityId = new SelectList(db.TicketPriorities, "Id", "Name");
            ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Name");
            ViewBag.ProjectKnown = true;
            ViewBag.HiddenProjectId = projectId;
            ViewBag.ProjectName = db.Projects.Find(projectId).Name;
            return View();
        }

        // POST: Tickets/Create
        [Authorize(Roles = "Submitter")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Description,ProjectId,TicketTypeId,TicketPriorityId")] Ticket ticket)
        {
            var userId = User.Identity.GetUserId();
            var user = db.Users.Find(userId);
            
            if (ModelState.IsValid && user.Projects.Select(p => p.Id).Contains(ticket.ProjectId))
            {
                ticket.SubmitterId = userId;
                ticket.Created = new DateTimeOffset(DateTime.Now, TimeZoneInfo.Local.GetUtcOffset(DateTime.Now));
                int ticketStatusId = db.TicketStatuses.FirstOrDefault(ts => ts.Name == "Unassigned").Id;
                ticket.TicketStatusId = ticketStatusId;
                db.Tickets.Add(ticket);
                db.SaveChanges();
                //makes the id look more official and systematic
                string paddedId = ticket.Id.ToString("0000");
                ticket.Title = "BI:" + paddedId;
                db.Entry(ticket).Property("Title").IsModified = true;
                db.SaveChanges();
                Ticket savedTicket = db.Tickets.Find(ticket.Id);

                //Ticket history creation
                TicketHistoryEvent th1 = new TicketHistoryEvent
                {
                    TicketId = savedTicket.Id,
                    Property = "ID#",
                    NewValue = savedTicket.Title,
                    ChangedDate = savedTicket.Created,
                    UserId = savedTicket.SubmitterId
                };
                TicketHistoryEvent th2 = new TicketHistoryEvent
                {
                    TicketId = savedTicket.Id,
                    Property = "Description",
                    NewValue = savedTicket.Description,
                    ChangedDate = savedTicket.Created,
                    UserId = savedTicket.SubmitterId
                };
                TicketHistoryEvent th3 = new TicketHistoryEvent
                {
                    TicketId = savedTicket.Id,
                    Property = "Priority",
                    NewValue = db.TicketPriorities.Find(savedTicket.TicketPriorityId).Name,
                    ChangedDate = savedTicket.Created,
                    UserId = savedTicket.SubmitterId
                };
                TicketHistoryEvent th4 = new TicketHistoryEvent
                {
                    TicketId = savedTicket.Id,
                    Property = "Status",
                    NewValue = db.TicketStatuses.Find(savedTicket.TicketStatusId).Name,
                    ChangedDate = savedTicket.Created,
                    UserId = savedTicket.SubmitterId
                };
                db.TicketHistoryEvents.Add(th1);
                db.SaveChanges();
                db.TicketHistoryEvents.Add(th2);
                db.SaveChanges();
                db.TicketHistoryEvents.Add(th3);
                db.SaveChanges();
                db.TicketHistoryEvents.Add(th4);
                db.SaveChanges();
                return RedirectToAction("Details", new { id = ticket.Id });
            }
            ViewBag.ProjectKnown = false;
            ViewBag.AssignedToId = new SelectList(db.Users, "Id", "FirstName", ticket.AssignedToId);
            ViewBag.ProjectId = new SelectList(user.Projects, "Id", "Name", ticket.ProjectId);
            ViewBag.SubmitterId = new SelectList(db.Users, "Id", "FirstName", ticket.SubmitterId);
            ViewBag.TicketPriorityId = new SelectList(db.TicketPriorities, "Id", "Name", ticket.TicketPriorityId);
            ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Name", ticket.TicketTypeId);
            return View(ticket);
        }

        // GET: Tickets/Edit/5
        public ActionResult Edit(int? id)
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


            //make sure this user is allowed
            var userId = User.Identity.GetUserId();
            if (!URManager.UserIsInRole(userId, "Administrator"))
            {
                if (!(ticket.Project.InChargeOfId == userId))
                {
                    if (!(ticket.AssignedToId == userId))
                    {
                        if (!(ticket.SubmitterId == userId))
                        {
                            return RedirectToAction("Login", "Account");
                        }
                    }
                }
            }

            //cant alter resolved tickets
            if (ticket.TicketStatus.Name == "Resolved")
            {
                return RedirectToAction("Details", "Tickets", new { id = ticket.Id });
            }

            ViewBag.TicketPriorityId = new SelectList(db.TicketPriorities, "Id", "Name", ticket.TicketPriorityId);
            ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Name", ticket.TicketTypeId);
            return View(ticket);
        }

        // POST: Tickets/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "Id,Title,Description,Created,ProjectId,TicketTypeId,TicketPriorityId,SubmitterId,AssignedToId,TicketStatusId")] Ticket ticket)
        {
            if (ModelState.IsValid)
            {
                Ticket oldTicket = db.Tickets.AsNoTracking().FirstOrDefault(t => t.Id == ticket.Id);
                var currentUser = db.Users.Find(User.Identity.GetUserId());
                ticket.Updated = new DateTimeOffset(DateTime.Now, TimeZoneInfo.Local.GetUtcOffset(DateTime.Now));
                db.Entry(ticket).State = EntityState.Modified;
                db.SaveChanges();

                if (oldTicket.Title != ticket.Title)
                {
                    TicketHistoryEvent th = new TicketHistoryEvent
                    {
                        TicketId = ticket.Id,
                        Property = "Title",
                        NewValue = ticket.Title,
                        OldValue = oldTicket.Title,
                        ChangedDate = new DateTimeOffset(DateTime.Now, TimeZoneInfo.Local.GetUtcOffset(DateTime.Now)),
                        UserId = User.Identity.GetUserId()
                    };
                    db.TicketHistoryEvents.Add(th);
                    db.SaveChanges();

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
                                Subject = "Title of your ticket has been edited",
                                Body = $"A ticket you are assigned to (<strong>{ticket.Title}</strong>) has had its title changed from \"{oldTicket.Title}\" to \"{ticket.Title}\" by {currentUser.FirstName} {currentUser.LastName}. Ticket Details can be found <a href='http://kferretti-bugtracker.azurewebsites.net/Tickets/Details/{ticket.Id}'>here</a>.",
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
                }
                if (oldTicket.Description != ticket.Description)
                {
                    TicketHistoryEvent th = new TicketHistoryEvent
                    {
                        TicketId = ticket.Id,
                        Property = "Description",
                        NewValue = ticket.Description,
                        OldValue = oldTicket.Description,
                        ChangedDate = new DateTimeOffset(DateTime.Now, TimeZoneInfo.Local.GetUtcOffset(DateTime.Now)),  
                        UserId = User.Identity.GetUserId()
                    };
                    db.TicketHistoryEvents.Add(th);
                    db.SaveChanges();

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
                                Subject = "Description of your ticket has been edited",
                                Body = $"A ticket you are assigned to (<strong>{ticket.Title}</strong>) has had its description changed from \"{oldTicket.Description}\" to \"{ticket.Description}\" by {currentUser.FirstName} {currentUser.LastName}. Ticket Details can be found <a href='http://kferretti-bugtracker.azurewebsites.net/Tickets/Details/{ticket.Id}'>here</a>.",
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
                }
                if (oldTicket.TicketPriorityId != ticket.TicketPriorityId)
                {
                    TicketHistoryEvent th = new TicketHistoryEvent
                    {
                        TicketId = ticket.Id,
                        Property = "Priority",
                        NewValue = db.TicketPriorities.Find(ticket.TicketPriorityId).Name,
                        OldValue = db.TicketPriorities.Find(oldTicket.TicketPriorityId).Name,
                        ChangedDate = new DateTimeOffset(DateTime.Now, TimeZoneInfo.Local.GetUtcOffset(DateTime.Now)), //DateTimeOffset.Now.ToLocalTime(),
                        UserId = User.Identity.GetUserId()
                    };
                    db.TicketHistoryEvents.Add(th);
                    db.SaveChanges();

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
                                Subject = "Priority of your ticket has been edited",
                                Body = $"A ticket you are assigned to (<strong>{ticket.Title}</strong>) has had its priority changed from \"{db.TicketPriorities.Find(oldTicket.TicketPriorityId).Name}\" to \"{db.TicketPriorities.Find(ticket.TicketPriorityId).Name}\" by {currentUser.FirstName} {currentUser.LastName}. Ticket Details can be found <a href='http://kferretti-bugtracker.azurewebsites.net/Tickets/Details/{ticket.Id}'>here</a>.",
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
                }

                if (oldTicket.TicketTypeId != ticket.TicketTypeId)
                {
                    TicketHistoryEvent th = new TicketHistoryEvent
                    {
                        TicketId = ticket.Id,
                        Property = "Type",
                        NewValue = db.TicketTypes.Find(ticket.TicketTypeId).Name,
                        OldValue = db.TicketTypes.Find(oldTicket.TicketTypeId).Name,
                        ChangedDate = new DateTimeOffset(DateTime.Now, TimeZoneInfo.Local.GetUtcOffset(DateTime.Now)),
                        UserId = User.Identity.GetUserId()
                    };
                    db.TicketHistoryEvents.Add(th);
                    db.SaveChanges();

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
                                Subject = "Type of your ticket has been edited",
                                Body = $"A ticket you are assigned to (<strong>{ticket.Title}</strong>) has had its type changed from \"{db.TicketTypes.Find(oldTicket.TicketTypeId).Name}\" to \"{db.TicketTypes.Find(ticket.TicketTypeId).Name}\" by {currentUser.FirstName} {currentUser.LastName}. Ticket Details can be found <a href='http://kferretti-bugtracker.azurewebsites.net/Tickets/Details/{ticket.Id}'>here</a>.",
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
                }

                return RedirectToAction("Details", new { id = ticket.Id });
            }
            
            ViewBag.TicketPriorityId = new SelectList(db.TicketPriorities, "Id", "Name", ticket.TicketPriorityId);
            ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Name", ticket.TicketTypeId);
            return View(ticket);
        }

        // GET: Tickets/Assign/5
        [Authorize (Roles = "Administrator, Project Manager")]
        public ActionResult Assign(int? ticketId)
        {
            if (ticketId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ticket ticket = db.Tickets.Find(ticketId);
            if (ticket == null)
            {
                return HttpNotFound();
            }

            //cant alter resolved tickets
            if (ticket.TicketStatus.Name == "Resolved")
            {
                return RedirectToAction("Details", "Tickets", new { id = ticket.Id });
            }

            var userId = User.Identity.GetUserId();
            if (!URManager.UserIsInRole(userId, "Administrator"))
            {
                if (!(ticket.Project.InChargeOfId == userId))
                {
                    return RedirectToAction("Login", "Account");
                }
            }

            //developers in dropdown
            var userOptions = URManager.UsersInRole("Developer").Where(u => ticket.Project.Users.Select(u1 => u1.Id).Contains(u.Id));
            ViewBag.UserId = new SelectList((from u in userOptions select new { Id = u.Id, FullName = u.FirstName + " " + u.LastName }), "Id", "FullName");

            TicketAssignViewModel model = new TicketAssignViewModel();
            model.ticketId = ticketId;

            if(ticket.AssignedToId == null || ticket.AssignedToId == "" || ticket.AssignedToId == "N/A")
            {
                ViewBag.TicketIsAssigned = false;
            }
            else
            {
                ViewBag.TicketIsAssigned = true;
            }
            
            return View(model);
        }

        // POST: Tickets/Assign/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator, Project Manager")]
        public async Task<ActionResult> Assign([Bind (Include="UserId,ticketId")] TicketAssignViewModel model)
        {
            Ticket ticket = db.Tickets.Find(model.ticketId);
            var currentUser = db.Users.Find(User.Identity.GetUserId());

            //make sure user is eligable for assignment
            if (ModelState.IsValid && URManager.UsersInRole("Developer").Where(u => ticket.Project.Users.Select(u1 => u1.Id).Contains(u.Id)).Select(u2 => u2.Id).Contains(model.UserId))
            {
                if("Assigned" != db.TicketStatuses.Find(ticket.TicketStatusId).Name)
                {
                    TicketHistoryEvent th = new TicketHistoryEvent
                    {
                        TicketId = ticket.Id,
                        Property = "Status",
                        NewValue = "Assigned",
                        OldValue = db.TicketStatuses.Find(ticket.TicketStatusId).Name,
                        ChangedDate = new DateTimeOffset(DateTime.Now, TimeZoneInfo.Local.GetUtcOffset(DateTime.Now)),
                        UserId = User.Identity.GetUserId()
                    };
                    db.TicketHistoryEvents.Add(th);
                    db.SaveChanges();
                }

                var user = db.Users.Find(model.UserId);
                if (ticket.AssignedToId == null || ticket.AssignedToId == "" || ticket.AssignedToId == "N/A")
                {
                    TicketHistoryEvent th1 = new TicketHistoryEvent
                    {
                        TicketId = ticket.Id,
                        Property = "Assigned To",
                        NewValue = user.FirstName + " " + user.LastName,
                        OldValue = "N/A",
                        ChangedDate = new DateTimeOffset(DateTime.Now, TimeZoneInfo.Local.GetUtcOffset(DateTime.Now)),
                        UserId = User.Identity.GetUserId()
                    };
                    db.TicketHistoryEvents.Add(th1);
                    db.SaveChanges();
                }
                else if (user.FirstName + " " + user.LastName != db.Users.Find(ticket.AssignedToId).FirstName + " " + db.Users.Find(ticket.AssignedToId).LastName)
                {
                    TicketHistoryEvent th1 = new TicketHistoryEvent
                    {
                        TicketId = ticket.Id,
                        Property = "Assigned To",
                        NewValue = user.FirstName + " " + user.LastName,
                        OldValue = db.Users.Find(ticket.AssignedToId).FirstName + " " + db.Users.Find(ticket.AssignedToId).LastName,
                        ChangedDate = new DateTimeOffset(DateTime.Now, TimeZoneInfo.Local.GetUtcOffset(DateTime.Now)),
                        UserId = User.Identity.GetUserId()
                    };
                    db.TicketHistoryEvents.Add(th1);
                    db.SaveChanges();

                    //email notification
                    try
                    {
                        var from = "ReSolve Bugtracker<kjferretti@gmail.com>";
                        var to = db.Users.Find(ticket.AssignedToId).Email;
                        var email = new MailMessage(from, to)
                        {
                            Subject = "You were unassigned",
                            Body = $"You were unassigned from ticket: <strong>{ticket.Title}</strong> by {currentUser.FirstName} {currentUser.LastName}.",
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

                ticket.AssignedToId = model.UserId;
                db.Entry(ticket).Property("AssignedToId").IsModified = true;
                db.SaveChanges();
                int ticketStatusId = db.TicketStatuses.FirstOrDefault(ts => ts.Name == "Assigned").Id;
                ticket.TicketStatusId = ticketStatusId;
                db.Entry(ticket).Property("TicketStatusId").IsModified = true;
                db.SaveChanges();

                //email notification
                try
                {
                    var from = "ReSolve Bugtracker<kjferretti@gmail.com>";
                    var to = user.Email;
                    var email = new MailMessage(from, to)
                    {
                        Subject = "New Ticket Assignment",
                        Body = $"You were assigned to ticket: <strong>{ticket.Title}</strong> by {currentUser.FirstName} {currentUser.LastName}. Ticket Details can be found <a href='http://kferretti-bugtracker.azurewebsites.net/Tickets/Details/{ticket.Id}'>here</a>.",
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

            return RedirectToAction("Details", new { id = ticket.Id });
        }

        // POST: Tickets/Unassign/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator, Project Manager")]
        public async Task<ActionResult> Unassign([Bind(Include = "UserId,ticketId")] TicketAssignViewModel model)
        {
            Ticket ticket = db.Tickets.Find(model.ticketId);

            var userId = User.Identity.GetUserId();
            var currentUser = db.Users.Find(userId);
            if (!URManager.UserIsInRole(userId, "Administrator"))
            {
                if (!(ticket.Project.InChargeOfId == userId))
                {
                    return RedirectToAction("Login", "Account");
                }
            }

            if ("Unassigned" != db.TicketStatuses.Find(ticket.TicketStatusId).Name)
            {
                TicketHistoryEvent th = new TicketHistoryEvent
                {
                    TicketId = ticket.Id,
                    Property = "Status",
                    NewValue = "Unassigned",
                    OldValue = db.TicketStatuses.Find(ticket.TicketStatusId).Name,
                    ChangedDate = new DateTimeOffset(DateTime.Now, TimeZoneInfo.Local.GetUtcOffset(DateTime.Now)),
                    UserId = User.Identity.GetUserId()
                };
                db.TicketHistoryEvents.Add(th);
                db.SaveChanges();
            }

            var user = db.Users.Find(model.UserId);
            if (!(ticket.AssignedToId == null || ticket.AssignedToId == "" || ticket.AssignedToId == "N/A"))
            {
                TicketHistoryEvent th1 = new TicketHistoryEvent
                {
                    TicketId = ticket.Id,
                    Property = "Assigned To",
                    NewValue = "N/A",
                    OldValue = db.Users.Find(ticket.AssignedToId).FirstName + " " + db.Users.Find(ticket.AssignedToId).LastName,
                    ChangedDate = new DateTimeOffset(DateTime.Now, TimeZoneInfo.Local.GetUtcOffset(DateTime.Now)),
                    UserId = User.Identity.GetUserId()
                };
                db.TicketHistoryEvents.Add(th1);
                db.SaveChanges();

                //email notification
                try
                {
                    var from = "ReSolve Bugtracker<kjferretti@gmail.com>";
                    var to = db.Users.Find(ticket.AssignedToId).Email;
                    var email = new MailMessage(from, to)
                    {
                        Subject = "You were unassigned",
                        Body = $"You were unassigned from ticket: <strong>{ticket.Title}</strong> by {currentUser.FirstName} {currentUser.LastName}.",
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

            ticket.AssignedToId = null;
            db.Entry(ticket).Property("AssignedToId").IsModified = true;
            db.SaveChanges();
            int ticketStatusId = db.TicketStatuses.FirstOrDefault(ts => ts.Name == "Unassigned").Id;
            ticket.TicketStatusId = ticketStatusId;
            db.Entry(ticket).Property("TicketStatusId").IsModified = true;
            db.SaveChanges();

            return RedirectToAction("Details", new { id = ticket.Id });
        }


        // for making tickets resolved, not permanently deleted
        // GET: Tickets/Delete/5
        [Authorize(Roles = "Administrator, Project Manager")]
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

            var userId = User.Identity.GetUserId();
            if (!URManager.UserIsInRole(userId, "Administrator"))
            {
                if (!(ticket.Project.InChargeOfId == userId))
                {
                    return RedirectToAction("Login", "Account");
                }
            }

            return View(ticket);
        }

        // POST: Tickets/Delete/5
        [Authorize(Roles = "Administrator, Project Manager")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Ticket ticket = db.Tickets.Find(id);
            var currentUser = db.Users.Find(User.Identity.GetUserId());

            if (ticket.TicketStatus.Name != "Resolved")
            {
                TicketHistoryEvent th = new TicketHistoryEvent
                {
                    TicketId = ticket.Id,
                    Property = "Status",
                    NewValue = "Resolved",
                    OldValue = db.TicketStatuses.Find(ticket.TicketStatusId).Name,
                    ChangedDate = new DateTimeOffset(DateTime.Now, TimeZoneInfo.Local.GetUtcOffset(DateTime.Now)),
                    UserId = User.Identity.GetUserId()
                };
                db.TicketHistoryEvents.Add(th);
                db.SaveChanges();

                int ticketStatusId = db.TicketStatuses.FirstOrDefault(ts => ts.Name == "Resolved").Id;
                ticket.TicketStatusId = ticketStatusId;

                db.Entry(ticket).Property("TicketStatusId").IsModified = true;
                db.SaveChanges();

                if (!(ticket.AssignedToId == null || ticket.AssignedToId == "" || ticket.AssignedToId == "N/A"))
                {
                    //email notification
                    try
                    {
                        var from = "ReSolve Bugtracker<kjferretti@gmail.com>";
                        var to = db.Users.Find(ticket.AssignedToId).Email;
                        var email = new MailMessage(from, to)
                        {
                            Subject = "Ticket has been resolved",
                            Body = $"A ticket you are assigned to (<strong>{ticket.Title}</strong>) has been set to resolved by {currentUser.FirstName} {currentUser.LastName}.", //"Ticket Details can be found <a href='/Tickets/Details/{ticket.Id}'>here</a>.",
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
            }

            return RedirectToAction("Index");
        }

        // GET: Tickets/Unresolve/5
        [Authorize(Roles = "Administrator")]
        public ActionResult Unresolve(int? id)
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

        // POST: Tickets/Unresolve/5
        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Unresolve(int id)
        {
            Ticket ticket = db.Tickets.Find(id);
            var currentUser = db.Users.Find(User.Identity.GetUserId());

            if (ticket.TicketStatus.Name == "Resolved")
            {
                if (!(ticket.AssignedToId == null || ticket.AssignedToId == "" || ticket.AssignedToId == "N/A"))
                {
                    TicketHistoryEvent th = new TicketHistoryEvent
                    {
                        TicketId = ticket.Id,
                        Property = "Status",
                        NewValue = "Assigned",
                        OldValue = db.TicketStatuses.Find(ticket.TicketStatusId).Name,
                        ChangedDate = new DateTimeOffset(DateTime.Now, TimeZoneInfo.Local.GetUtcOffset(DateTime.Now)),
                        UserId = User.Identity.GetUserId()
                    };
                    db.TicketHistoryEvents.Add(th);
                    db.SaveChanges();

                    int ticketStatusId = db.TicketStatuses.FirstOrDefault(ts => ts.Name == "Assigned").Id;
                    ticket.TicketStatusId = ticketStatusId;

                    db.Entry(ticket).Property("TicketStatusId").IsModified = true;
                    db.SaveChanges();

                    //email notification
                    try
                    {
                        var from = "ReSolve Bugtracker<kjferretti@gmail.com>";
                        var to = db.Users.Find(ticket.AssignedToId).Email;
                        var email = new MailMessage(from, to)
                        {
                            Subject = "Ticket has been reactivated",
                            Body = $"A ticket you are assigned to (<strong>{ticket.Title}</strong>) has been reactivated by {currentUser.FirstName} {currentUser.LastName}. Ticket Details can be found <a href='http://kferretti-bugtracker.azurewebsites.net/Tickets/Details/{ticket.Id}'>here</a>.",
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

                if (ticket.AssignedToId == null || ticket.AssignedToId == "" || ticket.AssignedToId == "N/A")
                {
                    TicketHistoryEvent th = new TicketHistoryEvent
                    {
                        TicketId = ticket.Id,
                        Property = "Status",
                        NewValue = "Unassigned",
                        OldValue = db.TicketStatuses.Find(ticket.TicketStatusId).Name,
                        ChangedDate = new DateTimeOffset(DateTime.Now, TimeZoneInfo.Local.GetUtcOffset(DateTime.Now)),
                        UserId = User.Identity.GetUserId()
                    };
                    db.TicketHistoryEvents.Add(th);
                    db.SaveChanges();

                    int ticketStatusId = db.TicketStatuses.FirstOrDefault(ts => ts.Name == "Unassigned").Id;
                    ticket.TicketStatusId = ticketStatusId;

                    db.Entry(ticket).Property("TicketStatusId").IsModified = true;
                    db.SaveChanges();
                }
            }

            return RedirectToAction("Index");
        }

        // GET: Tickets/Resolved
        [Authorize(Roles = "Administrator")]
        public ActionResult Resolved()
        {
            List<Ticket> tickets = db.Tickets.Where(t => t.TicketStatus.Name == "Resolved").ToList();
            return View(tickets);
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
