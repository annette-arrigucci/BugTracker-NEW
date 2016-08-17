using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BugTracker.Models
{
    public class TicketEditViewModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Title is required")]
        [StringLength(100)]
        public string Title { get; set; }
        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }
        public DateTimeOffset Created { get; set; }
        public DateTimeOffset? Updated { get; set; }
        //public int ProjectId { get; set; }
        //public int TicketTypeId { get; set; }
        //public int TicketPriorityId { get; set; }
        //public int TicketStatusId { get; set; }
        //public string OwnerUserId { get; set; }
        [Display(Name = "Assigned to")]
        public string AssignedToUserName { get; set; }

        [Display(Name = "Project")]
        public SelectList Projects { get; set; }
        [Display(Name = "Type")]
        public SelectList TicketTypes { get; set; }
        [Display(Name = "Priority")]
        public SelectList TicketPriorities { get; set; }
        [Display(Name = "Status")]
        public SelectList TicketStatuses { get; set; }
        //public SelectList TicketStatuses { get; set; }

        [Required(ErrorMessage = "Please select a project")]
        public int SelectedProject { get; set; }
        [Required(ErrorMessage = "Please select a type")]
        public int SelectedType { get; set; }
        [Required(ErrorMessage = "Please select a priority")]
        public int SelectedPriority { get; set; }
        [Required(ErrorMessage = "Please select a status")]
        public int SelectedStatus { get; set; }
    }
}