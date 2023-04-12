using System.ComponentModel.DataAnnotations.Schema;

namespace sp2023_mis421_mockinterviews.Models.MockInterviewDb
{
    public class SignupInterviewerTimeslot
    {
        //new relationship sam had to create
        //since each interviewer can sign up for multiple times, each signup interviewer can be associated with multiple time slots, as well as the other way around
        //many to many relationships require their own entity
        public int Id { get; set; }
        [ForeignKey("SignupInterviewer")]
        public int SignupInterviewerId { get; set; }
        public SignupInterviewer SignupInterviewer { get; set; }
        [ForeignKey("Timeslot")]
        public int TimeslotId { get; set; }
        public Timeslot Timeslot { get; set; }
    }
}
