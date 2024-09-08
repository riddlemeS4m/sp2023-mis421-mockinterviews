using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sp2023_mis421_mockinterviews.Data.Constants;
using sp2023_mis421_mockinterviews.Interfaces.IDbContext;
using sp2023_mis421_mockinterviews.Models.ViewModels.ReportsController;
using sp2023_mis421_mockinterviews.Services.SignupDb;

namespace sp2023_mis421_mockinterviews.Areas.Reports.Controllers
{
    [Area("Reports")]
    [Authorize(Roles = RolesConstants.AdminRole)]
    public class ReportsController : Controller
    {
        private readonly EventService _eventService;
        private readonly ISignupDbContext _context;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(EventService eventService, 
            ISignupDbContext context, 
            ILogger<ReportsController> logger)
        {
            _eventService = eventService;
            _context = context;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> EventStatistics()
        {
            var events = await _eventService.GetAllAsync();

            var participantCounts = new List<ParticipantCountPerDateViewModel>();

            foreach (var @event in events)
            {
                var studentCount = await _context.Interviews
                    .Where(e => e.Timeslot.EventId == @event.Id)
                    .Select(e => e.StudentId)
                    .Distinct()
                    .CountAsync();

                var interviewerCount = await _context.InterviewerTimeslots
                    .Where(s => s.Timeslot.EventId == @event.Id)
                    .Select(s => s.InterviewerSignup.InterviewerId)
                    .Distinct()
                    .CountAsync();

                var volunteerCount = await _context.VolunteerTimeslots
                    .Where(v => v.Timeslot.EventId == @event.Id)
                    .Select(v => v.StudentId)
                    .Distinct()
                    .CountAsync();

                var countViewModel = new ParticipantCountPerDateViewModel
                {
                    EventDate = @event,
                    StudentCount = studentCount,
                    InterviewerCount = interviewerCount,
                    VolunteerCount = volunteerCount
                };

                participantCounts.Add(countViewModel);
            }

            var uniqueStudentCount = await _context.Interviews
                .Where(x => x.Timeslot.Event.IsActive)
                .Select(e => e.StudentId)
                .Distinct()
                .CountAsync();

            var uniqueInterviewerCount = await _context.InterviewerTimeslots
                .Where(s => s.Timeslot.Event.IsActive)
                .Select(s => s.InterviewerSignup.InterviewerId)
                .Distinct()
                .CountAsync();

            var uniqueVolunteerCount = await _context.VolunteerTimeslots
                .Where(v => v.Timeslot.Event.IsActive)
                .Select(v => v.StudentId)
                .Distinct()
                .CountAsync();


            var eventStatisticsVM = new EventStatisticsViewModel
            {
                EventStatistics = participantCounts,
                TotalStudents = uniqueStudentCount,
                TotalInterviewers = uniqueInterviewerCount,
                TotalVolunteers = uniqueVolunteerCount
            };

            return View("EventStatistics", eventStatisticsVM);
        }
    }
}
