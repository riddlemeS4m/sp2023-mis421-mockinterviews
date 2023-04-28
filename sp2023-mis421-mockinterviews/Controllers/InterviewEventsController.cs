using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Security.Claims;
using System.Security.Policy;
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
using NuGet.Versioning;

namespace sp2023_mis421_mockinterviews.Controllers
{
    public class InterviewEventsController : Controller
    {
        private readonly MockInterviewDataDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public InterviewEventsController(MockInterviewDataDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = RolesConstants.AdminRole)]
        // GET: InterviewEvents
        public async Task<IActionResult> Index()
        {
            var interviewEvents = await _context.InterviewEvent
                .Include(i => i.Location)
                .Include(i => i.SignupInterviewerTimeslot)
                .ThenInclude(i => i.SignupInterviewer)
                .Include(i => i.Timeslot)
                .ThenInclude(j => j.EventDate)
                .ToListAsync();

            var model = new List<InterviewEventViewModel>();
            var interviewEventViewModel = new InterviewEventViewModel();
            foreach(InterviewEvent interviewEvent in interviewEvents)
            {
                var student = await _userManager.FindByIdAsync(interviewEvent.StudentId);

                if(interviewEvent.SignupInterviewerTimeslot != null)
                {
                    var interviewer = await _userManager.FindByIdAsync(interviewEvent.SignupInterviewerTimeslot.SignupInterviewer.InterviewerId);

                    interviewEventViewModel = new InterviewEventViewModel
                    {
                        InterviewEvent = interviewEvent,
                        StudentName = student.FirstName + " " + student.LastName,
                        InterviewerName = interviewer.FirstName + " " + interviewer.LastName
                    };

                    model.Add(interviewEventViewModel);
                }
                else
                {
                    interviewEventViewModel = new InterviewEventViewModel
                    {
                        InterviewEvent = interviewEvent,
                        StudentName = student.FirstName + " " + student.LastName,
                        InterviewerName = "Not Assigned"
                    };

                    model.Add(interviewEventViewModel);
                }
            }

            return View(model);
        }

        [Authorize(Roles = RolesConstants.StudentRole)]
        public async Task<IActionResult> FeedbackIndex()
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userFull = await _userManager.FindByIdAsync(userId);

