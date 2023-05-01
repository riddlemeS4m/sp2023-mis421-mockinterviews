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
using sp2023_mis421_mockinterviews.Interfaces;
using sp2023_mis421_mockinterviews.Data.Access;
using sp2023_mis421_mockinterviews.Data.Access.Emails;

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
            var signupInterviewTimeslots = await _context.SignupInterviewerTimeslot
                .Include(s => s.SignupInterviewer)
                .Include(s => s.Timeslot)
                .ThenInclude(s => s.EventDate)
                .OrderBy(ve => ve.SignupInterviewer.InterviewerId)
                .ThenBy(ve => ve.Timeslot.EventDate.Date)
                .ThenBy(ve => ve.Timeslot.Time)
                .Where(s => s.Timeslot.IsInterviewer)
                .ToListAsync();

            var groupedEvents = new List<TimeRangeViewModel>();
            var location = "";

            if (signupInterviewTimeslots != null && signupInterviewTimeslots.Count != 0)
            {
                var ints = new List<int>();
                var currentStart = signupInterviewTimeslots.First().Timeslot;
                var currentEnd = signupInterviewTimeslots.First().Timeslot;
                var inperson = signupInterviewTimeslots.First().SignupInterviewer.InPerson;
                var interviewerId = signupInterviewTimeslots.First().SignupInterviewer.InterviewerId;
                var interviewtype = (signupInterviewTimeslots.First().SignupInterviewer.IsBehavioral, signupInterviewTimeslots.First().SignupInterviewer.IsTechnical);
                ints.Add(signupInterviewTimeslots.First().Id);

                for (int i = 1; i < signupInterviewTimeslots.Count; i++)
                {
                    var nextEvent = signupInterviewTimeslots[i].Timeslot;

                    if (currentEnd.Id + 1 == nextEvent.Id
                        && currentEnd.EventDate.Date == nextEvent.EventDate.Date
                        && signupInterviewTimeslots[i].SignupInterviewer.InPerson == inperson
                        && signupInterviewTimeslots[i].SignupInterviewer.InterviewerId == interviewerId
                        && (signupInterviewTimeslots[i].SignupInterviewer.IsBehavioral, signupInterviewTimeslots[i].SignupInterviewer.IsTechnical) == interviewtype)
                    {
                        currentEnd = nextEvent;
                        ints.Add(signupInterviewTimeslots[i].Id);
                    }
                    else
                    {
                        if (signupInterviewTimeslots[i - 1].SignupInterviewer.InPerson)
                        {
                            location = InterviewLocationConstants.InPerson;
                        }
                        else
                        {
                            location = InterviewLocationConstants.Virtual;
                        }
                        var type = "";
                        if (signupInterviewTimeslots[i - 1].SignupInterviewer.IsBehavioral && !signupInterviewTimeslots[i - 1].SignupInterviewer.IsTechnical)
                        {
                            type = InterviewTypesConstants.Behavioral;
                        }
                        else if(!signupInterviewTimeslots[i - 1].SignupInterviewer.IsBehavioral && signupInterviewTimeslots[i - 1].SignupInterviewer.IsTechnical)
                        {
                            type = InterviewTypesConstants.Technical;
                        }
                        else
                        {
                            type = InterviewTypesConstants.Behavioral + "/" + InterviewTypesConstants.Technical;
                        }
                        var name = await _userManager.FindByIdAsync(signupInterviewTimeslots[i - 1].SignupInterviewer.InterviewerId);
                        groupedEvents.Add(new TimeRangeViewModel
                        {
                            Date = currentStart.EventDate.Date,
                            EndTime = currentEnd.Time.AddMinutes(30).ToString(@"h\:mm tt"),
                            StartTime = currentStart.Time.ToString(@"h\:mm tt"),
                            Location = location,
                            Name = name.FirstName + " " + name.LastName,
                            InterviewType = type,
                            TimeslotIds = ints
                        });

                        currentStart = nextEvent;
                        currentEnd = nextEvent;
                        ints = new List<int>
                            {
                                signupInterviewTimeslots[i].Id
                            };
                        inperson = signupInterviewTimeslots[i].SignupInterviewer.InPerson;
                        interviewerId = signupInterviewTimeslots[i].SignupInterviewer.InterviewerId;
                        interviewtype = (signupInterviewTimeslots[i].SignupInterviewer.IsBehavioral, signupInterviewTimeslots[i].SignupInterviewer.IsTechnical);
                    }
                }

                if (inperson)
                {
                    location = InterviewLocationConstants.InPerson;
                }
                else
                {
                    location = InterviewLocationConstants.Virtual;
                }
                var lasttype = "";
                if (interviewtype.IsBehavioral && !interviewtype.IsTechnical)
                {
                    lasttype = InterviewTypesConstants.Behavioral;
                }
                else if (!interviewtype.IsBehavioral && interviewtype.IsTechnical)
                {
                    lasttype = InterviewTypesConstants.Technical;
                }
                else
                {
                    lasttype = InterviewTypesConstants.Behavioral + "/" + InterviewTypesConstants.Technical;
                }
                var user = await _userManager.FindByIdAsync(interviewerId);
                groupedEvents.Add(new TimeRangeViewModel
                {
                    Date = currentStart.EventDate.Date,
                    EndTime = currentEnd.Time.AddMinutes(30).ToString(@"h\:mm tt"),
                    StartTime = currentStart.Time.ToString(@"h\:mm tt"),
                    Location = location,
                    Name = user.FirstName + " " + user.LastName,
                    InterviewType = lasttype,
                    TimeslotIds = ints
                });
            }

            return View(groupedEvents);
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

            var emailTimes = new List<SignupInterviewerTimeslot>();
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
                emailTimes.Add(timeslot);
            }

            ComposeEmail(user, emailTimes);

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

        private async void ComposeEmail(ApplicationUser user, List<SignupInterviewerTimeslot> emailTimes)
        {
            var times = "";
            foreach (SignupInterviewerTimeslot interview in emailTimes)
            {
                times += interview.ToString();
            }

            ASendAnEmail emailer = new InterviewerSignupConfirmation();
            await emailer.SendEmailAsync(_sendGridClient, SubjectLineConstants.InterviewerSignupConfirmation, user.Email, user.FirstName, times);

            string fullName = user.FirstName + " " + user.LastName;

            ASendAnEmail emailNotification = new InterviewerSignupNotification();
            await emailNotification.SendEmailAsync(_sendGridClient, SubjectLineConstants.InterviewerSignupNotification + fullName, CurrentAdmin.Email, fullName, times);
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

        [Authorize(Roles = RolesConstants.InterviewerRole + "," + RolesConstants.AdminRole)]
        public async Task<IActionResult> UserDeleteRange(int[] timeslots)
        {
            // Check if the timeslotIds array is empty or null
            if (timeslots == null || timeslots.Length == 0)
            {
                return NotFound();
            }

            // Get the timeslots to delete
            var timeslotsToDelete = await _context.SignupInterviewerTimeslot
                .Include(x => x.SignupInterviewer)             
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

            if (!timeslotsToDelete.All(e => e.SignupInterviewer.InterviewerId == User.FindFirstValue(ClaimTypes.NameIdentifier)))
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

        [Authorize(Roles = RolesConstants.InterviewerRole + "," + RolesConstants.AdminRole)]
        public async Task<IActionResult> UserDeleteRangeConfirmed(int[] timeslots)
        {

            // Get the timeslots to delete
            var timeslotsToDelete = await _context.SignupInterviewerTimeslot
                .Include(x => x.SignupInterviewer)
                .Where(t => timeslots.Contains(t.Id))
                .ToListAsync();

            if (!timeslotsToDelete.All(e => e.SignupInterviewer.InterviewerId == User.FindFirstValue(ClaimTypes.NameIdentifier)))
            {
                return NotFound();
            }

            // Delete the timeslots
            _context.SignupInterviewerTimeslot.RemoveRange(timeslotsToDelete);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
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
            var timeslotsToDelete = await _context.SignupInterviewerTimeslot
                .Include(x => x.SignupInterviewer)
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


            return View("UserDeleteRange", viewModel);
        }

        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> DeleteRangeConfirmed(int[] timeslots)
        {

            // Get the timeslots to delete
            var timeslotsToDelete = await _context.SignupInterviewerTimeslot
                .Include(x => x.SignupInterviewer)
                .Where(t => timeslots.Contains(t.Id))
                .ToListAsync();

            // Delete the timeslots
            _context.SignupInterviewerTimeslot.RemoveRange(timeslotsToDelete);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "SignupInterviewerTimeslots");
        }
    }
}
