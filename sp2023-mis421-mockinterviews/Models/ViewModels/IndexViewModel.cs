using sp2023_mis421_mockinterviews.Models.MockInterviewDb;

namespace sp2023_mis421_mockinterviews.Models.ViewModels
{
    public class IndexViewModel
    {
        public List<VolunteerEventViewModel> VolunteerEventViewModels { get; set; }
        public List<SignupInterviewerTimeslot> SignupInterviewerTimeslots { get;  set; }
    }
}
