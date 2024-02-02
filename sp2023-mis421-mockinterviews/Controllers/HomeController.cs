using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SendGrid.Helpers.Mail;
using SendGrid;
using sp2023_mis421_mockinterviews.Data;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;
using sp2023_mis421_mockinterviews.Models.UserDb;
using sp2023_mis421_mockinterviews.Models.ViewModels;
using System.Diagnostics;
using System.Security.Claims;
using sp2023_mis421_mockinterviews.Data.Constants;
using sp2023_mis421_mockinterviews.Interfaces;
using sp2023_mis421_mockinterviews.Data.Access;
using sp2023_mis421_mockinterviews.Data.Access.Emails;

namespace sp2023_mis421_mockinterviews.Controllers
{
    public class HomeController : Controller
    {
/*        private readonly ILogger<HomeController> _logger;*/
        private readonly MockInterviewDataDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISendGridClient _sendGridClient;
        private readonly HttpClient _httpClient;

        public HomeController(/*ILogger<HomeController> logger,*/ 
            MockInterviewDataDbContext context, 
            UserManager<ApplicationUser> userManager,
            ISendGridClient sendGridClient)
        {
/*            _logger = logger;*/
            _context = context;
            _userManager = userManager;
            _sendGridClient = sendGridClient;
        }

        public async Task<IActionResult> Index()
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userFull = await _userManager.FindByIdAsync(userId);

            IndexViewModel model = new();
            if(User.Identity.IsAuthenticated)
            {
                model.Name = $"{userFull.FirstName} {userFull.LastName}";

                model.ZoomLink = await GetZoomLink();
                model.ZoomLinkVisible = await GetZoomLinkVisible();
                model.DisruptionBanner = await GetDisruptionBanner();
            }

            model.VolunteerEventViewModels = new List<VolunteerEventViewModel>();
            model.TimeRangeViewModels = new List<TimeRangeViewModel>();
            if (User.IsInRole(RolesConstants.AdminRole) || User.IsInRole(RolesConstants.StudentRole))
            {
                var volunteerEvents = await _context.VolunteerEvent
                    .Include(v => v.Timeslot)
                    .ThenInclude(y => y.EventDate)
                    .OrderBy(ve => ve.TimeslotId)
                    .Where(v => v.StudentId == userId && v.Timeslot.EventDate.IsActive)
                    .ToListAsync();

                var timeRanges = new ControlBreakVolunteer(_userManager);
                var groupedEvents = await timeRanges.ToTimeRanges(volunteerEvents);

                model.TimeRangeViewModels = groupedEvents;
            }

            model.InterviewerScheduledInterviews = new List<InterviewEventViewModel>();
            if(User.IsInRole(RolesConstants.AdminRole) || User.IsInRole(RolesConstants.InterviewerRole))
            {
                var interviewEvents = await _context.InterviewEvent
                    .Include(v => v.SignupInterviewerTimeslot)
                    .ThenInclude(v => v.SignupInterviewer)
                    .Include(v => v.Location)
                    .Include(v => v.Timeslot)
                    .ThenInclude(v => v.EventDate)
                    .Where(v => v.SignupInterviewerTimeslot.SignupInterviewer.InterviewerId == userId 
                        && v.SignupInterviewerTimeslot.Timeslot.EventDate.IsActive
                        && v.Status != StatusConstants.Completed)
                    .ToListAsync();

                if (interviewEvents != null && interviewEvents.Count != 0)
                {
                    foreach (InterviewEvent interviewEvent in interviewEvents)
                    {

                        if (interviewEvent.SignupInterviewerTimeslot != null)
                        {
                            var student = await _userManager.FindByIdAsync(interviewEvent.StudentId);

                            model.InterviewerScheduledInterviews.Add(new InterviewEventViewModel()
                            {
                                InterviewEvent = interviewEvent,
                                StudentName = $"{student.FirstName} {student.LastName}",
                                InterviewerName = $"{userFull.FirstName} {userFull.LastName}"
                            });
                        }
                        else
                        {
                            model.InterviewerScheduledInterviews.Add(new InterviewEventViewModel()
                            {
                                InterviewEvent = interviewEvent,
                                StudentName = "Not Assigned",
                                InterviewerName = $"{userFull.FirstName} {userFull.LastName}"
                            });
                        }
                    }
                }
            }

