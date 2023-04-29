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
                .ToListAsync();

            var studentIds = volunteerEvents.Select(v => v.StudentId).Distinct().ToList();

            var students = await _userManager.Users.Where(u => studentIds.Contains(u.Id)).ToListAsync();

            var query = from volunteerEvent in volunteerEvents
                        join student in students on volunteerEvent.StudentId equals student.Id
                        select new VolunteerEventViewModel
                        {
                            VolunteerEvent = volunteerEvent,
                            StudentName = student.FirstName + " " + student.LastName,
                        };

            var viewModel = query.ToList();

            return View(viewModel);
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

            var timeslotsTask = _context.Timeslot
                .Where(x => x.IsVolunteer == true)
                .Include(y => y.EventDate)
                .Where(x => !_context.VolunteerEvent.Any(y => y.TimeslotId == x.Id && y.StudentId == userId))
                .ToListAsync();
            timeslotsTask.GetAwaiter().GetResult();
            var timeslots = timeslotsTask.Result;
            VolunteerEventSignupViewModel volunteerEventsViewModel = new VolunteerEventSignupViewModel
            {
                Timeslots = timeslots
            };
            return View(volunteerEventsViewModel);
        }

        // POST: VolunteerEvents/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = RolesConstants.StudentRole)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int[] SelectedEventIds)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            string email = User.FindFirstValue(ClaimTypes.Email);
            string username = User.FindFirstValue(ClaimTypes.Name);
            var student = await _userManager.Users.Where(u => u.Id == userId).FirstOrDefaultAsync();
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
                    //return RedirectToAction(nameof(Index));
                }
            }
            var from = new EmailAddress("mismockinterviews@gmail.com", "UA MIS Program Support");
            var subject = "Mock Interviews Volunteer Sign-Up Confirmation";
            var to = new EmailAddress(email);
            var plainTextContent = "";
            var htmlContent = " <head>\r\n    <title>Volunteer Confirmation Email</title>\r\n    <style>\r\n      /* Define styles for the header */\r\n      header {\r\n        background-color: crimson;\r\n        color: white;\r\n        text-align: center;\r\n        padding: 20px;\r\n      }\r\n      \r\n      /* Define styles for the subheading */\r\n      .subheading {\r\n        color: black;\r\n        font-weight: bold;\r\n        margin: 20px 0;\r\n      }\r\n      \r\n      /* Define styles for the closing */\r\n      .closing {\r\n        font-style: italic;\r\n        margin-top: 20px;\r\n        text-align: center;\r\n      }\r\n    </style>\r\n  </head>\r\n  <body>\r\n    <header>\r\n      <h1>Thank you for Volunteering, "+student.FirstName+"!</h1>\r\n    </header>\r\n    <div class=\"content\">\r\n      <p class=\"subheading\">\r\n        You have signed up to be a volunteer for MIS Mock Interviews for the following times:<br>";
            foreach(VolunteerEvent vol in volEvents)
            {
                htmlContent += vol.ToString();
            }
            htmlContent += "This email serves as a confirmation that your volunteer information has been submitted to Program Support.\r\n      </p>\r\n      <p>\r\n        If you have any questions or concerns, please don't hesitate to contact us.\r\n      </p>\r\n      <p class=\"closing\">\r\n        Thank you, Program Support\r\n      </p>\r\n    </div>\r\n  </body>";
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = _sendGridClient.SendEmailAsync(msg);
            System.Console.WriteLine(response);
            var from2 = new EmailAddress("mismockinterviews@gmail.com", "UA MIS Program Support");
            var subject2 = "Mock Interviews Volunteer Sign-Up Confirmation";
            var to2 = new EmailAddress("lmthompson6@crimson.ua.edu");
            var plainTextContent2 = "";
            var htmlContent2 = $"<h1>{student.FirstName} {student.LastName} has signed up to volunteer at Mock Interviews</h1>";
            var msg2 = MailHelper.CreateSingleEmail(from2, to2, subject2, plainTextContent2, htmlContent2);
            var response2 = _sendGridClient.SendEmailAsync(msg2);
            System.Console.WriteLine(response2);
            //var timeslots = await _context.Timeslot
            //    .Where(x => x.IsVolunteer == true)
            //    .Include(y => y.EventDate)
            //    .Where(x => !_context.VolunteerEvent.Any(y => y.TimeslotId == x.Id && y.StudentId == userId))
            //    .ToListAsync();
            //VolunteerEventsViewModel volunteerEventsViewModel = new VolunteerEventsViewModel
            //{
            //    Timeslots = timeslots
            //};
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
            //ViewData["StudentId"] = new SelectList(_context.Student, "Id", "Id", volunteerEvent.StudentId);
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
			string email = User.FindFirstValue(ClaimTypes.Email);
			string username = User.FindFirstValue(ClaimTypes.Name);
			var student = await _userManager.Users.Where(u => u.Id == userId).FirstOrDefaultAsync();

			if (_context.VolunteerEvent == null)
            {
                return Problem("Entity set 'MockInterviewDataDbContext.VolunteerEvent'  is null.");
            }
            var volunteerEvent = await _context.VolunteerEvent.FindAsync(id);
            if (volunteerEvent != null)
            {
				var volunteerEventLocal = await _context.VolunteerEvent
	            .Include(v => v.Timeslot)
	            .FirstOrDefaultAsync(m => m.Id == id);
				var specificTimeslot = await _context.Timeslot
					.Include(v => v.EventDate)
					.FirstOrDefaultAsync(m => m.Id == volunteerEvent.Timeslot.Id);
                volunteerEventLocal.Timeslot = specificTimeslot;
				var from2 = new EmailAddress("mismockinterviews@gmail.com", "UA MIS Program Support");
				var subject2 = "Mock Interviews Volunteer Canellation";
				var to2 = new EmailAddress("lmthompson6@crimson.ua.edu");
				var plainTextContent2 = "";
				var htmlContent2 = $"<h1>{student.FirstName} {student.LastName} has cancelled their volunteering for Mock Interviews for {volunteerEventLocal.Timeslot.Time} on {volunteerEventLocal.Timeslot.EventDate.Date}</h1>";
				var msg2 = MailHelper.CreateSingleEmail(from2, to2, subject2, plainTextContent2, htmlContent2);
				var response2 = _sendGridClient.SendEmailAsync(msg2);

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
    }
}
