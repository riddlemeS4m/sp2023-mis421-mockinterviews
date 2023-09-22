using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace sp2023_mis421_mockinterviews.Models.MockInterviewDb
{
    public class LocationInterviewer
    {
        //could just have a nullable field on each interviewer, although the disadvantage of that is
        //then the same location couldn't be assigned to multiple interviewers throughout the day

        //LT-I like the setup of having table of locations we can update
        public int Id { get; set; }
        [Display(Name = "Interviewer")]
        public string InterviewerId { get; set; }
        [ForeignKey("Location")]
        [Display(Name = "Location")]
        public int? LocationId { get; set; }
        [ValidateNever]
        public Location? Location { get; set; }
        [ForeignKey("EventDate")]
        [Display(Name = "Event")]
        public int? EventDateId { get; set; }
        [ValidateNever]
        public EventDate? EventDate { get; set; }
        [ValidateNever]
        public string LocationPreference { get; set; }
    }
}
