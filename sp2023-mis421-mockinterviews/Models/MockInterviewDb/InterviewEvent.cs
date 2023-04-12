using System.ComponentModel.DataAnnotations.Schema;

namespace sp2023_mis421_mockinterviews.Models.MockInterviewDb
{
    public class InterviewEvent
    {
        //probably our most important entity
        //not 100% sure what fields need to be nullable or not, would help to doublecheck
        //also not 100% sure about the relationship with signupinterviewertimeslot
        public int Id { get; set; }
        [ForeignKey("Student")]
        public int StudentId { get; set; }
        public Student Student { get; set; }
        [ForeignKey("Location")]
        public int LocationId { get; set; }
        public Location Location { get; set; }
        [ForeignKey("Timeslot")]
        public int TimeslotId { get; set; }
        public Timeslot Timeslot { get; set; }
        public string? InterviewType { get; set; }
        public string? Status { get; set; }
        public string? InterviewerRating { get; set; }
        public string? InterviewerFeedback { get; set; }
        public string? ProcessFeedback { get; set; }
        [ForeignKey("SignupInterviewerTimeslot")]
        public int? SignupInterviewerTimeslotId { get; set; }
        public SignupInterviewerTimeslot? SignupInterviewerTimeslot { get; set; }
    }
}
