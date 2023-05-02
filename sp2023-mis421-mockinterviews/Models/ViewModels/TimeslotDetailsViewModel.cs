using sp2023_mis421_mockinterviews.Models.MockInterviewDb;

namespace sp2023_mis421_mockinterviews.Models.ViewModels
{
    public class TimeslotDetailsViewModel
    {
        public Timeslot Timeslot { get; set; }
        public List<string>? VolunteerNames { get; set; }
        public List<string>? StudentNames { get; set; }
        public List<string>? InterviewerNames { get; set; }
    }
}
