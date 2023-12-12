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
using sp2023_mis421_mockinterviews.Data.Migrations.Data;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Text;

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
                .ThenBy(ve => ve.TimeslotId)
                .Where(s => s.Timeslot.IsInterviewer && s.Timeslot.EventDate.IsActive)
                .ToListAsync();

            var timeRanges = new ControlBreakInterviewer(_userManager);
            var groupedEvents = await timeRanges.ToTimeRanges(signupInterviewTimeslots);

            return View(groupedEvents);
        }

        // GET: SignupInterviewerTimeslots
        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> LunchReport()
        {
            var uniqueSignupInterviewerTimeslots = await _context.SignupInterviewerTimeslot
                .Include(s => s.SignupInterviewer)
                .Include(s => s.Timeslot)
                .ThenInclude(s => s.EventDate)
                .Where(s => s.Timeslot.IsInterviewer && s.Timeslot.EventDate.For221 == For221.n)
                .ToListAsync();

            var groupedSignupInterviewerTimeslots = uniqueSignupInterviewerTimeslots
                .GroupBy(s => new { s.SignupInterviewer.InterviewerId, s.Timeslot.EventDateId })
                .Select(g => g.First()) // Select the first element from each group (unique combination)
                .OrderBy(ve => ve.SignupInterviewer.InterviewerId)
                .ThenBy(ve => ve.TimeslotId)
                .ToList();

            var lunchReport = new LunchReportViewModel();

            if(groupedSignupInterviewerTimeslots.Count != 0)
            {
                var lunchReports = new List<LunchReport>();

                foreach (var uniqueSignupInterviewerTimeslot in groupedSignupInterviewerTimeslots)
                {
                    var signupInterviewer = uniqueSignupInterviewerTimeslot.SignupInterviewer;

                    lunchReports.Add(new LunchReport
                    {
                        Name = signupInterviewer.FirstName + " " + signupInterviewer.LastName,
                        LunchDesire = signupInterviewer.Lunch ?? false,
                        ForDate = uniqueSignupInterviewerTimeslot.Timeslot.EventDate.Date
                    });
                }

                var lunchReportData = groupedSignupInterviewerTimeslots
                    .GroupBy(s => s.Timeslot.EventDateId)
                    .Select(g => new
                    {
                        EventDateId = g.Key,
                        EventDateDate = g.First().Timeslot.EventDate.Date,
                        EventDateName = g.First().Timeslot.EventDate.EventName,
                        LunchCount = g.Count(s => s.SignupInterviewer.Lunch == true)
                    })
                    .OrderBy(g => g.EventDateId)
                    .ToList();

                lunchReport = new LunchReportViewModel
                {
                    LunchReports = lunchReports,
                    Day1TotalLunchCount = lunchReportData[0].LunchCount,
                    Day1Name = $"{lunchReportData[0].EventDateName} ({lunchReportData[0].EventDateDate:M/dd/yyyy})",
                    AnyLunches = true
                };

                if (lunchReportData.Count > 1)
                {
                    lunchReport.Day2TotalLunchCount = lunchReportData[1].LunchCount;
                    lunchReport.Day2Name = $"{lunchReportData[1].EventDateName} ({lunchReportData[1].EventDateDate:M/dd/yyyy})";
                }
            }
            else
            {
                lunchReport = new LunchReportViewModel
                {
                    LunchReports = new List<LunchReport>(),
                    Day1Name = "No Lunches"
                };
            }

            return View("LunchReport", lunchReport);
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
        public async Task<IActionResult> Create()
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.Users
                .Where(x => x.Id == userId)
                .Select(x => new { x.FirstName, x.LastName })
                .FirstOrDefaultAsync();

            var timeslots = new List<Timeslot>();
            if (User.IsInRole(RolesConstants.StudentRole))
            {
                 timeslots = await _context.Timeslot
                    .Where(x => x.IsInterviewer == true)
                    .Include(y => y.EventDate)
                    .Where(x => !_context.SignupInterviewerTimeslot.Any(y => y.TimeslotId == x.Id && y.SignupInterviewer.InterviewerId == userId))
                    .Where(x => x.EventDate.For221 != For221.n && x.EventDate.IsActive == true)
                    .ToListAsync();
            }
            else
            {
                timeslots = await _context.Timeslot
                    .Where(x => x.IsInterviewer == true)
                    .Include(y => y.EventDate)
                    .Where(x => !_context.SignupInterviewerTimeslot.Any(y => y.TimeslotId == x.Id && y.SignupInterviewer.InterviewerId == userId))
                    .Where(x => x.EventDate.For221 != For221.y && x.EventDate.IsActive == true)
                    .ToListAsync();
            }

            var eventdates = await _context.EventDate
                .Where(x => x.IsActive == true)
                .ToListAsync();

            SignupInterviewerTimeslotsViewModel volunteerEventsViewModel = new()
            {
                Timeslots = timeslots,
                SignupInterviewer = new SignupInterviewer
                { 
                    InterviewerId = userId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    IsBehavioral = false,
                    IsTechnical = false,
                    IsCase = false,
                    IsVirtual = false,
                    InPerson = false
                },
                EventDates = eventdates
            };

            if (timeslots.Count == 0)
            {
                volunteerEventsViewModel.SignedUp = true;
            }
            else
            {
                volunteerEventsViewModel.SignedUp = false;
            }

            return View(volunteerEventsViewModel);
        }

        // POST: SignupInterviewerTimeslots/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = RolesConstants.InterviewerRole)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int[] SelectedEventIds1, int[] SelectedEventIds2, [Bind("IsTechnical,IsBehavioral,IsCase,IsVirtual,InPerson")] SignupInterviewer signupInterviewer, bool Lunch )
        {
            int[] SelectedEventIds = SelectedEventIds1.Concat(SelectedEventIds2).ToArray();

            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            var timeslots = new List<Timeslot>();
            var dates = new List<int>();
            if(User.IsInRole(RolesConstants.StudentRole))
            {
                timeslots = await _context.Timeslot
                    .Where(x => x.IsInterviewer == true)
                    .Include(y => y.EventDate)
                    .Where(x => !_context.SignupInterviewerTimeslot.Any(y => y.TimeslotId == x.Id && y.SignupInterviewer.InterviewerId == userId) &&
                        x.EventDate.IsActive == true &&
                        x.EventDate.For221 != For221.n)
                    .ToListAsync();

                dates = timeslots
                    .Where(x => x.EventDate.For221 != For221.n && SelectedEventIds.Contains(x.Id))
                    .Select(t => t.EventDate.Id)
                    .Distinct()
                    .ToList();
            }
            else
            {
                timeslots = await _context.Timeslot
                    .Where(x => x.IsInterviewer == true)
                    .Include(y => y.EventDate)
                    .Where(x => !_context.SignupInterviewerTimeslot.Any(y => y.TimeslotId == x.Id && y.SignupInterviewer.InterviewerId == userId) &&
                        x.EventDate.IsActive == true &&
                        x.EventDate.For221 != For221.y)
                    .ToListAsync();

                dates = timeslots
                    .Where(x => x.EventDate.For221 != For221.y && SelectedEventIds.Contains(x.Id))
                    .Select(t => t.EventDate.Id)
                    .Distinct()
                    .ToList();
            }

            SignupInterviewerTimeslotsViewModel volunteerEventsViewModel = new()
            {
                Timeslots = timeslots,
                SignupInterviewer = new SignupInterviewer
                {
                    InterviewerId = userId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    IsBehavioral = false,
                    IsTechnical = false,
                    IsCase = false,
                    IsVirtual = false,
                    InPerson = false
                }
            };

            if (!signupInterviewer.IsTechnical && !signupInterviewer.IsBehavioral && !signupInterviewer.IsCase)
            {
                ModelState.AddModelError("SignupInterviewer.IsTechnical", "Please select at least one checkbox");
                
                return View(volunteerEventsViewModel);
            }

            if (!signupInterviewer.IsVirtual && !signupInterviewer.InPerson)
            {
                ModelState.AddModelError("SignupInterviewer.InPerson", "Please select at least one checkbox");

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
                foreach(int date in dates)
                {
                    if (!_context.LocationInterviewer.Any(x => x.InterviewerId == existingSignupInterviewer.InterviewerId && x.EventDateId == date))
                    {
                        var interviewerPreference = "";
                        if (existingSignupInterviewer.InPerson && existingSignupInterviewer.IsVirtual)
                        {
                            interviewerPreference = InterviewLocationConstants.InPerson + "/" + InterviewLocationConstants.IsVirtual;
                        }
                        else if(existingSignupInterviewer.InPerson)
                        {
                            interviewerPreference = InterviewLocationConstants.InPerson;
                        }
                        else if(existingSignupInterviewer.IsVirtual)
                        {
                            interviewerPreference = InterviewLocationConstants.IsVirtual;
                        }

                        _context.Add(new LocationInterviewer
                        {
                            LocationId = null,
                            InterviewerId = userId,
                            LocationPreference = interviewerPreference,
                            EventDateId = date
                        });

                        await _context.SaveChangesAsync();
                    }
                }
                
            }
            else
            {
                var interviewtype = "";
                if(signupInterviewer.IsBehavioral && signupInterviewer.IsTechnical && signupInterviewer.IsCase)
                {
                    interviewtype = InterviewTypeConstants.Behavioral + ", " + InterviewTypeConstants.Technical + ", " + InterviewTypeConstants.Case;
                }
                else if(signupInterviewer.IsBehavioral && signupInterviewer.IsTechnical)
                {
                    interviewtype = InterviewTypeConstants.Behavioral + ", " + InterviewTypeConstants.Technical;
                }
                else if(signupInterviewer.IsBehavioral && signupInterviewer.IsCase)
                {
                    interviewtype = InterviewTypeConstants.Behavioral + ", " + InterviewTypeConstants.Case;
                }
                else if(signupInterviewer.IsTechnical && signupInterviewer.IsCase)
                {
                    interviewtype = InterviewTypeConstants.Technical + ", " + InterviewTypeConstants.Case;
                }
                else if(signupInterviewer.IsBehavioral)
                {
                    interviewtype = InterviewTypeConstants.Behavioral;
                }
                else if(signupInterviewer.IsTechnical)
                {
                    interviewtype = InterviewTypeConstants.Technical;
                }
                else if(signupInterviewer.IsCase)
                {
                    interviewtype = InterviewTypeConstants.Case;
                }

                if(Lunch == null)
                {
                    Lunch = false;
                }

                post = new SignupInterviewer
                {
                    InterviewerId = userId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    InPerson = signupInterviewer.InPerson,
                    IsVirtual = signupInterviewer.IsVirtual,
                    IsBehavioral = signupInterviewer.IsBehavioral,
                    IsTechnical = signupInterviewer.IsTechnical,
                    IsCase = signupInterviewer.IsCase,
                    Lunch = Lunch,
                    InterviewType = interviewtype
                };

                var interviewerPreference = "";
                if (signupInterviewer.InPerson && signupInterviewer.IsVirtual)
                {
                    interviewerPreference = InterviewLocationConstants.InPerson + "/" + InterviewLocationConstants.IsVirtual;
                }
                else if (signupInterviewer.InPerson)
                {
                    interviewerPreference = InterviewLocationConstants.InPerson;
                }
                else if (signupInterviewer.IsVirtual)
                {
                    interviewerPreference = InterviewLocationConstants.IsVirtual;
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
                            LocationPreference = interviewerPreference,
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

            var sortedTimes = emailTimes
                .OrderBy(ve => ve.TimeslotId)
                .ToList();

            ComposeEmail(user, sortedTimes);

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
			var timeRanges = new ControlBreakInterviewer(_userManager);
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

			ASendAnEmail emailer = new InterviewerSignupConfirmation();
            await emailer.SendEmailAsync(_sendGridClient, SubjectLineConstants.InterviewerSignupConfirmation, user.Email, user.FirstName, times, calendarEvents);

            string fullName = user.FirstName + " " + user.LastName;

            ASendAnEmail emailNotification = new InterviewerSignupNotification();
            await emailNotification.SendEmailAsync(_sendGridClient, SubjectLineConstants.InterviewerSignupNotification + fullName, CurrentAdmin.Email, fullName, times, null);
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

            if (!timeslotsToDelete.All(e => e.SignupInterviewer.InterviewerId == User.FindFirstValue(ClaimTypes.NameIdentifier)) && !User.IsInRole(RolesConstants.AdminRole))
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
                .Include(x=>x.Timeslot)
                .Where(t => timeslots.Contains(t.Id))
                .ToListAsync();

            if (!timeslotsToDelete.All(e => e.SignupInterviewer.InterviewerId == User.FindFirstValue(ClaimTypes.NameIdentifier)) && !User.IsInRole(RolesConstants.AdminRole))
            {
                return NotFound();
            }

            // Delete the timeslots
            _context.SignupInterviewerTimeslot.RemoveRange(timeslotsToDelete);
                        
            await _context.SaveChangesAsync();

            if (!_context.SignupInterviewerTimeslot.Any(x => x.SignupInterviewer.InterviewerId == timeslotsToDelete[0].SignupInterviewer.InterviewerId && x.Timeslot.EventDateId == timeslotsToDelete[0].Timeslot.EventDateId))
            {
                var locationInterviewersToDelete = _context.LocationInterviewer
                    .Where(li => li.InterviewerId == timeslotsToDelete[0].SignupInterviewer.InterviewerId && li.EventDateId == timeslotsToDelete[0].Timeslot.EventDateId);

                _context.LocationInterviewer.RemoveRange(locationInterviewersToDelete);
                await _context.SaveChangesAsync();

                if(!_context.SignupInterviewerTimeslot.Any(x => x.SignupInterviewer.InterviewerId == timeslotsToDelete[0].SignupInterviewer.InterviewerId))
                {
                    var signupInterviewersToDelete = _context.SignupInterviewer
                    .Where(li => li.InterviewerId == timeslotsToDelete[0].SignupInterviewer.InterviewerId);

                    _context.SignupInterviewer.RemoveRange(signupInterviewersToDelete);
                    await _context.SaveChangesAsync();
                }
            }
            
            if(User.IsInRole(RolesConstants.AdminRole))
            {
                return RedirectToAction("Index", "SignupInterviewerTimeslots");
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
