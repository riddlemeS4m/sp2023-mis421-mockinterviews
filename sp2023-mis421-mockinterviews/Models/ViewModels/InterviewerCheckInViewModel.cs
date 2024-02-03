using Microsoft.AspNetCore.Mvc.Rendering;

namespace sp2023_mis421_mockinterviews.Models.ViewModels
{
    public class InterviewerCheckInViewModel
    {
        public string InterviewerId { get; set; }
        public List<SelectListItem> Interviewers { get; set; }
    }
}