            model.SignupInterviewerTimeslots = new List<SignupInterviewerTimeslot>();
            model.InterviewerRangeViewModels = new List<TimeRangeViewModel>();
            if (User.IsInRole(RolesConstants.AdminRole) || User.IsInRole(RolesConstants.InterviewerRole))
            {
                var signupInterviewTimeslots = await _context.SignupInterviewerTimeslot
    				.Include(s => s.SignupInterviewer)
					.Include(v => v.Timeslot)
                    .ThenInclude(v => v.EventDate)
                    .Include(v => v.SignupInterviewer)
                    //.OrderBy(ve => ve.Timeslot.EventDate.Date)
                    //.ThenBy(ve => ve.Timeslot.Time)
                    .OrderBy(ve => ve.TimeslotId)
                    .Where(v => v.SignupInterviewer.InterviewerId == userId 
                        && v.Timeslot.IsInterviewer
                        && v.Timeslot.EventDate.IsActive)
                    .ToListAsync();


                if (signupInterviewTimeslots.Count > 0)
                {
                    var si = signupInterviewTimeslots
                        .Select(x => x.SignupInterviewerId)
                        .Distinct()
                        .ToList();

                    model.SignupInterviewerId1 = si[0];

                    if(si.Count == 2)
                    {
                        model.SignupInterviewerId2 = si[1];
                    }
                }

                var timeRanges = new ControlBreakInterviewer(_userManager);
                var groupedEvents = await timeRanges.ToTimeRanges(signupInterviewTimeslots);

                model.InterviewerRangeViewModels = groupedEvents;
            }

            model.StudentScheduledInterviews = new List<InterviewEventViewModel>();
            if(User.IsInRole(RolesConstants.AdminRole) || User.IsInRole(RolesConstants.StudentRole))
            {
                var interviewEvents = await _context.InterviewEvent
                    .Include(v => v.SignupInterviewerTimeslot)
                    .ThenInclude(v => v.SignupInterviewer)
                    .Include(v => v.Location)
                    .Include(v => v.Timeslot)
                    .ThenInclude(v => v.EventDate)
                    .Where(v =>  v.StudentId == userId 
                        && v.Timeslot.EventDate.IsActive)
                    .ToListAsync();

                if (interviewEvents != null && interviewEvents.Count != 0)
                {
                    foreach (InterviewEvent interviewEvent in interviewEvents)
                    {
                        if(interviewEvent.SignupInterviewerTimeslot != null)
                        {
                            var interviewer = await _userManager.FindByIdAsync(interviewEvent.SignupInterviewerTimeslot.SignupInterviewer.InterviewerId);

                            model.StudentScheduledInterviews.Add(new InterviewEventViewModel()
                            {
                                InterviewEvent = interviewEvent,
                                StudentName = $"{userFull.FirstName} {userFull.LastName}",
                                InterviewerName = $"{interviewer.FirstName} {interviewer.LastName}"
                            });
                        }
                        else
                        {
                            model.StudentScheduledInterviews.Add(new InterviewEventViewModel()
                            {
                                InterviewEvent = interviewEvent,
                                StudentName = $"{userFull.FirstName} {userFull.LastName}",
                                InterviewerName = "Not Assigned"
                            });
                        }
                    }
                }
            }

            model.CompletedInterviews = new();
            if (User.IsInRole(RolesConstants.AdminRole) || User.IsInRole(RolesConstants.InterviewerRole))
            {
                var interviewEvents = await _context.InterviewEvent
                    .Include(v => v.SignupInterviewerTimeslot)
                    .ThenInclude(v => v.SignupInterviewer)
                    .Include(v => v.Location)
                    .Include(v => v.Timeslot)
                    .ThenInclude(v => v.EventDate)
                    .Where(v => v.SignupInterviewerTimeslot.SignupInterviewer.InterviewerId == userId
                        && v.SignupInterviewerTimeslot.Timeslot.EventDate.IsActive
                        && v.Status == StatusConstants.Completed)
                    .ToListAsync();

                if (interviewEvents != null && interviewEvents.Count != 0)
                {
                    foreach (InterviewEvent interviewEvent in interviewEvents)
                    {

                        if (interviewEvent.SignupInterviewerTimeslot != null)
                        {
                            var student = await _userManager.FindByIdAsync(interviewEvent.StudentId);

                            model.CompletedInterviews.Add(new InterviewEventViewModel()
                            {
                                InterviewEvent = interviewEvent,
                                StudentName = $"{student.FirstName} {student.LastName}",
                                InterviewerName = $"{userFull.FirstName} {userFull.LastName}"
                            });
                        }
                        else
                        {
                            model.CompletedInterviews.Add(new InterviewEventViewModel()
                            {
                                InterviewEvent = interviewEvent,
                                StudentName = "Not Assigned",
                                InterviewerName = $"{userFull.FirstName} {userFull.LastName}"
                            });
                        }
                    }
                }
            }

