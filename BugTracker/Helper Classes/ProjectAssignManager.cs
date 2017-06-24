using BugTracker.Models;
using BugTracker.Models.CodeFirst;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BugTracker.Helper_Classes
{
    public class ProjectAssignManager
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public bool IsUserOnProject(string userId, int projectId)
        {
            var project = db.Projects.Find(projectId);
            var flag = project.Users.Any(u => u.Id == userId);
            return flag;
        }

        public void AddUserToProject(string userId, int projectId)
        {
            Project project = db.Projects.Find(projectId);
            ApplicationUser user = db.Users.Find(userId);
            project.Users.Add(user);
            db.SaveChanges();
        }

        public void RemoveUserFromProject(string userId, int projectId)
        {
                Project project = db.Projects.Find(projectId);
                ApplicationUser user = db.Users.Find(userId);
                project.Users.Remove(user);
                db.SaveChanges();
        }

        public List<Project> ListUserProjects(string userId)
        {
            ApplicationUser user = db.Users.Find(userId);
            return user.Projects.ToList();
        }

        public List<ApplicationUser> ListUsersOnProject(int projectId)
        {
            Project project = db.Projects.Find(projectId);
            return project.Users.ToList();
        }

        public List<ApplicationUser> ListUsersNotOnProject(string projectId)
        {
            Project project = db.Projects.Find(projectId);
            var usersOnProject = project.Users.ToList();
            return db.Users.Where(u => !usersOnProject.Contains(u)).ToList();
        }
    }
}