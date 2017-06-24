using BugTracker.Helper_Classes;
using BugTracker.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace BugTracker.Controllers
{
    public class GuestController : Controller
    {

        private ApplicationDbContext db = new ApplicationDbContext();
        private UserRolesManager URManager = new UserRolesManager();

        // GET: Guest/Index
        [Authorize(Roles = "Guest")]
        public ActionResult Index()
        {
            return View();
        }

        // POST: Guest/GuestAssignment
        [Authorize(Roles = "Guest")]
        public ActionResult GuestAssignment()
        {
            URManager.AddUserToRole(User.Identity.GetUserId(), "Submitter");
            URManager.RemoveUserFromRole(User.Identity.GetUserId(), "Guest");

            return RedirectToAction("NewSubmitterLogin", "Account");
        }
    }
}