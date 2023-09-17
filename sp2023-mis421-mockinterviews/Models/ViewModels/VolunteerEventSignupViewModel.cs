using sp2023_mis421_mockinterviews.Models.MockInterviewDb;

namespace sp2023_mis421_mockinterviews.Models.ViewModels
{
    public class VolunteerEventSignupViewModel
    {
        //public List<VolunteerEvent> VolunteerEvent { get; set; }
        public List<Timeslot> Timeslots { get; set; }
        public int[] SelectedEventIds1 { get; set; }
        public int[] SelectedEventIds2 { get; set; }
        public bool SignedUp { get; set; }
    }
}
