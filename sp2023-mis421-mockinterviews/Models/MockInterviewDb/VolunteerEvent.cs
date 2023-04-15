using System.ComponentModel.DataAnnotations.Schema;

namespace sp2023_mis421_mockinterviews.Models.MockInterviewDb
{
    public class VolunteerEvent
    {
        //is this all the information we'll need for this?

        //LT - yes it should be
        public int Id { get; set; }
        public int StudentId { get; set; }
        public Student Student { get; set; }
        [ForeignKey("Timeslot")]
        public int TimeslotId { get; set; }
        public Timeslot Timeslot { get; set; }
    }
}
