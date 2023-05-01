using System.ComponentModel.DataAnnotations.Schema;

namespace sp2023_mis421_mockinterviews.Models.MockInterviewDb
{
    public class VolunteerEvent
    {
        //is this all the information we'll need for this?

        //LT - yes it should be
        public int Id { get; set; }
        public string StudentId { get; set; }

        [ForeignKey("Timeslot")]
        public int TimeslotId { get; set; }
        public Timeslot Timeslot { get; set; }
        public override string ToString()
        {
            return $"{Timeslot.Time:h\\:mm tt} on {Timeslot.EventDate.Date:M/dd/yyyy} <br>";
        }
    }
}
