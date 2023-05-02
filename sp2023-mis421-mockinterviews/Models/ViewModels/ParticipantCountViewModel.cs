using sp2023_mis421_mockinterviews.Models.MockInterviewDb;

namespace sp2023_mis421_mockinterviews.Models.ViewModels
{
	public class ParticipantCountViewModel
	{
		public Timeslot Timeslot { get; set; }
		public int StudentCount { get; set; }
		public int InterviewerCount { get; set; }
		public int VolunteerCount { get; set; }
	}
}
