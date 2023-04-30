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

namespace sp2023_mis421_mockinterviews.Controllers
{
    public class SignupInterviewerTimeslotsController : Controller
    {
        private readonly MockInterviewDataDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISendGridClient _sendGridClient;

        public SignupInterviewerTimeslotsController(MockInterviewDataDbContext context, 
            UserManager<ApplicationUser> userManager,
            ISendGridClient sendGridClient)
        {
            _context = context;
            _userManager = userManager;
            _sendGridClient = sendGridClient;
        }

        // GET: SignupInterviewerTimeslots
        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> Index()
        {
            var signupInterviewerTimeslots = await _context.SignupInterviewerTimeslot
                .Include(s => s.SignupInterviewer)
                .Include(s => s.Timeslot)
                .ThenInclude(s => s.EventDate)
                .Where(s => s.Timeslot.IsInterviewer)
                .ToListAsync();

            return View(signupInterviewerTimeslots);
        }

        // GET: SignupInterviewerTimeslots/Details/5
        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.SignupInterviewerTimeslot == null)
            {
                return NotFound();
            }

            var signupInterviewerTimeslot = await _context.SignupInterviewerTimeslot
                .Include(s => s.SignupInterviewer)
                .Include(s => s.Timeslot)
                .ThenInclude(s => s.EventDate)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (signupInterviewerTimeslot == null)
            {
                return NotFound();
            }