            return View(model);
        }

        private async Task<string> GetZoomLink()
        {
            Console.WriteLine("GetZoomLink");
            var banner = await _context.GlobalConfigVar.FirstOrDefaultAsync(m => m.Name == "zoom_link");

            try
            {
                return banner.Value;
            }
            catch
            {
                throw new Exception("GlobalConfigVar 'zoom_link' does not exist.");
            }
        }

        private async Task<string> GetDisruptionBanner()
        {
            Console.WriteLine("Get Disruption Banner");
            var banner = await _context.GlobalConfigVar.FirstOrDefaultAsync(m => m.Name == "disruption_banner");

            try
            {
                if (int.Parse(banner.Value) == 0)
                {
                    return "none";
                }
                return "block";
            }
            catch
            {
                throw new Exception("GlobalConfigVar 'disruption_banner' does not exist, or it is not an integer.");
            }
        }

        private async Task<string> GetZoomLinkVisible()
        {
            Console.WriteLine("Get Zoom Link Visible");
            var banner = await _context.GlobalConfigVar.FirstOrDefaultAsync(m => m.Name == "zoom_link_visible");

            try
            {
                if (int.Parse(banner.Value) == 0)
                {
                    return "none";
                }
                return "block";
            }
            catch
            {
                throw new Exception("GlobalConfigVar 'zoom_link_visible' does not exist, or it is not an integer.");
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> EmailStudents()
        {
            var uniqueUsers = await _context.InterviewEvent
                .Select(x => x.StudentId)
                .Distinct()
                .ToListAsync();

            foreach (var user in uniqueUsers)
            {
                var userFull = await _userManager.FindByIdAsync(user);
                var interviews = await _context.InterviewEvent
                    .Include(x => x.Timeslot)
                    .ThenInclude(x => x.EventDate)
                    .Where(x => x.StudentId == user)
                    .ToListAsync();
                
                var times = "";
                foreach (var interview in interviews)
                {
                    times += interview.ToString();
                }

                ASendAnEmail emailer = new StudentReminderEmail();
                await emailer.SendEmailAsync(_sendGridClient, "Mock Interviews Reminder", userFull.Email, userFull.FirstName, times, null);
            }

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> EmailInterviewers()
        {
            var uniqueUsers = await _context.SignupInterviewer
                .Select(x => x.InterviewerId)
                .Distinct()
                .ToListAsync();

            foreach (var user in uniqueUsers)
            {
                var userFull = await _userManager.FindByIdAsync(user);
                var interviews = await _context.SignupInterviewerTimeslot
                    .Include(x => x.Timeslot)
                    .ThenInclude(x => x.EventDate)
                    .Include(x => x.SignupInterviewer)
                    .OrderBy(x => x.Timeslot.EventDate.Date)
                    .ThenBy(x => x.Timeslot.Time)
                    .Where(x => x.SignupInterviewer.InterviewerId == user)
                    .ToListAsync();

				var timeRanges = new ControlBreakInterviewer(_userManager);
				var groupedEvents = await timeRanges.ToTimeRanges(interviews);

				var times = "";
				foreach (TimeRangeViewModel interview in groupedEvents)
				{
					times += interview.StartTime + " - " + interview.EndTime + " on " + interview.Date.ToString(@"M/dd/yyyy") + "<br>";
				}

				ASendAnEmail emailer = new InterviewerReminderEmail();
                await emailer.SendEmailAsync(_sendGridClient, "UA MIS Mock Interviews Reminder", userFull.Email, userFull.FirstName, times, null);
            }

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> AttemptLogin()
        {
            return View("LandingPage");
        }
    }
}