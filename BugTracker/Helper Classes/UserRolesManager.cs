using BugTracker.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;

namespace BugTracker.Helper_Classes
{
    public class UserRolesManager
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private UserManager<ApplicationUser> userManager;
        private RoleManager<IdentityRole> roleManager;

        //
        public UserRolesManager()
        {
            userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(db));
        }

        public bool UserIsInRole(string userId, string roleName)
        {
            return userManager.IsInRole(userId, roleName);
        }

        public string PriorityRole(string userId)
        {
            UserRolesManager URManager = new UserRolesManager();

            if(URManager.UserIsInRole(userId, "Administrator"))
            {
                return "Administrator";
            }
            if (URManager.UserIsInRole(userId, "Project Manager"))
            {
                return "Project Manager";
            }
            if (URManager.UserIsInRole(userId, "Developer"))
            {
                return "Developer";
            }
            if (URManager.UserIsInRole(userId, "Submitter"))
            {
                return "Submitter";
            }
            if (URManager.UserIsInRole(userId, "Guest"))
            {
                return "Guest";
            }

            return "";
        }

        public IList<string> ListUserRoles(string userId)
        {
            return userManager.GetRoles(userId);
        }

        public IList<string> ListRolesUserIsNotIn(string userId)
        {
            var roles = userManager.GetRoles(userId);
            var nonRoles = db.Roles.Where(r => !roles.Contains(r.Name)).Select(r => r.Name).ToList();
            return nonRoles;
        }

        public void AddUserToRole(string userId, string roleName)
        {
            userManager.AddToRole(userId, roleName);
            db.SaveChanges();
        }

        public void RemoveUserFromRole(string userId, string roleName)
        {
            userManager.RemoveFromRole(userId, roleName);
            db.SaveChanges();
        }

        public IList<ApplicationUser> UsersInRole(string roleName)
        {
            var userIdList = roleManager.FindByName(roleName).Users.Select(r => r.UserId);
            return userManager.Users.Where(u => userIdList.Contains(u.Id)).ToList();
        }

        public IList<ApplicationUser> UsersNotInRole(string roleName)
        {
            var userIdList = Roles.GetUsersInRole(roleName);
            return userManager.Users.Where(u => !userIdList.Contains(u.Id)).ToList();
        }
    }
}