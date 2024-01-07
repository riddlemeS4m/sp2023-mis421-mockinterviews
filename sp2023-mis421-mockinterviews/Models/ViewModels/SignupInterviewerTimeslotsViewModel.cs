using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;

namespace sp2023_mis421_mockinterviews.Models.ViewModels
{
    public class SignupInterviewerTimeslotsViewModel
    {
        public List<Timeslot> Timeslots { get; set; }
        public List<EventDate> EventDates { get; set; }
        public SignupInterviewer SignupInterviewer { get; set; }
        public Dictionary<int, bool> EventDateDictionary { get; set; }
        public List<SelectListItem> Interviewers { get; set; }
        public string InterviewerId { get; set; }
        public int[] SelectedEventIds { get; set; }
        public bool SignedUp { get; set; }
    }
}
