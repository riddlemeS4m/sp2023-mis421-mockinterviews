namespace sp2023_mis421_mockinterviews.Models.MockInterviewDb
{
    public class Timeslot
    {
        //all static entries
        //do we need isactive field?
        //yes because we want the dependencies to stay around even after a week of mock interviews is over, that way we can go back in the history and see previous stats
        public int Id { get; set; }
        public string Time { get; set; }
        public bool IsActive { get; set; }
        public bool IsVolunteer { get; set; }
        public bool IsInterviewer { get; set; }
        public bool IsStudent { get; set; }
    }
}