            return View(signupInterviewerTimeslot);
        }

        // GET: SignupInterviewerTimeslots/Create
        [Authorize(Roles = RolesConstants.InterviewerRole)]
        public IActionResult Create()
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userTask = _userManager.FindByIdAsync(userId);
            userTask.GetAwaiter().GetResult();
            var user = userTask.Result;


            var timeslotsTask = _context.Timeslot
                .Where(x => x.IsInterviewer == true)
                .Include(y => y.EventDate)
                .Where(x => !_context.SignupInterviewerTimeslot.Any(y => y.TimeslotId == x.Id && y.SignupInterviewer.InterviewerId == userId))
                .ToListAsync();
            timeslotsTask.GetAwaiter().GetResult();
            var timeslots = timeslotsTask.Result;
            SignupInterviewerTimeslotsViewModel volunteerEventsViewModel = new SignupInterviewerTimeslotsViewModel
            {
                Timeslots = timeslots,
                SignupInterviewer = new SignupInterviewer
                { 
                    InterviewerId = userId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    IsBehavioral = false,
                    IsTechnical = false,
                    IsVirtual = false,
                    InPerson = false
                }
            };
            return View(volunteerEventsViewModel);

        }

        // POST: SignupInterviewerTimeslots/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = RolesConstants.InterviewerRole)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int[] SelectedEventIds, [Bind("IsTechnical,IsBehavioral,IsVirtual,InPerson")] SignupInterviewer signupInterviewer )
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            var timeslots = await _context.Timeslot
                .Where(x => x.IsInterviewer == true)
                .Include(y => y.EventDate)
                .Where(x => !_context.SignupInterviewerTimeslot.Any(y => y.TimeslotId == x.Id && y.SignupInterviewer.InterviewerId == userId))
                .ToListAsync();

            var dates = timeslots
                .Select(t => t.EventDate.Id)
                .Distinct()
                .ToList();

            SignupInterviewerTimeslotsViewModel volunteerEventsViewModel = new SignupInterviewerTimeslotsViewModel
            {
                Timeslots = timeslots,
                SignupInterviewer = new SignupInterviewer
                {
                    InterviewerId = userId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    IsBehavioral = false,
                    IsTechnical = false,
                    IsVirtual = false,
                    InPerson = false
                }
            };

            if (!signupInterviewer.IsTechnical && !signupInterviewer.IsBehavioral)
            {
                ModelState.AddModelError("SignupInterviewer.IsTechnical", "Please select at least one checkbox");
                
                return View(volunteerEventsViewModel);
            }

            // Check whether at least one timeslot is selected
            if (SelectedEventIds == null || SelectedEventIds.Length == 0)
            {
                ModelState.AddModelError("SelectedEventIds", "Please select at least one timeslot");
                return View(volunteerEventsViewModel);
            }

            var existingSignupInterviewer = await _context.SignupInterviewer.FirstOrDefaultAsync(si =>
                    si.IsVirtual == signupInterviewer.IsVirtual &&
                    si.InPerson == signupInterviewer.InPerson &&
                    si.InterviewerId == userId);

            SignupInterviewer post;
            if (existingSignupInterviewer != null)
            {
                post = existingSignupInterviewer;
            }
            else
            {
                post = new SignupInterviewer
                {
                    InterviewerId = userId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    InPerson = signupInterviewer.InPerson,
                    IsVirtual = !signupInterviewer.InPerson,
                    IsBehavioral = signupInterviewer.IsBehavioral,
                    IsTechnical = signupInterviewer.IsTechnical
                };

                var interviewerPreference = "";
                if(signupInterviewer.InPerson)
                {
                    interviewerPreference = InterviewLocationConstants.InPerson;
                }
                else
                {
                    interviewerPreference = InterviewLocationConstants.Virtual;
                }

                //foreach(int id in SelectedEventIds)
                //{
                //    var timeslot = await _context.Timeslot.FirstOrDefaultAsync(t => t.Id == id);
                //}

                if (ModelState.IsValid)
                {
                    _context.Add(post);
                    await _context.SaveChangesAsync();

                    foreach(int date in dates)
                    {
                        _context.Add(new LocationInterviewer
                        {
                            LocationId = null,
                            InterviewerId = userId,
                            InterviewerPreference = interviewerPreference,
                            EventDateId = date
                        });
                        await _context.SaveChangesAsync();
                    }
                }
            }


            foreach (int id in SelectedEventIds)
            {
                var timeslot = new SignupInterviewerTimeslot 
                { 
                    TimeslotId = id, 
                    SignupInterviewerId = post.Id 
                };

                if (ModelState.IsValid)
                {
                    _context.Add(timeslot);
                    await _context.SaveChangesAsync();
                }
            }

            ComposeEmail(user, SelectedEventIds, post.Id);

            return RedirectToAction("Index", "Home");
        }

        // GET: SignupInterviewerTimeslots/Edit/5
        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.SignupInterviewerTimeslot == null)
            {
                return NotFound();
            }

            var signupInterviewerTimeslot = await _context.SignupInterviewerTimeslot.FindAsync(id);
            if (signupInterviewerTimeslot == null)
            {
                return NotFound();
            }

            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
           

            var timeslots = await _context.Timeslot
                .Where(x => x.IsInterviewer == true)
                .Include(y => y.EventDate)
                //.Where(x => !_context.SignupInterviewerTimeslot.Any(y => y.TimeslotId == x.Id && y.SignupInterviewer.InterviewerId == userId))
                .ToListAsync();
            
            SignupInterviewerTimeslotsViewModel volunteerEventsViewModel = new SignupInterviewerTimeslotsViewModel
            {
                Timeslots = timeslots,
                SignupInterviewer = signupInterviewerTimeslot.SignupInterviewer
            };
            return View(volunteerEventsViewModel);
        }

        // POST: SignupInterviewerTimeslots/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        //currently not used -sam
        [Authorize(Roles = RolesConstants.AdminRole)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,SignupInterviewerId,TimeslotId")] SignupInterviewerTimeslot signupInterviewerTimeslot)
        {
            if (id != signupInterviewerTimeslot.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(signupInterviewerTimeslot);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SignupInterviewerTimeslotExists(signupInterviewerTimeslot.Id))
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
            //ViewData["SignupInterviewerId"] = new SelectList(_context.SignupInterviewer, "Id", "Id", signupInterviewerTimeslot.SignupInterviewerId);
            //ViewData["TimeslotId"] = new SelectList(_context.Set<Timeslot>(), "Id", "Id", signupInterviewerTimeslot.TimeslotId);
            return View(signupInterviewerTimeslot);
        }

        // GET: SignupInterviewerTimeslots/Delete/5
        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.SignupInterviewerTimeslot == null)
            {
                return NotFound();
            }

            var signupInterviewerTimeslot = await _context.SignupInterviewerTimeslot
                .Include(s => s.SignupInterviewer)
                .Include(s => s.Timeslot).ThenInclude(y => y.EventDate)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (signupInterviewerTimeslot == null)
            {
                return NotFound();
            }

            return View(signupInterviewerTimeslot);
        }

        // POST: SignupInterviewerTimeslots/Delete/5
        [Authorize(Roles = RolesConstants.AdminRole)]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.SignupInterviewerTimeslot == null)
            {
                return Problem("Entity set 'MockInterviewDataDbContext.SignupInterviewerTimeslot'  is null.");
            }
            var signupInterviewerTimeslot = await _context.SignupInterviewerTimeslot.FindAsync(id);
            if (signupInterviewerTimeslot != null)
            {
                _context.SignupInterviewerTimeslot.Remove(signupInterviewerTimeslot);
            }
            
            await _context.SaveChangesAsync();
			return RedirectToAction("Index", "Home");
        }

        private bool SignupInterviewerTimeslotExists(int id)
        {
          return (_context.SignupInterviewerTimeslot?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        private async void ComposeEmail(ApplicationUser user, int[] SelectedEventIds, int signupInterviewerId)
        {
            var emailTimes = new List<SignupInterviewerTimeslot>();

            foreach (int id in SelectedEventIds)
            {
                var timeslot = new SignupInterviewerTimeslot { TimeslotId = id, SignupInterviewerId = signupInterviewerId };
                var newEvent = await _context.SignupInterviewerTimeslot
                    .Include(v => v.Timeslot)
                    .ThenInclude(y => y.EventDate).Where(v => v.Id == timeslot.Id).FirstOrDefaultAsync();
                emailTimes.Add(newEvent);
            }

            var from = new EmailAddress("mismockinterviews@gmail.com", "UA MIS Program Support");
            var subject = "Interviewer Sign-Up Confirmation";
            var to = new EmailAddress(user.Email);
            var plainTextContent = "";
            var htmlContent = " <head>\r\n    <title>Interviewer Confirmation Email</title>\r\n    <style>\r\n      /* Define styles for the header */\r\n      header {\r\n        background-color: crimson;\r\n        color: white;\r\n        text-align: center;\r\n        padding: 20px;\r\n      }\r\n      \r\n      /* Define styles for the subheading */\r\n      .subheading {\r\n        color: black;\r\n        font-weight: bold;\r\n        margin: 20px 0;\r\n      }\r\n      \r\n      /* Define styles for the closing */\r\n      .closing {\r\n        font-style: italic;\r\n        margin-top: 20px;\r\n        text-align: center;\r\n      }\r\n    </style>\r\n  </head>\r\n  <body>\r\n    <header>\r\n      <h1>Thank you for signing up, " + user.FirstName + "!</h1>\r\n    </header>\r\n    <div class=\"content\">\r\n      <p class=\"subheading\">\r\n        You have signed up to be an interviewer for MIS Mock Interviews for the following times:<br>";
            foreach (SignupInterviewerTimeslot interview in emailTimes)
            {
                htmlContent += interview.ToString();
            }
            htmlContent += "This email serves as a confirmation that your interviewer information has been submitted to Program Support.\r\n      </p>\r\n      <p>\r\n        If you have any questions or concerns, please don't hesitate to contact us.\r\n      </p>\r\n      <p class=\"closing\">\r\n        Thank you, Program Support\r\n      </p>\r\n    </div>\r\n  </body>";
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = _sendGridClient.SendEmailAsync(msg);
            System.Console.WriteLine(response);
            var from2 = new EmailAddress("mismockinterviews@gmail.com", "UA MIS Program Support");
            var subject2 = "Mock Interviews Interviewer Sign-Up Confirmation";
            var to2 = new EmailAddress("lmthompson6@crimson.ua.edu");
            var plainTextContent2 = "";
            var htmlContent2 = $"<h1>{user.FirstName} {user.LastName} has signed up to interview at Mock Interviews</h1>";
            var msg2 = MailHelper.CreateSingleEmail(from2, to2, subject2, plainTextContent2, htmlContent2);
            var response2 = _sendGridClient.SendEmailAsync(msg2);
            System.Console.WriteLine(response2);
        }

        [Authorize(Roles = RolesConstants.InterviewerRole)]
        public async Task<IActionResult> UserDelete(int? id)
        {
            if (id == null || _context.SignupInterviewerTimeslot == null)
            {
                return NotFound();
            }

            var signupInterviewerTimeslot = await _context.SignupInterviewerTimeslot
                .Include(s => s.SignupInterviewer)
                .Include(s => s.Timeslot).ThenInclude(y => y.EventDate)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (signupInterviewerTimeslot == null)
            {
                return NotFound();
            }

            if (signupInterviewerTimeslot.SignupInterviewer.InterviewerId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return NotFound();
            }

            return View(signupInterviewerTimeslot);
        }

        // POST: SignupInterviewerTimeslots/Delete/5
        [Authorize(Roles = RolesConstants.InterviewerRole)]
        public async Task<IActionResult> UserDeleteConfirmed(int id)
        {
            if (_context.SignupInterviewerTimeslot == null)
            {
                return Problem("Entity set 'MockInterviewDataDbContext.SignupInterviewerTimeslot'  is null.");
            }
            var signupInterviewerTimeslot = await _context.SignupInterviewerTimeslot
                .Include(s => s.SignupInterviewer)
                .Include(s => s.Timeslot)
                .ThenInclude(y => y.EventDate)
                .FirstOrDefaultAsync(m => m.Id == id);
            ;
            if (signupInterviewerTimeslot != null && signupInterviewerTimeslot.SignupInterviewer.InterviewerId == User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                _context.SignupInterviewerTimeslot.Remove(signupInterviewerTimeslot);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
