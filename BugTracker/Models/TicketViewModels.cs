using BugTracker.Models.CodeFirst;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BugTracker.Models
{
    public class TicketAssignViewModel
    {
        public string UserId { get; set; }
        public int? ticketId { get; set; }
    }

    public class TicketHistoryViewModel
    {
        public Ticket Ticket { get; set; }
        public ApplicationUser CreatedBy { get; set; }
        public string CreatedId { get; set; }  
        public string CreatedDescription { get; set; }
        public string CreatedPriority { get; set; }
        public string CreatedStatus { get; set; }
    }
}