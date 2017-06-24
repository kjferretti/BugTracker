using BugTracker.Models.CodeFirst;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BugTracker.Models
{
    public class DashboardViewModel
    {
        public DashboardViewModel()
        {
            Projects = new List<Project>();
            Tickets = new List<Ticket>();
            TicketComments = new List<TicketComment>();
            TicketAttachments = new List<TicketAttachment>();
        }

        public IList<Project> Projects { get; set; }
        public IList<Ticket> Tickets { get; set; }
        public IList<TicketComment> TicketComments { get; set; }
        public IList<TicketAttachment> TicketAttachments { get; set; }
    }
}