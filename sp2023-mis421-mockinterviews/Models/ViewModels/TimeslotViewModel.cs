using sp2023_mis421_mockinterviews.Models.MockInterviewDb;

namespace sp2023_mis421_mockinterviews.Models.ViewModels
{
    public class TimeslotViewModel
    {
        public List<ParticipantCountViewModel> Timeslots { get; set; }
        public List<Event> EventDates { get; set; }
    }
}
