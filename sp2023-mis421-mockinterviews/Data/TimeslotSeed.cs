using sp2023_mis421_mockinterviews.Models.MockInterviewDb;

namespace sp2023_mis421_mockinterviews.Data
{
    public class TimeslotSeed
    {
        public const int MaxSignups = 2;
        public static readonly string[] Times = { "8:00 AM",
            "8:30 AM",
            "9:00 AM",
            "9:30 AM",
            "10:00 AM",
            "10:30 AM",
            "11:00 AM",
            "11:30 AM",
            "12:00 PM",
            "12:30 PM",
            "1:00 PM",
            "1:30 PM",
            "2:00 PM",
            "2:30 PM",
            "3:00 PM",
            "3:30 PM",
            "4:00 PM",
            "4:30 PM",
        };

        public static readonly bool[] Student = { false, false, true, false, true, false, true, false, false, false, true, false, true, false, true, false, false, false };
        public static readonly bool[] Interviewer = { false, false, true, true, true, true, true, true, false, false, true, true, true, true, true, true, false, false };

        public static List<Timeslot> SeedTimeslots(List<EventDate> dates)
        {
            List<Timeslot> timeslots = new List<Timeslot>();

            foreach (EventDate date in dates)
            {
                for (int i = 0; i < Times.Length; i++)
                {
                    Timeslot timeslot = new Timeslot();
                    timeslot.Time = Times[i];
                    timeslot.EventDate = date;
                    timeslot.EventDateId = date.Id;
                    timeslot.IsVolunteer = true;
                    timeslot.IsInterviewer = Interviewer[i];
                    timeslot.IsStudent = Student[i];
                    timeslot.MaxSignUps = MaxSignups;
                    timeslots.Add(timeslot);
                }
            }

            return timeslots;
        }
    }
}
