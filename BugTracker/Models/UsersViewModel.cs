using BugTracker.Models.CodeFirst;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BugTracker.Models
{
    public class UsersViewModel
    {
        public UsersViewModel()
        {
            TicketsForAssigning = new List<IList<Ticket>>();
            ProjectsForAssigning = new List<IList<Project>>();
            UserRoles = new List<IList<IdentityRole>>();
            NonUserRoles = new List<IList<IdentityRole>>();
            Users = new List<ApplicationUser>();
        }

        public ICollection<ApplicationUser> Users { get; set; }
        public string[] Roles { get; set; }
        public string[] Projects { get; set; }
        public string[] Tickets { get; set; }
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public IList<IList<IdentityRole>> UserRoles { get; set; }
        public IList<IList<IdentityRole>> NonUserRoles { get; set; }
        public IList<IList<Project>> ProjectsForAssigning { get; set; }
        public IList<IList<Ticket>> TicketsForAssigning { get; set; }
    }
}