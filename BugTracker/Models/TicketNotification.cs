using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace BugTracker.Models
{
    public class TicketNotification
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string UserId { get; set; }
        public string NotificationType { get; set; }

        public TicketNotification(int tId, string uId, string nType)
        {
            this.TicketId = tId;
            this.UserId = uId;
            this.NotificationType = nType;
        }

        public void AddTicketNotification()
        {
            var db = new ApplicationDbContext();
            db.TicketNotifications.Add(this);
            db.SaveChanges();
        }

        //Email a developer a notification
        public async Task SendNotificationEmail()
        {
            var db = new ApplicationDbContext();
            var callbackUrl = "http://aarrigucci-bugtracker.azurewebsites.net/Tickets/Details/" + TicketId.ToString();
            ApplicationUser user = db.Users.Find(UserId);
            var ticket = db.Tickets.Find(TicketId);
            var es = new EmailService();
            string body = "";
            string subject = "";

            switch (NotificationType)
            {
                case "Assign":      subject = "Ticket assigned - " + ticket.Title;
                                    body = "You have been assigned a new ticket. Click <a href=\"" + callbackUrl + "\">here</a> to view ticket details.";
                                    break;
                case "Reassign":    subject = "Ticket reassigned - " + ticket.Title;
                                    body = "The ticket \""+ ticket.Title + "\" has been reassigned. View your updated tickets list <a href=\"http://aarrigucci-bugtracker.azurewebsites.net/Tickets\">here</a>.";
                                    break;
                case "Edited":      subject = "Ticket edited - " + ticket.Title;
                                    body = "The ticket \"" + ticket.Title + "\" was edited. Click <a href=\"" + callbackUrl + "\">here</a> to view the updated ticket details.";
                                    break;
                case "Attachment":  subject = "Attachment added - " + ticket.Title;
                                    body = "An attachment was add to the ticket \"" + ticket.Title + ".\" Click <a href=\"" + callbackUrl + "\">here</a> to view the attachment and other ticket details.";
                                    break;
            }
            es.SendAsync(new IdentityMessage
            {
                Destination = user.Email,
                Subject = subject,
                Body = body
            });
            //case: assign
            //case: reassign
            //case: edit
            //case: comment
            //case: attachement
        }
    }
}