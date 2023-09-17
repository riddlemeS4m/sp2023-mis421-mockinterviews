using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sp2023_mis421_mockinterviews.Models.MockInterviewDb
{
    public class InterviewEvent
    {
        //probably our most important entity
       
        public int Id { get; set; }
        public string StudentId { get; set; }
        [ForeignKey("Location")]
        public int? LocationId { get; set; }
        public Location? Location { get; set; }
        [ForeignKey("Timeslot")]
        public int TimeslotId { get; set; }
        [ValidateNever]
        public Timeslot Timeslot { get; set; }
        [Display(Name = "Interview Type")]
        public string? InterviewType { get; set; }
        public string Status { get; set; }
        [Display(Name = "Rating")]
        public string? InterviewerRating { get; set; }
        [Display(Name = "Interviewer Feedback")]
        public string? InterviewerFeedback { get; set; }
        [Display(Name = "Process Feedback")]
        public string? ProcessFeedback { get; set; }
        [ForeignKey("SignupInterviewerTimeslot")]
        public int? SignupInterviewerTimeslotId { get; set; }
        [ValidateNever]
        public SignupInterviewerTimeslot? SignupInterviewerTimeslot { get; set; }
		public override string ToString()
		{
			return $"{Timeslot.Time:h\\:mm tt} on {Timeslot.EventDate.Date:M/dd/yyyy} <br>";
		}
	}
}
