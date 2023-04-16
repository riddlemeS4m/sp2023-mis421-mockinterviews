using sp2023_mis421_mockinterviews.Models.MockInterviewDb;

namespace sp2023_mis421_mockinterviews.Models.ViewModels
{
    public class SignupInterviewerTimeslotsViewModel
    {
        public List<Timeslot> Timeslots { get; set; }
        public SignupInterviewer SignupInterviewer { get; set; }
        public int[] SelectedEventIds { get; set; }
    }
}
