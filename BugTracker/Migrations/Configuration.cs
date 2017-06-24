using BugTracker.Models;
using BugTracker.Models.CodeFirst;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Web;

namespace BugTracker.Migrations
{
    public class Configuration : DbMigrationsConfiguration<ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
        }

        protected override void Seed(ApplicationDbContext db)
        {
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(db));

            if (!db.Roles.Any(r => r.Name == "Administrator"))
            {
                roleManager.Create(new IdentityRole { Name = "Administrator" });
            }

            if (!db.Roles.Any(r => r.Name == "Project Manager"))
            {
                roleManager.Create(new IdentityRole { Name = "Project Manager" });
            }

            if (!db.Roles.Any(r => r.Name == "Developer"))
            {
                roleManager.Create(new IdentityRole { Name = "Developer" });
            }

            if (!db.Roles.Any(r => r.Name == "Submitter"))
            {
                roleManager.Create(new IdentityRole { Name = "Submitter" });
            }

            if (!db.Roles.Any(r => r.Name == "Guest"))
            {
                roleManager.Create(new IdentityRole { Name = "Guest" });
            }

            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));

            if (!db.Users.Any(u => u.Email == "kjferretti@gmail.com"))
            {
                userManager.Create(new ApplicationUser
                {
                    UserName = "kjferretti@gmail.com",
                    Email = "kjferretti@gmail.com",
                    FirstName = "Kevin",
                    LastName = "Ferretti",
                }, "password");
            }

            var userId = userManager.FindByEmail("kjferretti@gmail.com").Id;
            userManager.AddToRole(userId, "Administrator");
            userManager.AddToRole(userId, "Project Manager");
            userManager.AddToRole(userId, "Developer");
            userManager.AddToRole(userId, "Submitter");

            if (!db.Users.Any(u => u.Email == "JohnPatton@testuser.com"))
            {
                userManager.Create(new ApplicationUser
                {
                    UserName = "JohnPatton@testuser.com",
                    Email = "JohnPatton@testuser.com",
                    FirstName = "John",
                    LastName = "Patton",
                }, "password");
            }

            userId = userManager.FindByEmail("JohnPatton@testuser.com").Id;
            userManager.AddToRole(userId, "Administrator");
            userManager.AddToRole(userId, "Submitter");

            if (!db.Users.Any(u => u.Email == "GeorgeEaston@testuser.com"))
            {
                userManager.Create(new ApplicationUser
                {
                    UserName = "GeorgeEaston@testuser.com",
                    Email = "GeorgeEaston@testuser.com",
                    FirstName = "George",
                    LastName = "Easton",
                }, "password");
            }

            userId = userManager.FindByEmail("GeorgeEaston@testuser.com").Id;
            userManager.AddToRole(userId, "Project Manager");
            userManager.AddToRole(userId, "Developer");
            userManager.AddToRole(userId, "Submitter");

            if (!db.Users.Any(u => u.Email == "JosephPeeples@testuser.com"))
            {
                userManager.Create(new ApplicationUser
                {
                    UserName = "JosephPeeples@testuser.com",
                    Email = "JosephPeeples@testuser.com",
                    FirstName = "Joseph",
                    LastName = "Peeples",
                }, "password");
            }

            userId = userManager.FindByEmail("JosephPeeples@testuser.com").Id;
            userManager.AddToRole(userId, "Developer");
            userManager.AddToRole(userId, "Submitter");

            if (!db.Users.Any(u => u.Email == "MichaelMcKenzie@testuser.com"))
            {
                userManager.Create(new ApplicationUser
                {
                    UserName = "MichaelMcKenzie@testuser.com",
                    Email = "MichaelMcKenzie@testuser.com",
                    FirstName = "Michael",
                    LastName = "McKenzie",
                }, "password");
            }

            userId = userManager.FindByEmail("MichaelMcKenzie@testuser.com").Id;
            userManager.AddToRole(userId, "Submitter");

            if (!db.Users.Any(u => u.Email == "guest@administrator.com"))
            {
                userManager.Create(new ApplicationUser
                {
                    UserName = "guest@administrator.com",
                    Email = "guest@administrator.com",
                    FirstName = "Guest Administrator",
                    LastName = "",
                }, "password");
            }

            userId = userManager.FindByEmail("guest@administrator.com").Id;
            userManager.AddToRole(userId, "Administrator");
            userManager.AddToRole(userId, "Submitter");

            if (!db.Users.Any(u => u.Email == "guest@projectmanager.com"))
            {
                userManager.Create(new ApplicationUser
                {
                    UserName = "guest@projectmanager.com",
                    Email = "guest@projectmanager.com",
                    FirstName = "Guest Project Manager",
                    LastName = "",
                }, "password");
            }

            userId = userManager.FindByEmail("guest@projectmanager.com").Id;
            userManager.AddToRole(userId, "Project Manager");
            userManager.AddToRole(userId, "Developer");
            userManager.AddToRole(userId, "Submitter");

            if (!db.Users.Any(u => u.Email == "guest@developer.com"))
            {
                userManager.Create(new ApplicationUser
                {
                    UserName = "guest@developer.com",
                    Email = "guest@developer.com",
                    FirstName = "Guest Developer",
                    LastName = "",
                }, "password");
            }

            userId = userManager.FindByEmail("guest@developer.com").Id;
            userManager.AddToRole(userId, "Developer");
            userManager.AddToRole(userId, "Submitter");

            if (!db.Users.Any(u => u.Email == "guest@submitter.com"))
            {
                userManager.Create(new ApplicationUser
                {
                    UserName = "guest@submitter.com",
                    Email = "guest@submitter.com",
                    FirstName = "Guest Submitter",
                    LastName = "",
                }, "password");
            }

            userId = userManager.FindByEmail("guest@submitter.com").Id;
            userManager.AddToRole(userId, "Submitter");

            if (!db.TicketTypes.Any())
            {
                db.TicketTypes.AddOrUpdate(x => x.Id, new TicketType()
                {
                    Name = "Bug",
                },
                new TicketType()
                {
                    Name = "Issue",
                });
            }
            if (!db.TicketPriorities.Any())
            {
                db.TicketPriorities.AddOrUpdate(x => x.Id, new TicketPriority ()
                {
                    Name = "High",
                },
                new TicketPriority()
                {
                    Name = "Medium",
                },
                new TicketPriority()
                {
                    Name = "Low",
                });
            }
            if (!db.TicketStatuses.Any())
            {
                db.TicketStatuses.AddOrUpdate(x => x.Id, new TicketStatus()
                {
                    Name = "Unassigned",
                },
                new TicketStatus()
                {
                    Name = "Assigned",
                },
                new TicketStatus()
                {
                    Name = "Resolved",
                });
            }
        }
    }
}