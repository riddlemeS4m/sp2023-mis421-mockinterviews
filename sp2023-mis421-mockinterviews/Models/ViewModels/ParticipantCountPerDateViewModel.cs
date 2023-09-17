using sp2023_mis421_mockinterviews.Models.MockInterviewDb;
using System.ComponentModel.DataAnnotations;

namespace sp2023_mis421_mockinterviews.Models.ViewModels
{
    public class ParticipantCountPerDateViewModel
    {
        [Display(Name = "Event Name")]
        public EventDate? EventDate { get; set; }
        [Display(Name = "No. of Students")]
        public int? StudentCount { get; set; }
        [Display(Name = "No. of Interviewers")]
        public int? InterviewerCount { get; set; }
        [Display(Name = "No. of Volunteers")]
        public int? VolunteerCount { get; set; }
    }
}