using sp2023_mis421_mockinterviews.Models.MockInterviewDb;

namespace sp2023_mis421_mockinterviews.Models.ViewModels
{
    public class VolunteerEventsViewModel
    {
        public List<VolunteerEvent> VolunteerEvent { get; set; }
        public List<Timeslot> Timeslots { get; set; }
    }
}
