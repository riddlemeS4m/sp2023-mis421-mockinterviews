using System.ComponentModel.DataAnnotations.Schema;

namespace sp2023_mis421_mockinterviews.Models.MockInterviewDb
{
    public class LocationInterviewer
    {
        //could just have a nullable field on each interviewer, although the disadvantage of that is
        //then the same location couldn't be assigned to multiple interviewers throughout the day

        //LT-I like the setup of having table of locations we can update
        public int Id { get; set; }
        [ForeignKey("Interviewer")]
        public int InterviewerId { get; set; }
        public Interviewer Interviewer { get; set; }
        [ForeignKey("Location")]
        public int LocationId { get; set; }
        public Location Location { get; set; }
    }
}
