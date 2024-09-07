using sp2023_mis421_mockinterviews.Models.MockInterviewDb;
using System.ComponentModel.DataAnnotations;

namespace sp2023_mis421_mockinterviews.Models.ViewModels
{
    public class InterviewEventViewModel
    {
        public Interview InterviewEvent { get; set; }
        [Display(Name = "Student Name")]
        public string StudentName { get; set; }
        [Display(Name = "Interviewer Name")]
        public string InterviewerName { get; set; }
    }
}