            var model = new IndexViewModel();
            model.StudentScheduledInterviews = new List<InterviewEventViewModel>();
            if (User.IsInRole(RolesConstants.AdminRole) || User.IsInRole(RolesConstants.StudentRole))
            {
                var interviewEvents = await _context.InterviewEvent
                    .Include(v => v.SignupInterviewerTimeslot)
                    .ThenInclude(v => v.SignupInterviewer)
                    .Include(v => v.Location)
                    .Include(v => v.Timeslot)
                    .ThenInclude(v => v.EventDate)
                    .Where(v => v.StudentId == userId && v.Status == StatusConstants.Completed)
                    .ToListAsync();

                if (interviewEvents != null)
                {
                    foreach (InterviewEvent interviewEvent in interviewEvents)
                    {
                        if (interviewEvent.SignupInterviewerTimeslot != null)
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

            return View("FeedbackIndex", model);
        }

        [Authorize(Roles = RolesConstants.StudentRole)]
        public async Task<IActionResult> ProvideFeedback(int id)
        {
            var interviewEvent = await _context.InterviewEvent
                .Include(x=>x.SignupInterviewerTimeslot)
                .ThenInclude(x=>x.SignupInterviewer)
                .Include(x => x.Location)
                .Include(x=>x.Timeslot)
                .ThenInclude(x=>x.EventDate)
                .FirstOrDefaultAsync(x => x.Id == id);
            var interviewer = await _userManager.FindByIdAsync(interviewEvent.SignupInterviewerTimeslot.SignupInterviewer.InterviewerId);
            var model = new InterviewEventViewModel()
            {
                InterviewEvent = interviewEvent,
                InterviewerName = $"{interviewer.FirstName} {interviewer.LastName}"
            };

            return View("ProvideFeedback", model);
        }

        [Authorize(Roles = RolesConstants.StudentRole)]
        [HttpPost]
        public async Task<IActionResult> ProvideFeedback(int id, [Bind("Id,StudentId,TimeslotId,LocationId,Status,InterviewType,SignupInterviewerTimeslotId,InterviewerRating,InterviewerFeedback,ProcessFeedback")] InterviewEvent interviewEvent)
        {
            if (id != interviewEvent.Id)
            {
                return NotFound();
            }


            if (ModelState.IsValid)
            {

                try
                {
                    _context.Update(interviewEvent);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InterviewEventExists(interviewEvent.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("FeedbackIndex","InterviewEvents");
            }


            var interviewEventActual = await _context.InterviewEvent
                .Include(x => x.SignupInterviewerTimeslot)
                .ThenInclude(x => x.SignupInterviewer)
                .Include(x => x.Location)
                .Include(x => x.Timeslot)
                .ThenInclude(x => x.EventDate)
                .FirstOrDefaultAsync(x => x.Id == id);
            var interviewer = await _userManager.FindByIdAsync(interviewEventActual.SignupInterviewerTimeslot.SignupInterviewer.InterviewerId);
            var model = new InterviewEventViewModel()
            {
                InterviewEvent = interviewEventActual,
                InterviewerName = $"{interviewer.FirstName} {interviewer.LastName}"
            };

            return View("ProvideFeedback", model);
        }


        // GET: InterviewEvents/Details/5
        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.InterviewEvent == null)
            {
                return NotFound();
            }

            var interviewEvent = await _context.InterviewEvent
                .Include(i => i.Location)
                .Include(i => i.SignupInterviewerTimeslot)
                .ThenInclude(i => i.SignupInterviewer)
                .Include(i => i.Timeslot)
                .ThenInclude(j => j.EventDate)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (interviewEvent == null)
            {
                return NotFound();
            }

            var student = await _userManager.FindByIdAsync(interviewEvent.StudentId);
            var interviewer = await _userManager.FindByIdAsync(interviewEvent.SignupInterviewerTimeslot.SignupInterviewer.InterviewerId);

            var viewModel = new InterviewEventViewModel
            {
                InterviewEvent = interviewEvent,
                InterviewerName = interviewer.FirstName + " " + interviewer.LastName,
                StudentName = student.FirstName + " " + student.LastName
            };


            return View(viewModel);
        }

        // GET: InterviewEvents/Create
        [Authorize(Roles = RolesConstants.StudentRole)]
        public IActionResult Create()
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userTask = _userManager.FindByIdAsync(userId);
            userTask.GetAwaiter().GetResult();
            var user = userTask.Result;

            var timeslotsTask = _context.Timeslot
                .Where(x => x.IsStudent)
                .Include(y => y.EventDate)
                .Where(x => _context.InterviewEvent.Count(y => y.TimeslotId == x.Id) < x.MaxSignUps)
                .ToListAsync();
            timeslotsTask.GetAwaiter().GetResult();
            var timeslots = timeslotsTask.Result;

            var interviewEventsTask = _context.InterviewEvent
                .Where(x => x.StudentId == userId)
                .ToListAsync();
            interviewEventsTask.GetAwaiter().GetResult();
            var interviewEvents = interviewEventsTask.Result;

            bool signedUp = false;
            if (interviewEvents.Count > 1)
            {
                signedUp = true;
            }

            InterviewEventSignupViewModel model = new InterviewEventSignupViewModel
            {
                Timeslots = timeslots,
                ApplicationUser = user,
                SignedUp = signedUp
            };

            return View(model);
        }

        // POST: InterviewEvents/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = RolesConstants.StudentRole)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int SelectedEventIds)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            var interviewTypeTwo = InterviewTypesConstants.Technical;
            if (user.Class == ClassConstants.PreMIS || user.Class == ClassConstants.FirstSemester)
            {
                interviewTypeTwo = InterviewTypesConstants.Behavioral;
            }

            var interviewEvents = new List<InterviewEvent>{
                new InterviewEvent {
                    TimeslotId = SelectedEventIds,
                    StudentId = userId,
                    Status = StatusConstants.Default,
                    InterviewType = InterviewTypesConstants.Behavioral
                },
                new InterviewEvent {
                    TimeslotId = SelectedEventIds + 1,
                    StudentId = userId,
                    Status = StatusConstants.Default,
                    InterviewType= interviewTypeTwo
                }
};



			if (ModelState.IsValid)
            {
                _context.AddRange(interviewEvents);
                await _context.SaveChangesAsync();

				var emailTimes = new List<InterviewEvent>();

				var newEvent = await _context.InterviewEvent
		.Include(v => v.Timeslot)
		.ThenInclude(y => y.EventDate)
		.Where(v => v.Id == SelectedEventIds)
		.FirstOrDefaultAsync();
				emailTimes.Add(newEvent);
				newEvent = await _context.InterviewEvent
		.Include(v => v.Timeslot)
		.ThenInclude(y => y.EventDate)
		.Where(v => v.Id == SelectedEventIds + 1)
		.FirstOrDefaultAsync();
				emailTimes.Add(newEvent);




				var client = new SendGridClient("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");
				var from = new EmailAddress("mismockinterviews@gmail.com", "UA MIS Program Support");
				var subject = "Interviewer Sign-Up Confirmation";
				var to = new EmailAddress(user.Email);
				var plainTextContent = "";
				var htmlContent = " <head>\r\n    <title>Interviewee Confirmation Email</title>\r\n    <style>\r\n      /* Define styles for the header */\r\n      header {\r\n        background-color: crimson;\r\n        color: white;\r\n        text-align: center;\r\n        padding: 20px;\r\n      }\r\n      \r\n      /* Define styles for the subheading */\r\n      .subheading {\r\n        color: black;\r\n        font-weight: bold;\r\n        margin: 20px 0;\r\n      }\r\n      \r\n      /* Define styles for the closing */\r\n      .closing {\r\n        font-style: italic;\r\n        margin-top: 20px;\r\n        text-align: center;\r\n      }\r\n    </style>\r\n  </head>\r\n  <body>\r\n    <header>\r\n      <h1>Thank you for signing up, " + user.FirstName + "!</h1>\r\n    </header>\r\n    <div class=\"content\">\r\n      <p class=\"subheading\">\r\n        You have signed up for MIS Mock Interviews for the following times:<br>";
				foreach (InterviewEvent interview in emailTimes)
				{

					htmlContent += interview.ToString();
				}
				htmlContent += "This email serves as a confirmation that your information has been submitted to Program Support.\r\n      </p>\r\n      <p>\r\n        If you have any questions or concerns, please don't hesitate to contact us.\r\n      </p>\r\n      <p class=\"closing\">\r\n        Thank you, Program Support\r\n      </p>\r\n    </div>\r\n  </body>";
				var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
				var response = client.SendEmailAsync(msg);

				return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // GET: InterviewEvents/Edit/5
        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.InterviewEvent == null)
            {
                return NotFound();
            }

            var interviewEvent = await _context.InterviewEvent.FindAsync(id);
            if (interviewEvent == null)
            {
                return NotFound();
            }

            var selectedInterviewers = await OutsourceQuery(interviewEvent);

            var selectedInterviewersNames = new List<SelectListItem>();
            if (selectedInterviewers.Count == 0)
            {
                if(interviewEvent.SignupInterviewerTimeslot != null)
                {
                    selectedInterviewersNames.Add(new SelectListItem
                    {
                        Value = interviewEvent.SignupInterviewerTimeslot.SignupInterviewer.InterviewerId,
                        Text = interviewEvent.SignupInterviewerTimeslot.SignupInterviewer.FirstName + " " + interviewEvent.SignupInterviewerTimeslot.SignupInterviewer.LastName
                    });
                }
                else
                {
                    selectedInterviewersNames.Add(new SelectListItem
                    {
                        Value = "0",
                        Text = "No Interviewers Available"
                    });
                }

            }
            else
            {
                if (interviewEvent.SignupInterviewerTimeslot != null)
                {
                    selectedInterviewersNames.Add(new SelectListItem
                    {
                        Value = interviewEvent.SignupInterviewerTimeslot.SignupInterviewer.InterviewerId,
                        Text = interviewEvent.SignupInterviewerTimeslot.SignupInterviewer.FirstName + " " + interviewEvent.SignupInterviewerTimeslot.SignupInterviewer.LastName
                    });
                }
                foreach (string sit in selectedInterviewers)
                {
                    var user = await _userManager.FindByIdAsync(sit);

                    selectedInterviewersNames.Add(new SelectListItem
                    {
                        Value = user.Id,
                        Text = user.FirstName + " " + user.LastName
                    });
                }
                
            }


            var interviewEventManageViewModel = new InterviewEventManageViewModel
            {
                InterviewEvent = interviewEvent,
                InterviewerNames = selectedInterviewersNames
            };

            return View(interviewEventManageViewModel);
        }

