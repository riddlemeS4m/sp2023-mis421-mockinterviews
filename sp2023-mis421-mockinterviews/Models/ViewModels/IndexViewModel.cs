using sp2023_mis421_mockinterviews.Models.MockInterviewDb;

namespace sp2023_mis421_mockinterviews.Models.ViewModels
{
    public class IndexViewModel
    {
        public List<InterviewEventViewModel> StudentScheduledInterviews { get; set; }
        public List<VolunteerEventViewModel> VolunteerEventViewModels { get; set; }
        public List<InterviewEventViewModel> InterviewerScheduledInterviews { get;  set; }
        public List<SignupInterviewerTimeslot> SignupInterviewerTimeslots { get; set; }
        public List<TimeRangeViewModel> TimeRangeViewModels { get; set; }
        public List<TimeRangeViewModel> InterviewerRangeViewModels { get; set; }
        public string Name { get; set; }
    }
}
