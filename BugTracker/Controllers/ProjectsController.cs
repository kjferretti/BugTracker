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
using Microsoft.AspNet.Identity;

namespace BugTracker.Controllers
{
    [Authorize (Roles = "Administrator, Project Manager, Developer, Submitter")]
    public class ProjectsController : Controller
    {
        //if a project manager is assigned to a project that he is not in charge of, he must have the developer role
        //so that he has permissions to fall back on since he doenst have project manager permissions for that project.
        private ApplicationDbContext db = new ApplicationDbContext();
        private ProjectAssignManager PAManager = new ProjectAssignManager();
        UserRolesManager URManager = new UserRolesManager();

        // GET: Projects/Index
        [Authorize(Roles = "Administrator,Project Manager,Developer,Submitter")]
        public ActionResult Index()
        {
            string userId = User.Identity.GetUserId();
            var user = db.Users.Find(userId);
            DisplayProjectsViewModel model = new DisplayProjectsViewModel();

            //see all projects for admin and PM, only assigned projects for dev and submitter
            if (URManager.UserIsInRole(userId, "Administrator"))
            {
                model.AllProjects = db.Projects.Where(p => !(p.Archived ?? false)).ToList();
                model.AssignedProjects = user.Projects.Where(p => !(p.Archived ?? false)).ToList();
                return View(model);
            }

            else if (URManager.UserIsInRole(userId, "Project Manager"))
            {
                model.AllProjects = db.Projects.Where(p => !(p.Archived ?? false)).ToList();
                model.AssignedProjects = user.Projects.Where(p => !(p.Archived ?? false)).ToList();
                return View(model);
            }

            else if (URManager.UserIsInRole(userId, "Developer"))
            {
                model.AssignedProjects = user.Projects.Where(p => !(p.Archived ?? false)).ToList();
                return View(model);
            }

            else if (URManager.UserIsInRole(userId, "Submitter"))
            {
                model.AssignedProjects = user.Projects.Where(p => !(p.Archived ?? false)).ToList();
                return View(model);
            }
            else
            {
                return View();
            }
        }

