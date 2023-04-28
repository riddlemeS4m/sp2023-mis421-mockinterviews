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

namespace sp2023_mis421_mockinterviews.Controllers
{
    public class HomeController : Controller
    {
/*        private readonly ILogger<HomeController> _logger;*/
        private readonly MockInterviewDataDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(/*ILogger<HomeController> logger,*/ MockInterviewDataDbContext context, UserManager<ApplicationUser> userManager)
        {
/*            _logger = logger;*/
            _context = context;
            _userManager = userManager;
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
            var uniqueUsers = _context.InterviewEvent.GroupBy(s => s.StudentId).Select(g => new { StudentId = g.Key });
            foreach (var user in uniqueUsers)
            {
                var userFull = await _userManager.FindByIdAsync(user.StudentId);
                var interviews = await _context.InterviewEvent.Include(x => x.Timeslot).ThenInclude(x => x.EventDate).Where(x => x.StudentId == user.StudentId).ToListAsync();
                var client = new SendGridClient("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");
                var from = new EmailAddress("mismockinterviews@gmail.com", "UA MIS Program Support");
                var subject = "Mock Interviews Reminder";
                var to = new EmailAddress(userFull.Email);
                var plainTextContent = "";
                var htmlContent = " <head>\r\n    <title>MIS Mock Interviews Reminder</title>\r\n    <style>\r\n      /* Define styles for the header */\r\n      header {\r\n        background-color: crimson;\r\n        color: white;\r\n        text-align: center;\r\n        padding: 20px;\r\n      }\r\n      \r\n      /* Define styles for the subheading */\r\n      .subheading {\r\n        color: black;\r\n        font-weight: bold;\r\n        margin: 20px 0;\r\n      }\r\n      \r\n      /* Define styles for the closing */\r\n      .closing {\r\n        font-style: italic;\r\n        margin-top: 20px;\r\n        text-align: center;\r\n      }\r\n    </style>\r\n  </head>\r\n  <body>\r\n    <header>\r\n      <h1>Hey, " + userFull.FirstName + "! Mock Interviews are coming up!</h1>\r\n    </header>\r\n    <div class=\"content\">\r\n      <p class=\"subheading\">\r\n        You have signed up to be interviewed during the following times:<br>";
                foreach (var interview in interviews)
                {
                    htmlContent += interview.Timeslot.Time;
                    htmlContent += " on ";
                    htmlContent += interview.Timeslot.EventDate.Date;
                }
                htmlContent += "<br>This email serves as your final reminder that you have signed-up for Mock Interviews. Please be sure to arrive <u>15 minutes early</u> to your first interview time!\r\n      </p>\r\n      <p>\r\n        If you have any questions or concerns, please don't hesitate to contact us.\r\n      </p>\r\n      <p class=\"closing\">\r\n        Thank you, Program Support\r\n      </p>\r\n    </div>\r\n  </body>";
                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
                var response = client.SendEmailAsync(msg);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}