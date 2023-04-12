namespace sp2023_mis421_mockinterviews.Models.MockInterviewDb
{
    public class Timeslot
    {
        //all static entries
        //do we need isactive field?
        public int Id { get; set; }
        public string Time { get; set; }
        public bool IsActive { get; set; }
        public bool IsVolunteer { get; set; }
        public bool IsInterviewer { get; set; }
        public bool IsStudent { get; set; }
    }
}