        // GET: Projects/Assign/5
        [Authorize(Roles = "Administrator, Project Manager")]
        public ActionResult Assign(int? projectId)
        {
            if (projectId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Project project = db.Projects.Find(projectId);
            if (project == null)
            {
                return HttpNotFound();
            }
            var userId = User.Identity.GetUserId();
            if(!URManager.UserIsInRole(userId, "Administrator"))
            {
                if(userId != project.InChargeOfId)
                {
                    return RedirectToAction("Login", "Account");
                }
            }

            //archive handling
            if (project.Archived ?? false)
            {
                ViewBag.ErrorMessage = "";
                return RedirectToAction("Login", "Account");
            }

            ProjectAssignViewModel model = new ProjectAssignViewModel();
            model.projectId = projectId;
            model.UnassignedUserList = db.Users.Where(u => !u.Projects.Select(p => p.Id).Contains(project.Id)).ToList();

            return View(model);
        }

        // POST: Projects/Assign/5
        [Authorize(Roles = "Administrator, Project Manager")]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult Assign([Bind(Include = "Users,projectId")] ProjectAssignViewModel model)
        {
            Project project = db.Projects.Find(model.projectId);
            if (ModelState.IsValid)
            {
                foreach (var user in model.Users)
                {
                    if (!URManager.UserIsInRole(user, "Guest"))
                    {
                        if (!project.Users.Select(u => u.Id).Contains(user))
                        {
                            PAManager.AddUserToProject(user, project.Id);
                        }
                    }
                }

                ViewBag.Message = "Project Assignments have been saved";
                return RedirectToAction("Details", new { id = model.projectId });
            }
            model = new ProjectAssignViewModel();
            model.projectId = project.Id;
            model.UnassignedUserList = db.Users.Where(u => !u.Projects.Select(p => p.Id).Contains(project.Id)).ToList();

            return View(model);
        }

        // POST: Projects/Unassign/5
        [Authorize(Roles = "Administrator, Project Manager")]
        public ActionResult Unassign(int? projectId, string userId)
        {
            if (projectId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Project project = db.Projects.Find(projectId);
            if (project == null)
            {
                return HttpNotFound();
            }
            if (userId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ApplicationUser user = db.Users.Find(userId);
            if (user == null)
            {
                return HttpNotFound();
            }

            string permissionUserId = User.Identity.GetUserId();
            if (!URManager.UserIsInRole(permissionUserId, "Administrator"))
            {
                if (permissionUserId != project.InChargeOfId)
                {
                    return RedirectToAction("Login", "Account");
                }
            }

            //archive handling
            if (project.Archived ?? false)
            {
                ViewBag.ErrorMessage = "";
                return RedirectToAction("Login", "Account");
            }

            if (project.Users.Select(u => u.Id).Contains(userId))
            {
                PAManager.RemoveUserFromProject(user.Id, project.Id);
            }

            return RedirectToAction("Details", new { id = projectId });
        }

        // GET: Projects/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Project project = db.Projects.Find(id);
            if (project == null)
            {
                return HttpNotFound();
            }

            string userId = User.Identity.GetUserId();
            if (!(URManager.UserIsInRole(userId, "Administrator") || URManager.UserIsInRole(userId, "Project Manager")))
            {
                if (!project.Users.Select(u => u.Id).Contains(userId))
                {
                    return RedirectToAction("Login", "Account");
                }
            }

            //archive handling
            if ((project.Archived ?? false) && !URManager.UserIsInRole(userId, "Administrator"))
            {
                ViewBag.ErrorMessage = "";
                return RedirectToAction("Login", "Account");
            }

            return View(project);
        }

        // GET: Projects/Create
        [Authorize(Roles ="Administrator,Project Manager")]
        public ActionResult Create()
        {
            var userId = User.Identity.GetUserId();
            if (URManager.UserIsInRole(userId, "Administrator"))
            {
                ViewBag.InChargeOfId = new SelectList((from u in URManager.UsersInRole("Project Manager") select new { Id = u.Id, FullName = u.FirstName + " " + u.LastName }), "Id", "FullName");
            }
            return View();
        }

        // POST: Projects/Create
        [Authorize(Roles = "Administrator, Project Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Name,InChargeOfId")] Project project)
        {
            var userId = User.Identity.GetUserId();
            if (ModelState.IsValid && !(URManager.UserIsInRole(userId, "Administrator") && (project.InChargeOfId == null)))
            {
                if(URManager.UserIsInRole(userId, "Project Manager"))
                {
                    project.InChargeOfId = userId;
                }

                var user = db.Users.Find(project.InChargeOfId);
                string firstName = user.FirstName;
                string lastName = user.LastName;
                project.InChargeOfName = firstName + " " + lastName;
                db.Projects.Add(project);
                db.SaveChanges();

                if (!project.Users.Select(u => u.Id).Contains(project.InChargeOfId))
                {
                    PAManager.AddUserToProject(project.InChargeOfId, project.Id);
                }

                return RedirectToAction("Details", new { id = project.Id });
            }

            if (URManager.UserIsInRole(userId, "Administrator"))
            {
                ViewBag.InChargeOfId = new SelectList((from u in URManager.UsersInRole("Project Manager") select new { Id = u.Id, FullName = u.FirstName + " " + u.LastName }), "Id", "FullName");
            }
            return View(project);
        }

        // GET: Projects/Edit/5
        [Authorize(Roles = "Administrator,Project Manager")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Project project = db.Projects.Find(id);
            if (project == null)
            {
                return HttpNotFound();
            }

            string permissionUserId = User.Identity.GetUserId();
            if (!URManager.UserIsInRole(permissionUserId, "Administrator"))
            {
                if (permissionUserId != project.InChargeOfId)
                {
                    return RedirectToAction("Login", "Account");
                }
            }

            //archive handling
            if(project.Archived ?? false)
            {
                ViewBag.ErrorMessage = "";
                return RedirectToAction("Login", "Account");
            }

            //project manager selectlist
            if (URManager.UserIsInRole(permissionUserId, "Administrator"))
            {
                ViewBag.InChargeOfId = new SelectList((from u in URManager.UsersInRole("Project Manager") select new { Id = u.Id, FullName = u.FirstName + " " + u.LastName }), "Id", "FullName", project.InChargeOfId);
            }

            return View(project);
        }

        // POST: Projects/Edit/5
        [Authorize(Roles = "Administrator, Project Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Name,InChargeOfId")] Project project)
        {
            string permissionUserId = User.Identity.GetUserId();
            if (ModelState.IsValid)
            {
                if (URManager.UserIsInRole(permissionUserId, "Project Manager"))
                {
                    project.InChargeOfId = permissionUserId;
                }

                if(!project.Users.Select(u => u.Id).Contains(project.InChargeOfId))
                {
                    PAManager.AddUserToProject(project.InChargeOfId, project.Id);
                }

                ApplicationUser user = db.Users.Find(project.InChargeOfId);
                string firstName = user.FirstName;
                string lastName = user.LastName;
                project.InChargeOfName = firstName + " " + lastName;

                db.Entry(project).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Details", new { id = project.Id });
            }

            if (URManager.UserIsInRole(permissionUserId, "Administrator"))
            {
                ViewBag.InChargeOfId = new SelectList((from u in URManager.UsersInRole("Project Manager") select new { Id = u.Id, FullName = u.FirstName + " " + u.LastName }), "Id", "FullName");
            }

            return View(project);
        }

        // GET: Projects/Archive/5
        [Authorize(Roles = "Administrator, Project Manager")]
        public ActionResult Archive(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Project project = db.Projects.Find(id);
            if (project == null)
            {
                return HttpNotFound();
            }

            string permissionUserId = User.Identity.GetUserId();
            if (!URManager.UserIsInRole(permissionUserId, "Administrator"))
            {
                if (permissionUserId != project.InChargeOfId)
                {
                    return RedirectToAction("Login", "Account");
                }
            }

            return View(project);
        }

        // POST: Projects/Archive/5
        [Authorize(Roles = "Administrator, Project Manager")]
        [HttpPost]
        public ActionResult Archive(int id)
        {
            Project project = db.Projects.Find(id);
            project.Archived = true;
            db.Entry(project).Property("Archived").IsModified = true;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // GET: Projects/Unarchive/5
        [Authorize(Roles = "Administrator")]
        public ActionResult Unarchive(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Project project = db.Projects.Find(id);
            if (project == null)
            {
                return HttpNotFound();
            }

            string permissionUserId = User.Identity.GetUserId();
            if (!URManager.UserIsInRole(permissionUserId, "Administrator"))
            {
                if (permissionUserId != project.InChargeOfId)
                {
                    return RedirectToAction("Login", "Account");
                }
            }

            return View(project);
        }

        // POST: Projects/Unarchive/5
        [HttpPost]
        public ActionResult Unarchive(int id)
        {
            Project project = db.Projects.Find(id);
            project.Archived = false;
            //db.Projects.Remove(project);
            db.Entry(project).Property("Archived").IsModified = true;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // GET: Projects/Archived
        [Authorize (Roles="Administrator")]
        public ActionResult Archived()
        {
            List<Project> projects = db.Projects.Where(p => p.Archived ?? false).ToList();
            return View(projects);
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
