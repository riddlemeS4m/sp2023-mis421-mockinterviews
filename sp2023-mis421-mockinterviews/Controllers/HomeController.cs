using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sp2023_mis421_mockinterviews.Data;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;
using sp2023_mis421_mockinterviews.Models.ViewModels;
using System.Diagnostics;
using System.Security.Claims;

namespace sp2023_mis421_mockinterviews.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly MockInterviewDataDbContext _context;

        public HomeController(ILogger<HomeController> logger, MockInterviewDataDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            IndexViewModel model = new IndexViewModel();
            model.VolunteerEventViewModels = new List<VolunteerEventViewModel>();

            if(User.IsInRole(RolesConstants.AdminRole) || User.IsInRole(RolesConstants.StudentRole))
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