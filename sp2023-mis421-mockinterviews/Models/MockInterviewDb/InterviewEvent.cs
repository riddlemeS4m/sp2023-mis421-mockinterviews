using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;

namespace sp2023_mis421_mockinterviews.Models.MockInterviewDb
{
    public class InterviewEvent
    {
        //probably our most important entity
        //not 100% sure what fields need to be nullable or not, would help to doublecheck
        //also not 100% sure about the relationship with signupinterviewertimeslot

        //LT - I think status should be not null with a default
        public int Id { get; set; }
        public string StudentId { get; set; }
        [ForeignKey("Location")]
        public int? LocationId { get; set; }
        public Location? Location { get; set; }
        [ForeignKey("Timeslot")]
        public int TimeslotId { get; set; }
        [ValidateNever]
        public Timeslot Timeslot { get; set; }
        public string? InterviewType { get; set; }
        public string Status { get; set; }
        public string? InterviewerRating { get; set; }
        public string? InterviewerFeedback { get; set; }
        public string? ProcessFeedback { get; set; }
        [ForeignKey("SignupInterviewerTimeslot")]
        public int? SignupInterviewerTimeslotId { get; set; }
        [ValidateNever]
        public SignupInterviewerTimeslot? SignupInterviewerTimeslot { get; set; }
		public override string ToString()
		{
			return $"{Timeslot.Time} on {Timeslot.EventDate.Date} <br>";
		}
	}
}
