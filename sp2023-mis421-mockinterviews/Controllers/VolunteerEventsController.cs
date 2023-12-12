using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SendGrid.Helpers.Mail;
using SendGrid;
using sp2023_mis421_mockinterviews.Data;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;
using sp2023_mis421_mockinterviews.Models.UserDb;
using sp2023_mis421_mockinterviews.Models.ViewModels;
using sp2023_mis421_mockinterviews.Data.Constants;
using sp2023_mis421_mockinterviews.Data.Access;
using sp2023_mis421_mockinterviews.Interfaces;
using Microsoft.Extensions.Hosting;
using sp2023_mis421_mockinterviews.Data.Access.Emails;
using System.Globalization;
using System.Text;

namespace sp2023_mis421_mockinterviews.Controllers
{
    public class VolunteerEventsController : Controller
    {
        private readonly MockInterviewDataDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISendGridClient _sendGridClient;
    
        public VolunteerEventsController(MockInterviewDataDbContext context, 
            UserManager<ApplicationUser> userManager,
            ISendGridClient sendGridClient)
        {
            _context = context;
            _userManager = userManager;
            _sendGridClient = sendGridClient;
        }

        // GET: VolunteerEvents
        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> Index()
        {
            var volunteerEvents = await _context.VolunteerEvent
                .Include(v => v.Timeslot)
                .ThenInclude(y => y.EventDate)
                .Where(y => y.Timeslot.EventDate.IsActive == true)
                .OrderBy(ve => ve.StudentId)
                .ThenBy(x => x.TimeslotId)
                .ToListAsync();

            var timeRanges = new ControlBreakVolunteer(_userManager);
            var groupedEvents = await timeRanges.ToTimeRanges(volunteerEvents);

            return View(groupedEvents);
        }

        // GET: VolunteerEvents/Details/
        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.VolunteerEvent == null)
            {
                return NotFound();
            }

            var volunteerEvent = await _context.VolunteerEvent
                .Include(v => v.Timeslot)
                .FirstOrDefaultAsync(m => m.Id == id);
            var specificTimeslot = await _context.Timeslot
                .Include(v => v.EventDate)
                .FirstOrDefaultAsync(m => m.Id == volunteerEvent.Timeslot.Id);
            volunteerEvent.Timeslot = specificTimeslot;
            if (volunteerEvent == null)
            {
                return NotFound();
            }

