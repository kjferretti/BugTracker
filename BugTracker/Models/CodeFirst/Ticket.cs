using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace BugTracker.Models.CodeFirst
{
    public class Ticket
    {
        public Ticket()
        {
            TicketHistoryEvents = new HashSet<TicketHistoryEvent>();
            TicketAttachments = new HashSet<TicketAttachment>();
            TicketComments = new HashSet<TicketComment>();
            TicketNotifications = new HashSet<TicketNotification>();
        }

        public int Id { get; set; }
        //title property is the id string 
        public string Title { get; set; }
        public string Description { get; set; }
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        public DateTimeOffset Created { get; set; }
        [DisplayFormat(DataFormatString = "{0:MM/dd/yyyy}")]
        public DateTimeOffset? Updated { get; set; }

        public int ProjectId { get; set; }
        public virtual Project Project { get; set; }
        public int TicketTypeId { get; set; }
        public virtual TicketType TicketType { get; set; }
        public int TicketPriorityId { get; set; }
        public virtual TicketPriority TicketPriority { get; set; }
        public int TicketStatusId { get; set; }
        public virtual TicketStatus TicketStatus { get; set; }
        public string SubmitterId { get; set; }
        public virtual ApplicationUser Submitter { get; set; }
        public string AssignedToId { get; set; }
        public virtual ApplicationUser AssignedTo { get; set; }

        public virtual ICollection<TicketHistoryEvent> TicketHistoryEvents { get; set; }
        public virtual ICollection<TicketAttachment> TicketAttachments { get; set; }
        public virtual ICollection<TicketComment> TicketComments { get; set; }
        public virtual ICollection<TicketNotification> TicketNotifications { get; set; }
    }
}