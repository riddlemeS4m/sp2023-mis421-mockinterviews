using Microsoft.AspNetCore.Mvc.Rendering;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;

namespace sp2023_mis421_mockinterviews.Models.ViewModels
{
    public class LocationInterviewerCreateViewModel
    {
        public LocationInterviewer LocationInterviewer { get; set; }
        public List<SelectListItem> InterviewerNames { get; set; }
        public List<SelectListItem> Locations { get; set; }
    }
}