            return View(volunteerEvent);
        }

        // GET: VolunteerEvents/Create
        [Authorize(Roles = RolesConstants.StudentRole)]
        public async Task<IActionResult> Create()
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            //var userTask = _userManager.FindByIdAsync(userId);
            //userTask.GetAwaiter().GetResult();
            //var user = userTask.Result;

            var timeslots = await _context.Timeslot
                .Where(x => x.IsVolunteer == true)
                .Include(y => y.EventDate)
                .Where(x => !_context.VolunteerEvent.Any(y => y.TimeslotId == x.Id && y.StudentId == userId))
                .Where(x => !_context.InterviewEvent.Any(y => y.TimeslotId == x.Id && y.StudentId == userId))
                .Where(x => x.EventDate.For221 == For221.n)
                .Where(x => x.EventDate.IsActive == true)
                .ToListAsync();

            VolunteerEventSignupViewModel volunteerEventsViewModel = new()
            {
                Timeslots = timeslots
            };

            if(timeslots.Count == 0)
            {
                volunteerEventsViewModel.SignedUp = true;
            }
            else
            {
                volunteerEventsViewModel.SignedUp = false;
            }

            return View(volunteerEventsViewModel);
        }

        // POST: VolunteerEvents/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = RolesConstants.StudentRole)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int[] SelectedEventIds1, int[] SelectedEventIds2)
        {
            int[] SelectedEventIds = SelectedEventIds1.Concat(SelectedEventIds2).ToArray(); 

            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            List<VolunteerEvent> volEvents = new List<VolunteerEvent>();

            var allVolunteerEvents = await _context.VolunteerEvent
                .Include(v => v.Timeslot)
                .ThenInclude(y => y.EventDate)
                .ToListAsync();

            var studentIds = allVolunteerEvents
                .Select(v => v.StudentId)
                .Distinct()
                .ToList();

            foreach (int id in SelectedEventIds)
            {
                VolunteerEvent volunteerEvent = new VolunteerEvent
                {
                    TimeslotId = id,
                    StudentId = userId
                };
                if (ModelState.IsValid)
                {
                    
                    _context.Add(volunteerEvent);
                    await _context.SaveChangesAsync();

                    var newEvent = await _context.VolunteerEvent
                        .Include(v => v.Timeslot)
                        .ThenInclude(y => y.EventDate)
                        .Where(v => v.Id == volunteerEvent.Id)
                        .FirstOrDefaultAsync();

                    volEvents.Add(newEvent);
                }
            }

            var sortedEvents = volEvents
                .OrderBy(ve => ve.TimeslotId)
                .ToList();

            ComposeEmail(user, sortedEvents);

            return RedirectToAction("Index", "Home");
        }

        // GET: VolunteerEvents/Edit/5
        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.VolunteerEvent == null)
            {
                return NotFound();
            }

            var volunteerEvent = await _context.VolunteerEvent.FindAsync(id);
            if (volunteerEvent == null)
            {
                return NotFound();
            }
            ViewData["TimeslotId"] = new SelectList(_context.Timeslot, "Id", "Id", volunteerEvent.TimeslotId);
            return View(volunteerEvent);
        }

        // POST: VolunteerEvents/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = RolesConstants.AdminRole)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,StudentId,TimeslotId")] VolunteerEvent volunteerEvent)
        {
            if (id != volunteerEvent.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(volunteerEvent);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VolunteerEventExists(volunteerEvent.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["TimeslotId"] = new SelectList(_context.Timeslot, "Id", "Id", volunteerEvent.TimeslotId);
            return View(volunteerEvent);
        }

        // GET: VolunteerEvents/Delete/5
        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.VolunteerEvent == null)
            {
                return NotFound();
            }

            var volunteerEvent = await _context.VolunteerEvent
                .Include(v => v.Timeslot).ThenInclude(y => y.EventDate)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (volunteerEvent == null)
            {
                return NotFound();
            }

            return View(volunteerEvent);
        }

        // POST: VolunteerEvents/Delete/5
        [Authorize(Roles = RolesConstants.AdminRole)]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
			string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

			if (_context.VolunteerEvent == null)
            {
                return Problem("Entity set 'MockInterviewDataDbContext.VolunteerEvent'  is null.");
            }

            var volunteerEvent = await _context.VolunteerEvent.FindAsync(id);
            if (volunteerEvent != null)
            {
                string fullName = user.FirstName + " " + user.LastName;

                ASendAnEmail emailNotification = new VolunteerCancellationNotification();
                await emailNotification.SendEmailAsync(_sendGridClient, SubjectLineConstants.VolunteerCancellationNotification + fullName, CurrentAdmin.Email, fullName, volunteerEvent.ToString(), null);

                _context.VolunteerEvent.Remove(volunteerEvent);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }

        private bool VolunteerEventExists(int id)
        {
          return (_context.VolunteerEvent?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        private async Task<ActionResult> GetUserId()
        {
            var user = await _userManager.GetUserAsync(User);
            return Content(user.Id);
        }

        private async void ComposeEmail(ApplicationUser user, List<VolunteerEvent> emailTimes)
        {
            var timeRanges = new ControlBreakVolunteer(_userManager);
            var groupedEvents = await timeRanges.ToTimeRanges(emailTimes);
            List<string> calendarEvents = new List<string>();

            var times = "";
            foreach (TimeRangeViewModel interview in groupedEvents)
            {
                DateTime combinedStart = CombineDateWithTimeString(interview.Date, interview.StartTime);
                DateTime combinedEnd = CombineDateWithTimeString(interview.Date, interview.EndTime);
                var plainBytes = Encoding.UTF8.GetBytes(CreateCalendarEvent(combinedStart, combinedEnd));
                string newEvent = Convert.ToBase64String(plainBytes);
                calendarEvents.Add(newEvent);
                times += interview.StartTime + " - " + interview.EndTime + " on " + interview.Date.ToString(@"M/dd/yyyy") + "<br>";
            }

            ASendAnEmail emailer = new VolunteerSignupConfirmation();
            await emailer.SendEmailAsync(_sendGridClient, SubjectLineConstants.VolunteerSignupConfirmation, user.Email, user.FirstName, times, calendarEvents);

            string fullName = user.FirstName + " " + user.LastName;

            ASendAnEmail emailNotification = new VolunteerSignupNotification();
            await emailNotification.SendEmailAsync(_sendGridClient, SubjectLineConstants.VolunteerSignupNotification + fullName, CurrentAdmin.Email, fullName, times, null);
        }

        [Authorize(Roles = RolesConstants.StudentRole + "," + RolesConstants.AdminRole)]
        public async Task<IActionResult> UserDeleteRange(int[] timeslots)
        {
            // Check if the timeslotIds array is empty or null
            if (timeslots == null || timeslots.Length == 0)
            {
                return NotFound();
            }

            // Get the timeslots to delete
            var timeslotsToDelete = await _context.VolunteerEvent
                .Include(x => x.Timeslot)
                .ThenInclude(x => x.EventDate)
                .Where(t => timeslots.Contains(t.Id))
                .ToListAsync();

            // Check if any of the timeslots to delete are null
            if (timeslotsToDelete == null || timeslotsToDelete.Count == 0)
            {
                return NotFound();
            }

            var date = timeslotsToDelete.First().Timeslot.EventDate.Date;
            if (timeslotsToDelete.Any(t => t.Timeslot.EventDate.Date != date))
            {
                return NotFound();
            }

            if (!timeslotsToDelete.All(e => e.StudentId == User.FindFirstValue(ClaimTypes.NameIdentifier)) && !User.IsInRole(RolesConstants.AdminRole))
            {
                return NotFound();
            }

            var timeslotslist = timeslots.ToList();

            var viewModel = new TimeRangeViewModel
            {
                Date = date,
                StartTime = timeslotsToDelete.First().Timeslot.Time.ToString(@"h\:mm tt"),
                EndTime = timeslotsToDelete.Last().Timeslot.Time.AddMinutes(30).ToString(@"h\:mm tt"),
                TimeslotIds = timeslotslist
            };


            return View(viewModel);
        }

        [Authorize(Roles = RolesConstants.StudentRole + "," + RolesConstants.AdminRole)]
        public async Task<IActionResult> UserDeleteRangeConfirmed(int[] timeslots)
        {

            // Get the timeslots to delete
            var timeslotsToDelete = await _context.VolunteerEvent
                
                .Where(t => timeslots.Contains(t.Id))
                .ToListAsync();

            if (!timeslotsToDelete.All(e => e.StudentId == User.FindFirstValue(ClaimTypes.NameIdentifier)) && !User.IsInRole(RolesConstants.AdminRole))
            {
                return NotFound();
            }

            // Delete the timeslots
            _context.VolunteerEvent.RemoveRange(timeslotsToDelete);
            await _context.SaveChangesAsync();

            if (User.IsInRole(RolesConstants.AdminRole))
            {
                return RedirectToAction("Index", "VolunteerEvents");
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> DeleteRange(int[] timeslots)
        {
            // Check if the timeslotIds array is empty or null
            if (timeslots == null || timeslots.Length == 0)
            {
                return NotFound();
            }

            // Get the timeslots to delete
            var timeslotsToDelete = await _context.VolunteerEvent
                .Include(x => x.Timeslot)
                .ThenInclude(x => x.EventDate)
                .Where(t => timeslots.Contains(t.Id))
                .ToListAsync();

            // Check if any of the timeslots to delete are null
            if (timeslotsToDelete == null || timeslotsToDelete.Count == 0)
            {
                return NotFound();
            }

            var date = timeslotsToDelete.First().Timeslot.EventDate.Date;
            if (timeslotsToDelete.Any(t => t.Timeslot.EventDate.Date != date))
            {
                return NotFound();
            }

            var timeslotslist = timeslots.ToList();

            var viewModel = new TimeRangeViewModel
            {
                Date = date,
                StartTime = timeslotsToDelete.First().Timeslot.Time.ToString(@"h\:mm tt"),
                EndTime = timeslotsToDelete.Last().Timeslot.Time.AddMinutes(30).ToString(@"h\:mm tt"),
                TimeslotIds = timeslotslist
            };


            return View(viewModel);
        }

        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> DeleteRangeConfirmed(int[] timeslots)
        {

            // Get the timeslots to delete
            var timeslotsToDelete = await _context.VolunteerEvent

                .Where(t => timeslots.Contains(t.Id))
                .ToListAsync();

            // Delete the timeslots
            _context.VolunteerEvent.RemoveRange(timeslotsToDelete);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "VolunteerEvents");
        }
        private static DateTime CombineDateWithTimeString(DateTime date, string timeString)
        {
            Console.WriteLine(date);
            Console.WriteLine(timeString);
            DateTime dateTime = DateTime.ParseExact(timeString, "h:mm tt", CultureInfo.InvariantCulture);
            TimeSpan timeSpan = dateTime.TimeOfDay;
            return date.Date + timeSpan;
        }

        private string CreateCalendarEvent(DateTime start, DateTime end)
        {

            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("BEGIN:VCALENDAR");
            stringBuilder.AppendLine("VERSION:2.0");
            stringBuilder.AppendLine("PRODID:-//YourCompany//YourProduct//EN"); // Optional identifier
            stringBuilder.AppendLine("BEGIN:VTIMEZONE");
            stringBuilder.AppendLine("TZID:America/Chicago");
            stringBuilder.AppendLine("BEGIN:DAYLIGHT");
            stringBuilder.AppendLine("TZOFFSETFROM:-0600");
            stringBuilder.AppendLine("TZOFFSETTO:-0500");
            stringBuilder.AppendLine("TZNAME:CDT");
            stringBuilder.AppendLine("DTSTART:19700308T020000");
            stringBuilder.AppendLine("RRULE:FREQ=YEARLY;BYMONTH=3;BYDAY=2SU");
            stringBuilder.AppendLine("END:DAYLIGHT");
            stringBuilder.AppendLine("BEGIN:STANDARD");
            stringBuilder.AppendLine("TZOFFSETFROM:-0500");
            stringBuilder.AppendLine("TZOFFSETTO:-0600");
            stringBuilder.AppendLine("TZNAME:CST");
            stringBuilder.AppendLine("DTSTART:19701101T020000");
            stringBuilder.AppendLine("RRULE:FREQ=YEARLY;BYMONTH=11;BYDAY=1SU");
            stringBuilder.AppendLine("END:STANDARD");
            stringBuilder.AppendLine("END:VTIMEZONE");
            stringBuilder.AppendLine("BEGIN:VEVENT");
            stringBuilder.AppendLine("UID:" + Guid.NewGuid());
            stringBuilder.AppendFormat("DTSTAMP:{0:yyyyMMddTHHmmssZ}\r\n", DateTime.UtcNow); // Added DTSTAMP
            stringBuilder.AppendLine("SEQUENCE:0"); // Added SEQUENCE for indicating the version of the event
            stringBuilder.AppendFormat("DTSTART;TZID=America/Chicago:{0:yyyyMMddTHHmmss}\r\n", start);
            stringBuilder.AppendFormat("DTEND;TZID=America/Chicago:{0:yyyyMMddTHHmmss}\r\n", end);
            stringBuilder.AppendLine("SUMMARY:UA MIS Mock Interviews");
            stringBuilder.AppendLine("BEGIN:VALARM");
            stringBuilder.AppendLine("TRIGGER:-P14D"); // 14 days before
            stringBuilder.AppendLine("ACTION:DISPLAY");
            stringBuilder.AppendLine("DESCRIPTION:Reminder");
            stringBuilder.AppendLine("END:VALARM");
            // Add a reminder for 3 days before the event
            stringBuilder.AppendLine("BEGIN:VALARM");
            stringBuilder.AppendLine("TRIGGER:-P3D"); // 3 days before
            stringBuilder.AppendLine("ACTION:DISPLAY");
            stringBuilder.AppendLine("DESCRIPTION:Reminder");
            stringBuilder.AppendLine("END:VALARM");
            // Add a reminder for 30 minutes before the event
            stringBuilder.AppendLine("BEGIN:VALARM");
            stringBuilder.AppendLine("TRIGGER:-PT30M"); // 30 minutes before
            stringBuilder.AppendLine("ACTION:DISPLAY");
            stringBuilder.AppendLine("DESCRIPTION:Reminder");
            stringBuilder.AppendLine("END:VALARM");
            stringBuilder.AppendLine("END:VEVENT");
            stringBuilder.AppendLine("END:VCALENDAR");


            return stringBuilder.ToString();
        }
    }
}