        // POST: InterviewEvents/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = RolesConstants.AdminRole)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,StudentId,LocationId,TimeslotId,InterviewType,Status,SignupInterviewerTimeslotId")] InterviewEvent interviewEvent, string InterviewerId)
        {
            if (id != interviewEvent.Id)
            {
                return NotFound();
            }

            //if(InterviewerId == "0")
            //{
            //    interviewEvent.SignupInterviewerTimeslot = null;
            //}

            if (ModelState.IsValid)
            {
                if(InterviewerId == "0")
                {
                    interviewEvent.SignupInterviewerTimeslot = null;
                }
                else
                {
                    var signupInterviewTimeslot = await _context.SignupInterviewerTimeslot
                        .Include(x => x.SignupInterviewer)
                        .Include(x => x.Timeslot)
                        .Where(x => x.TimeslotId == interviewEvent.TimeslotId && x.SignupInterviewer.InterviewerId == InterviewerId)
                        .FirstOrDefaultAsync();

                    var location = await _context.LocationInterviewer
                        .Include(x => x.Location)
                        .Where(x => x.InterviewerId == InterviewerId)
                        .FirstOrDefaultAsync();

                    interviewEvent.SignupInterviewerTimeslot = signupInterviewTimeslot;
                    interviewEvent.Location = location.Location;
                    interviewEvent.SignupInterviewerTimeslotId = signupInterviewTimeslot.Id;
                    interviewEvent.LocationId = location.Location.Id;
                }
                
                try
                {
                    _context.Update(interviewEvent);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InterviewEventExists(interviewEvent.Id))
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

            return View(interviewEvent);
        }

        // GET: InterviewEvents/Delete/5
        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.InterviewEvent == null)
            {
                return NotFound();
            }

