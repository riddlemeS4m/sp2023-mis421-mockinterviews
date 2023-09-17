using System.ComponentModel.DataAnnotations;

namespace sp2023_mis421_mockinterviews.Models.ViewModels
{
    public class AllocationReportViewModel
    {
        [Display(Name = "Top 10 Underserved Timeslots")]
        public List<ParticipantCountViewModel> Top10Underserved { get; set; }
        [Display(Name = "Top 10 Available Timeslots")]
        public List<ParticipantCountViewModel> Top10Available { get; set; }
    }
}
