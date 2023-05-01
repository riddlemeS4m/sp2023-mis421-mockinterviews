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
            }

            model.VolunteerEventViewModels = new List<VolunteerEventViewModel>();
            model.TimeRangeViewModels = new List<TimeRangeViewModel>();
            if (User.IsInRole(RolesConstants.AdminRole) || User.IsInRole(RolesConstants.StudentRole))
            {
                var volunteerEvents = await _context.VolunteerEvent
                    .Include(v => v.Timeslot)
                    .ThenInclude(y => y.EventDate)
                    .OrderBy(ve => ve.Timeslot.EventDate.Date)
                    .ThenBy(ve => ve.Timeslot.Time)
                    .Where(v => v.StudentId == userId)
                    .ToListAsync();

                //if(volunteerEvents != null)
                //{
                //    foreach (VolunteerEvent volunteerEvent in volunteerEvents)
                //    {
                //        model.VolunteerEventViewModels.Add(new VolunteerEventViewModel()
                //        {
                //            VolunteerEvent = volunteerEvent
                //        });
                //    }
                //}

                var groupedEvents = new List<TimeRangeViewModel>();
              
                if(volunteerEvents != null && volunteerEvents.Count != 0)
                {
                    var ints = new List<int>();
                    var currentStart = volunteerEvents.First().Timeslot;
                    var currentEnd = volunteerEvents.First().Timeslot;
                    ints.Add(volunteerEvents.First().Id);

                    for(int i = 1;  i < volunteerEvents.Count; i++)
                    {
                        var nextEvent = volunteerEvents[i].Timeslot;

                        if (currentEnd.Id + 1 == nextEvent.Id 
                            && currentEnd.EventDate.Date == nextEvent.EventDate.Date)
                        {
                            currentEnd = nextEvent;
                            ints.Add(volunteerEvents[i].Id);
                        }
                        else
                        {
                            groupedEvents.Add(new TimeRangeViewModel
                            {
                                Date = currentStart.EventDate.Date,
                                EndTime = currentEnd.Time.AddMinutes(30).ToString(@"h\:mm tt"),
                                StartTime = currentStart.Time.ToString(@"h\:mm tt"),
                                TimeslotIds = ints
                            });

                            currentStart = nextEvent;
                            currentEnd = nextEvent;
                            ints = new List<int>
                            {
                                volunteerEvents[i].Id
                            };
                        }
                    }

                    groupedEvents.Add(new TimeRangeViewModel
                    {
                        Date = currentStart.EventDate.Date,
                        EndTime = currentEnd.Time.AddMinutes(30).ToString(@"h\:mm tt"),
                        StartTime = currentStart.Time.ToString(@"h\:mm tt"),
                        TimeslotIds = ints
                    });
                }

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
                    .Where(v => v.SignupInterviewerTimeslot.SignupInterviewer.InterviewerId == userId)
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
                    .Include(v => v.Timeslot)
                    .ThenInclude(v => v.EventDate)
                    .Include(v => v.SignupInterviewer)
                    .OrderBy(ve => ve.Timeslot.EventDate.Date)
                    .ThenBy(ve => ve.Timeslot.Time)
                    .Where(v => v.SignupInterviewer.InterviewerId == userId && v.Timeslot.IsInterviewer)
                    .ToListAsync();

                //if (signupInterviewTimeslots != null)
                //{
                //    foreach (SignupInterviewerTimeslot signupInterviewerTimeslot in signupInterviewTimeslots)
                //    {
                //        model.SignupInterviewerTimeslots.Add(signupInterviewerTimeslot);
                //    }
                //}

                var groupedEvents = new List<TimeRangeViewModel>();
                var location = "";

                if (signupInterviewTimeslots != null && signupInterviewTimeslots.Count != 0)
                {
                    var ints = new List<int>();
                    var currentStart = signupInterviewTimeslots.First().Timeslot;
                    var currentEnd = signupInterviewTimeslots.First().Timeslot;
                    var inperson = signupInterviewTimeslots.First().SignupInterviewer.InPerson;
                    ints.Add(signupInterviewTimeslots.First().Id);

                    for (int i = 1; i < signupInterviewTimeslots.Count; i++)
                    {
                        var nextEvent = signupInterviewTimeslots[i].Timeslot;

                        if (currentEnd.Id + 1 == nextEvent.Id 
                            && currentEnd.EventDate.Date == nextEvent.EventDate.Date
                            && signupInterviewTimeslots[i].SignupInterviewer.InPerson == inperson)
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

                            groupedEvents.Add(new TimeRangeViewModel
                            {
                                Date = currentStart.EventDate.Date,
                                EndTime = currentEnd.Time.AddMinutes(30).ToString(@"h\:mm tt"),
                                StartTime = currentStart.Time.ToString(@"h\:mm tt"),
                                Location = location,
                                TimeslotIds = ints
                            });

                            currentStart = nextEvent;
                            currentEnd = nextEvent;
                            ints = new List<int>
                            {
                                signupInterviewTimeslots[i].Id
                            };
                            inperson = signupInterviewTimeslots[i].SignupInterviewer.InPerson;
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

                    groupedEvents.Add(new TimeRangeViewModel
                    {
                        Date = currentStart.EventDate.Date,
                        EndTime = currentEnd.Time.AddMinutes(30).ToString(@"h\:mm tt"),
                        StartTime = currentStart.Time.ToString(@"h\:mm tt"),
                        Location = location,
                        TimeslotIds = ints
                    });
                }

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
                    .Where(v =>  v.StudentId == userId)
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

            return View(model);
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
                await emailer.SendEmailAsync(_sendGridClient, SubjectLineConstants.StudentReminderEmail, userFull.Email, userFull.FirstName, times);
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
                    .Where(x => x.SignupInterviewer.InterviewerId == user)
                    .ToListAsync();

                var times = "";
                foreach (var interview in interviews)
                {
                    times += interview.ToString();
                }

                ASendAnEmail emailer = new InterviewerReminderEmail();
                await emailer.SendEmailAsync(_sendGridClient, SubjectLineConstants.InterviewerReminderEmail, userFull.Email, userFull.FirstName, times);
            }

            return RedirectToAction("Index", "Home");
        }
    }
}