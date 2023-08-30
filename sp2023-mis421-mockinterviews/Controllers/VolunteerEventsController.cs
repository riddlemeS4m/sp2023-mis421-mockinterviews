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
                .OrderBy(ve => ve.TimeslotId)
                .ToListAsync();

            var timeRanges = new ControlBreakVolunteer(_userManager);
            var groupedEvents = await timeRanges.ToTimeRanges(volunteerEvents);

            return View(groupedEvents);
        }

        // GET: VolunteerEvents/Details/5
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
        public IActionResult Create()
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userTask = _userManager.FindByIdAsync(userId);
            userTask.GetAwaiter().GetResult();
            var user = userTask.Result;

            var timeslotsTask = _context.Timeslot
                .Where(x => x.IsVolunteer == true)
                .Include(y => y.EventDate)
                .Where(x => !_context.VolunteerEvent.Any(y => y.TimeslotId == x.Id && y.StudentId == userId))
                .Where(x => !_context.InterviewEvent.Any(y => y.TimeslotId == x.Id && y.StudentId == userId))
                .Where(x => x.EventDate.For221 == false)
                .ToListAsync();
            timeslotsTask.GetAwaiter().GetResult();
            var timeslots = timeslotsTask.Result;
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

            var studentIds = allVolunteerEvents.Select(v => v.StudentId).Distinct().ToList();

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
                await emailNotification.SendEmailAsync(_sendGridClient, SubjectLineConstants.VolunteerCancellationNotification + fullName, CurrentAdmin.Email, fullName, volunteerEvent.ToString());

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

            var times = "";
            foreach (TimeRangeViewModel interview in groupedEvents)
            {
                times += interview.StartTime + " - " + interview.EndTime + " on " + interview.Date.ToString(@"M/dd/yyyy") + "<br>";
            }

            ASendAnEmail emailer = new VolunteerSignupConfirmation();
            await emailer.SendEmailAsync(_sendGridClient, SubjectLineConstants.VolunteerSignupConfirmation, user.Email, user.FirstName, times);

            string fullName = user.FirstName + " " + user.LastName;

            ASendAnEmail emailNotification = new VolunteerSignupNotification();
            await emailNotification.SendEmailAsync(_sendGridClient, SubjectLineConstants.VolunteerSignupNotification + fullName, CurrentAdmin.Email, fullName, times);
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
    }
}