            var interviewEvent = await _context.InterviewEvent.Include(i => i.Location).Include(i => i.SignupInterviewerTimeslot)
                .Include(i => i.Timeslot)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (interviewEvent == null)
            {
                return NotFound();
            }

            return View(interviewEvent);
        }

        // POST: InterviewEvents/Delete/5
        [Authorize(Roles = RolesConstants.AdminRole)]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.InterviewEvent == null)
            {
                return Problem("Entity set 'MockInterviewDataDbContext.InterviewEvent'  is null.");
            }
            var interviewEvent = await _context.InterviewEvent.FindAsync(id);
            if (interviewEvent != null)
            {
                _context.InterviewEvent.Remove(interviewEvent);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction("Index","Home");
        }

        private bool InterviewEventExists(int id)
        {
          return (_context.InterviewEvent?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        private async Task<List<string>> OutsourceQuery(InterviewEvent interviewEvent)
        {
            // Get the timeslot of the current user's interview
            var timeslot = await _context.InterviewEvent
                .Include(i => i.Timeslot.EventDate)
                .Where(i => i.StudentId == interviewEvent.StudentId && i.TimeslotId == interviewEvent.TimeslotId)
                .Select(i => i.Timeslot)
                .FirstOrDefaultAsync();

            // Get the SignupInterviewerTimeslots for the same timeslot and event date as the user's interview
            var signupInterviewerTimeslots = await _context.SignupInterviewerTimeslot
                .Include(s => s.SignupInterviewer)
                .Where(s => s.TimeslotId == timeslot.Id && s.Timeslot.EventDate.Date == timeslot.EventDate.Date)
                .ToListAsync();

            // Get a list of all Interviewers
            var interviewersTask = await _userManager.GetUsersInRoleAsync(RolesConstants.InterviewerRole);
            var interviewers = interviewersTask.ToList();

            //Get list of all distinct interviewers that have signed up to deliver the same type of interview as the student needs
            var selectedTypes = _context.SignupInterviewer
                .Where(u => ((interviewEvent.InterviewType == InterviewTypesConstants.Technical && u.IsTechnical) ||
                             (interviewEvent.InterviewType == InterviewTypesConstants.Behavioral && u.IsBehavioral)))
                .Select(x => x.InterviewerId)
                .Distinct()
                .ToList();

            //Get list of all distinct interviewers that have a timeslot that matches the student's timeslot
            var selectedTimeslot = signupInterviewerTimeslots
                .Select(u => u.SignupInterviewer.InterviewerId)
                .Distinct()
                .ToList();

            //Gets list of all distinct interviewers that aren't in an InterviewEvent with a status of checked in or ongoing
            var selectedStatus = interviewers
                .Where(u => !_context.InterviewEvent.Any(i =>
                    i.SignupInterviewerTimeslot.SignupInterviewer.InterviewerId == u.Id &&
                    (i.Status == StatusConstants.CheckedIn || i.Status == StatusConstants.Ongoing)))
                .Select(x => x.Id)
                .Distinct()
                .ToList();

            //Get list of all distinct interviewers that are assigned to a location
            var selectedLocation = _context.LocationInterviewer
                .Where(x => x.LocationId != null)
                .Select(x => x.InterviewerId)
                .Distinct()
                .ToList();



            //Get list of all distinct interviewers that have not interviewed this student
            var haveInterviewed = _context.InterviewEvent
                .Where(x => x.StudentId == interviewEvent.StudentId)
                .Select(x => x.SignupInterviewerTimeslot.SignupInterviewer.InterviewerId)
                .Distinct()
                .ToList();

            var interviewersIds = interviewers
                .Select(x => x.Id)
                .Distinct()
                .ToList();

            var haveNotInterviewed = interviewersIds
                .Except(haveInterviewed)
                .ToList();

            //Get list of all distinct interviewers that are not the student
            var notStudent = interviewers
                .Where(x => x.Id != interviewEvent.StudentId)
                .Select(x => x.Id)
                .Distinct()
                .ToList();


            //combine the previous four query results
            var selectedInterviewers = selectedTypes
                .Intersect(selectedTimeslot)
                .Intersect(selectedStatus)
                .Intersect(selectedLocation)
                .Intersect(haveNotInterviewed)
                .Intersect(notStudent)
                .ToList();

            return selectedInterviewers;
        }

        [Authorize(Roles = RolesConstants.StudentRole)]
        public async Task<IActionResult> UserDelete(int? id)
        {
            if (id == null || _context.InterviewEvent == null)
            {
                return NotFound();
            }

            var interviewEvent = await _context.InterviewEvent.Include(i => i.Location).Include(i => i.SignupInterviewerTimeslot)
                .Include(i => i.Timeslot)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (interviewEvent == null)
            {
                return NotFound();
            }

            if (interviewEvent.StudentId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return NotFound();
            }

            return View(interviewEvent);
        }

        // POST: InterviewEvents/Delete/5
        [Authorize(Roles = RolesConstants.StudentRole)]
        public async Task<IActionResult> UserDeleteConfirmed(int id)
        {
            if (_context.InterviewEvent == null)
            {
                return Problem("Entity set 'MockInterviewDataDbContext.InterviewEvent'  is null.");
            }
            var interviewEvent = await _context.InterviewEvent.FindAsync(id);
            if (interviewEvent != null && interviewEvent.StudentId == User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                _context.InterviewEvent.Remove(interviewEvent);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
