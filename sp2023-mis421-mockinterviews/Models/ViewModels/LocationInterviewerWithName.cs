using sp2023_mis421_mockinterviews.Models.MockInterviewDb;
using System.ComponentModel.DataAnnotations;

namespace sp2023_mis421_mockinterviews.Models.ViewModels
{
    public class LocationInterviewerWithName
    {
        public LocationInterviewer LocationInterviewer { get; set; }
        [Display(Name = "Interviewer")]
        public string InterviewerName { get; set; }
        public string InterviewerPreference { get; set; }
    }
}
