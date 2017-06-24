using BugTracker.Models.CodeFirst;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BugTracker.Models
{
    public class DisplayProjectsViewModel
    {
        public DisplayProjectsViewModel()
        {
            AssignedProjects = new List<Project>();
            AllProjects = new List<Project>();
        }
        public ICollection<Project> AssignedProjects { get; set; }
        public ICollection<Project> AllProjects { get; set; }
    }

    public class ProjectAssignViewModel
    {
        public string[] Users { get; set; }
        public int? projectId { get; set; }

        //select2
        public List<ApplicationUser> UnassignedUserList { get; set; }
    }
}