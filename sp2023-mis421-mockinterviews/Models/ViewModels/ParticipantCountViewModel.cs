using sp2023_mis421_mockinterviews.Models.MockInterviewDb;
using System.ComponentModel.DataAnnotations;

namespace sp2023_mis421_mockinterviews.Models.ViewModels
{
	public class ParticipantCountViewModel
	{
		public Timeslot Timeslot { get; set; }
		[Display(Name ="Number of Students")]
		public int StudentCount { get; set; }
		[Display(Name = "Number of Interviewers")]
		public int InterviewerCount { get; set; }
		[Display(Name = "Number of Volunteers")]
		public int VolunteerCount { get; set; }
	}
}
