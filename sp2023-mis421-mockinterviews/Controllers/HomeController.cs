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

            IndexViewModel model = new IndexViewModel();
            if(User.Identity.IsAuthenticated)
            {
                model.Name = $"{userFull.FirstName} {userFull.LastName}";
            }

            model.VolunteerEventViewModels = new List<VolunteerEventViewModel>();
            if (User.IsInRole(RolesConstants.AdminRole) || User.IsInRole(RolesConstants.StudentRole))
            {
                var volunteerEvents = await _context.VolunteerEvent
                    .Include(v => v.Timeslot)
                    .ThenInclude(y => y.EventDate)
                    .Where(v => v.StudentId == userId)
                    .ToListAsync();
                if(volunteerEvents != null)
                {
                    foreach (VolunteerEvent volunteerEvent in volunteerEvents)
                    {
                        model.VolunteerEventViewModels.Add(new VolunteerEventViewModel()
                        {
                            VolunteerEvent = volunteerEvent
                        });
                    }
                }
                
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

                if (interviewEvents != null)
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
            if (User.IsInRole(RolesConstants.AdminRole) || User.IsInRole(RolesConstants.InterviewerRole))
            {
                var signupInterviewTimeslots = await _context.SignupInterviewerTimeslot
                    .Include(v => v.Timeslot)
                    .ThenInclude(v => v.EventDate)
                    .Include(v => v.SignupInterviewer)
                    .Where(v => v.SignupInterviewer.InterviewerId == userId && v.Timeslot.IsInterviewer)
                    .ToListAsync();

                if (signupInterviewTimeslots != null)
                {
                    foreach (SignupInterviewerTimeslot signupInterviewerTimeslot in signupInterviewTimeslots)
                    {
                        model.SignupInterviewerTimeslots.Add(signupInterviewerTimeslot);
                    }
                }
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

                if (interviewEvents != null)
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
    }
}