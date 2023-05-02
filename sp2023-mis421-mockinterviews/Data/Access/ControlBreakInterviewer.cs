using Microsoft.AspNetCore.Identity;
using sp2023_mis421_mockinterviews.Data.Constants;
using sp2023_mis421_mockinterviews.Interfaces;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;
using sp2023_mis421_mockinterviews.Models.UserDb;
using sp2023_mis421_mockinterviews.Models.ViewModels;

namespace sp2023_mis421_mockinterviews.Data.Access
{
	public class ControlBreakInterviewer : IControlBreakInterviewers
	{
		private readonly UserManager<ApplicationUser> _userManager;
		public ControlBreakInterviewer(UserManager<ApplicationUser> userManager)
		{
			_userManager = userManager;
		}

		public async Task<List<TimeRangeViewModel>> ToTimeRanges(List<SignupInterviewerTimeslot> signupInterviewTimeslots)
		{
			var groupedEvents = new List<TimeRangeViewModel>();
			if (signupInterviewTimeslots != null && signupInterviewTimeslots.Count != 0)
			{
				var ints = new List<int>();
				var currentStart = signupInterviewTimeslots.First().Timeslot;
				var currentEnd = signupInterviewTimeslots.First().Timeslot;
				var inperson = signupInterviewTimeslots.First().SignupInterviewer.InPerson;
				var interviewerId = signupInterviewTimeslots.First().SignupInterviewer.InterviewerId;
				var interviewtype = (signupInterviewTimeslots.First().SignupInterviewer.IsBehavioral, signupInterviewTimeslots.First().SignupInterviewer.IsTechnical);
				ints.Add(signupInterviewTimeslots.First().Id);

				string? location;
				for (int i = 1; i < signupInterviewTimeslots.Count; i++)
				{
					var nextEvent = signupInterviewTimeslots[i].Timeslot;

					if (currentEnd.Id + 1 == nextEvent.Id
						&& currentEnd.EventDate.Date == nextEvent.EventDate.Date
						&& signupInterviewTimeslots[i].SignupInterviewer.InPerson == inperson
						&& signupInterviewTimeslots[i].SignupInterviewer.InterviewerId == interviewerId
						&& (signupInterviewTimeslots[i].SignupInterviewer.IsBehavioral, signupInterviewTimeslots[i].SignupInterviewer.IsTechnical) == interviewtype)
					{
						currentEnd = nextEvent;
						ints.Add(signupInterviewTimeslots[i].Id);
					}
					else
					{
						if (signupInterviewTimeslots[i - 1].SignupInterviewer.InPerson)
						{
							location = InterviewLocationConstants.InPerson;
						}
						else
						{
							location = InterviewLocationConstants.Virtual;
						}

						string? type;
						if (signupInterviewTimeslots[i - 1].SignupInterviewer.IsBehavioral && !signupInterviewTimeslots[i - 1].SignupInterviewer.IsTechnical)
						{
							type = InterviewTypesConstants.Behavioral;
						}
						else if (!signupInterviewTimeslots[i - 1].SignupInterviewer.IsBehavioral && signupInterviewTimeslots[i - 1].SignupInterviewer.IsTechnical)
						{
							type = InterviewTypesConstants.Technical;
						}
						else
						{
							type = InterviewTypesConstants.Behavioral + "/" + InterviewTypesConstants.Technical;
						}

						var name = await _userManager.FindByIdAsync(signupInterviewTimeslots[i - 1].SignupInterviewer.InterviewerId);
						groupedEvents.Add(new TimeRangeViewModel
						{
							Date = currentStart.EventDate.Date,
							EndTime = currentEnd.Time.AddMinutes(30).ToString(@"h\:mm tt"),
							StartTime = currentStart.Time.ToString(@"h\:mm tt"),
							Location = location,
							Name = name.FirstName + " " + name.LastName,
							InterviewType = type,
							TimeslotIds = ints
						});

						currentStart = nextEvent;
						currentEnd = nextEvent;
						ints = new List<int>
							{
								signupInterviewTimeslots[i].Id
							};
						inperson = signupInterviewTimeslots[i].SignupInterviewer.InPerson;
						interviewerId = signupInterviewTimeslots[i].SignupInterviewer.InterviewerId;
						interviewtype = (signupInterviewTimeslots[i].SignupInterviewer.IsBehavioral, signupInterviewTimeslots[i].SignupInterviewer.IsTechnical);
					}
				}

				if (inperson)
				{
					location = InterviewLocationConstants.InPerson;
				}
				else
				{
					location = InterviewLocationConstants.Virtual;
				}

				string? lasttype;
				if (interviewtype.IsBehavioral && !interviewtype.IsTechnical)
				{
					lasttype = InterviewTypesConstants.Behavioral;
				}
				else if (!interviewtype.IsBehavioral && interviewtype.IsTechnical)
				{
					lasttype = InterviewTypesConstants.Technical;
				}
				else
				{
					lasttype = InterviewTypesConstants.Behavioral + "/" + InterviewTypesConstants.Technical;
				}

				var user = await _userManager.FindByIdAsync(interviewerId);
				groupedEvents.Add(new TimeRangeViewModel
				{
					Date = currentStart.EventDate.Date,
					EndTime = currentEnd.Time.AddMinutes(30).ToString(@"h\:mm tt"),
					StartTime = currentStart.Time.ToString(@"h\:mm tt"),
					Location = location,
					Name = user.FirstName + " " + user.LastName,
					InterviewType = lasttype,
					TimeslotIds = ints
				});
			}

			return groupedEvents;
		}
	}
}
