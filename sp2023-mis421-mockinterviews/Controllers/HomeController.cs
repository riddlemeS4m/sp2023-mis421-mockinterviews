using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            model.VolunteerEventViewModels = new List<VolunteerEventViewModel>();
            if(User.Identity.IsAuthenticated)
            {
                model.Name = $"{userFull.FirstName} {userFull.LastName}";
            }

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

            model.SignupInterviewerTimeslots = new List<SignupInterviewerTimeslot>();
            if(User.IsInRole(RolesConstants.AdminRole) || User.IsInRole(RolesConstants.InterviewerRole))
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
    }
}