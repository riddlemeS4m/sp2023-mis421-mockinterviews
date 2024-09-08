﻿using System;
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
using sp2023_mis421_mockinterviews.Data.Access.Emails;
using sp2023_mis421_mockinterviews.Data.Migrations.Data;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Text;
using SendGrid.Helpers.Errors.Model;
using sp2023_mis421_mockinterviews.Interfaces.IServices;

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
                .Include(s => s.InterviewerSignup)
                .Include(s => s.Timeslot)
                .ThenInclude(s => s.Event)
                .OrderBy(ve => ve.InterviewerSignup.InterviewerId)
                .ThenBy(ve => ve.TimeslotId)
                .Where(s => s.Timeslot.IsInterviewer && s.Timeslot.Event.IsActive)
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
                .Include(s => s.InterviewerSignup)
                .Include(s => s.Timeslot)
                .ThenInclude(s => s.Event)
                .Where(s => s.Timeslot.IsInterviewer && 
                    s.Timeslot.Event.For221 == For221.n &&
                    s.Timeslot.Event.IsActive)
                .ToListAsync();

            var groupedSignupInterviewerTimeslots = uniqueSignupInterviewerTimeslots
                .GroupBy(s => new { s.InterviewerSignup.InterviewerId, s.Timeslot.EventId })
                .Select(g => g.First()) // Select the first element from each group (unique combination)
                .OrderBy(ve => ve.InterviewerSignup.InterviewerId)
                .ThenBy(ve => ve.TimeslotId)
                .ToList();

            var lunchReport = new LunchReportViewModel();

            if(groupedSignupInterviewerTimeslots.Count != 0)
            {
                var lunchReports = new List<LunchReport>();

                foreach (var uniqueSignupInterviewerTimeslot in groupedSignupInterviewerTimeslots)
                {
                    var signupInterviewer = uniqueSignupInterviewerTimeslot.InterviewerSignup;

                    lunchReports.Add(new LunchReport
                    {
                        Name = signupInterviewer.FirstName + " " + signupInterviewer.LastName,
                        LunchDesire = signupInterviewer.Lunch ?? false,
                        ForDate = uniqueSignupInterviewerTimeslot.Timeslot.Event.Date
                    });
                }

                var lunchReportData = groupedSignupInterviewerTimeslots
                    .GroupBy(s => s.Timeslot.EventId)
                    .Select(g => new
                    {
                        EventDateId = g.Key,
                        EventDateDate = g.First().Timeslot.Event.Date,
                        EventDateName = g.First().Timeslot.Event.Name,
                        LunchCount = g.Count(s => s.InterviewerSignup.Lunch == true)
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
                .Include(s => s.InterviewerSignup)
                .Include(s => s.Timeslot)
                .ThenInclude(s => s.Event)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (signupInterviewerTimeslot == null)
            {
                return NotFound();
            }

            return View(signupInterviewerTimeslot);
        }

        // GET: SignupInterviewerTimeslots/Create
        //[Authorize(Roles = RolesConstants.InterviewerRole)]
        [AllowAnonymous]
        public async Task<IActionResult> Create()
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var company = "";
            var email = "";
            var firstName = "";
            var lastName = "";

            var timeslots = new List<Timeslot>();

            if(string.IsNullOrEmpty(userId))
            {
                timeslots = await _context.Timeslot
                    .Where(x => x.IsInterviewer)
                    .Include(y => y.Event)
                    .Where(x => x.Event.IsActive && x.Event.For221 != For221.y)
                    .ToListAsync();                
            }
            else
            {
                var theirClass = GetClass(User.IsInRole(RolesConstants.StudentRole));
                timeslots = await _context.Timeslot
                    .Where(x => x.IsInterviewer)
                    .Include(y => y.Event)
                    .Where(x => !_context.SignupInterviewerTimeslot.Any(y => y.TimeslotId == x.Id && y.InterviewerSignup.InterviewerId == userId) &&
                        x.Event.IsActive &&
                        x.Event.For221 != theirClass)
                    .ToListAsync();

                var user = await _userManager.FindByIdAsync(userId);
                company = user.Company;
                email = user.Email;
                firstName = user.FirstName;
                lastName = user.LastName;
            }

            var dates = await _context.EventDate
                    .Where(x => x.IsActive)
                    .ToListAsync();

            

            SignupInterviewerTimeslotsViewModel volunteerEventsViewModel = new()
            {
                Timeslots = timeslots,
                SignupInterviewer = new InterviewerSignup
                { 
                    IsBehavioral = false,
                    IsTechnical = false,
                    IsCase = false,
                    IsVirtual = false,
                    InPerson = false
                },
                EventDates = dates,
                Company = company,
                Email = email,
                FirstName = firstName,
                LastName = lastName
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
        //[Authorize(Roles = RolesConstants.InterviewerRole)]
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int[] SelectedEventIds1, int[] SelectedEventIds2, 
            [Bind("IsTechnical,IsBehavioral,IsCase,IsVirtual,InPerson")] InterviewerSignup signupInterviewer, bool Lunch,
            string Email, string Company, string FirstName, string LastName)
        {
            int[] SelectedEventIds = SelectedEventIds1.Concat(SelectedEventIds2).ToArray();

            var timeslots = await _context.Timeslot
                .Where(x => x.IsInterviewer)
                .Include(y => y.Event)
                .Where(x => x.Event.IsActive && x.Event.For221 != For221.y)
                .ToListAsync();

            //prepare vm in case of errors
            SignupInterviewerTimeslotsViewModel vm = new()
            {
                Timeslots = timeslots,
                SignupInterviewer = new InterviewerSignup
                {
                    IsBehavioral = false,
                    IsTechnical = false,
                    IsCase = false,
                    IsVirtual = false,
                    InPerson = false
                },
                EventDates = await _context.EventDate
                    .Where(x => x.IsActive)
                    .ToListAsync()
            };

            if (string.IsNullOrEmpty(FirstName))
            {
                ModelState.AddModelError("FirstName", "First Name is required.");
            }

            if (string.IsNullOrEmpty(LastName))
            {
                ModelState.AddModelError("LastName", "Last Name is required.");
            }

            if (string.IsNullOrEmpty(Email))
            {
                ModelState.AddModelError("Email", "Email is required.");
            }

            if (string.IsNullOrEmpty(Company))
            {
                ModelState.AddModelError("Company", "Company is required.");
            }

            // Check whether at least one interview type is selected
            if (!signupInterviewer.IsTechnical && !signupInterviewer.IsBehavioral && !signupInterviewer.IsCase)
            {
                ModelState.AddModelError("InterviewerSignup.IsTechnical", "Please select at least one checkbox");
            }

            // Check whether at least one timeslot is selected
            if (SelectedEventIds == null || SelectedEventIds.Length == 0)
            {
                ModelState.AddModelError("SelectedEventIds", "Please select at least one timeslot");
            }

            if(ModelState.ErrorCount > 0)
            {
                return View(vm);
            }

            var user = await _userManager.FindByEmailAsync(Email);
            if (user == null)
            {
                user = new ApplicationUser { FirstName = FirstName, LastName = LastName, Email = Email, UserName = Email, Company = Company };
                var result = await _userManager.CreateAsync(user, $"{FirstName}Fall2024!");

                user = await _userManager.FindByEmailAsync(Email) ?? throw new Exception($"User with email {Email} was not successfully created.");
                var roleResult = await _userManager.AddToRoleAsync(user, RolesConstants.InterviewerRole);
            }

            if (user.Company == null)
            {
                user.Company = Company;
                await _userManager.UpdateAsync(user);
            }

            string userId = user.Id;
            var userName = await _userManager.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.FirstName, u.LastName, u.Email })
                .FirstOrDefaultAsync();
            var theirClass = GetClass(User.IsInRole(RolesConstants.StudentRole));
            timeslots = await _context.Timeslot
                .Where(x => x.IsInterviewer)
                .Include(y => y.Event)
                .Where(x => !_context.SignupInterviewerTimeslot.Any(y => y.TimeslotId == x.Id && y.InterviewerSignup.InterviewerId == userId) &&
                    x.Event.IsActive &&
                    x.Event.For221 != theirClass)
                .ToListAsync();
            var dates = timeslots
                .Where(x => x.Event.For221 != theirClass && SelectedEventIds.Contains(x.Id))
                .Select(t => t.Event.Id)
                .Distinct()
                .ToList();

            //does the interview already have an existing signup?
            var existingSignupInterviewer = await _context.SignupInterviewer.FirstOrDefaultAsync(si =>
                    si.InPerson == signupInterviewer.InPerson &&
                    si.InterviewerId == userId);

            var post = new InterviewerSignup();

            //if they do, make sure they don't need a new location
            if (existingSignupInterviewer != null)
            {
                post = existingSignupInterviewer;
                foreach(int date in dates)
                {
                    if (!_context.LocationInterviewer.Any(x => x.InterviewerId == existingSignupInterviewer.InterviewerId && x.EventId == date))
                    {
                        var interviewerPreference = GetLocation(existingSignupInterviewer.InPerson);

                        _context.Add(new InterviewerLocation
                        {
                            LocationId = null,
                            InterviewerId = userId,
                            Preference = interviewerPreference,
                            EventId = date
                        });

                        await _context.SaveChangesAsync();
                    }
                }
                
            } //if they don't, make a new interview signup (most of the time, this will run)
            else
            {
                var interviewtype = GetType(signupInterviewer.IsBehavioral, signupInterviewer.IsTechnical, signupInterviewer.IsCase);

                post = new InterviewerSignup
                {
                    InterviewerId = userId,
                    FirstName = userName.FirstName,
                    LastName = userName.LastName,
                    InPerson = signupInterviewer.InPerson,
                    IsVirtual = !signupInterviewer.InPerson,
                    IsBehavioral = signupInterviewer.IsBehavioral,
                    IsTechnical = signupInterviewer.IsTechnical,
                    IsCase = signupInterviewer.IsCase,
                    Lunch = Lunch,
                    Type = interviewtype
                };

                var interviewerPreference = GetLocation(signupInterviewer.InPerson);

                //make signup
                _context.Add(post);
                await _context.SaveChangesAsync();

                //make locations
                foreach(int date in dates)
                {
                    _context.Add(new InterviewerLocation
                    {
                        LocationId = null,
                        InterviewerId = userId,
                        Preference = interviewerPreference,
                        EventId = date
                    });
                    await _context.SaveChangesAsync();
                }
            }

            var emailTimes = new List<InterviewerTimeslot>();

            //add sits
            foreach (int id in SelectedEventIds)
            {
                var bothTimeslots = new List<InterviewerTimeslot>();

                var timeslotOne = new InterviewerTimeslot 
                { 
                    TimeslotId = id, 
                    InterviewerSignupId = post.Id 
                };

                var timeslotTwo = new InterviewerTimeslot
                {
                    TimeslotId = id + 1,
                    Timeslot = await _context.Timeslot.FindAsync(id + 1),
                    InterviewerSignupId = post.Id
                };

                bothTimeslots.Add(timeslotOne);
                bothTimeslots.Add(timeslotTwo);

                _context.AddRange(bothTimeslots);
                await _context.SaveChangesAsync();

                emailTimes.Add(timeslotOne);
                emailTimes.Add(timeslotTwo);
            }

            //prepare and send email
            var sortedTimes = emailTimes
                .OrderBy(ve => ve.TimeslotId)
                .ToList();

            await ComposeEmail(userName.FirstName, userName.LastName, userName.Email, sortedTimes);

            return RedirectToAction("Index", "Home");
        }

        // GET: SignupInterviewerTimeslots/Edit/5
        [Authorize(Roles = RolesConstants.InterviewerRole + "," + RolesConstants.AdminRole)]
        public async Task<IActionResult> Edit(int id)
        {
            if (id == 0 || _context.SignupInterviewerTimeslot == null)
            {
                return BadRequest();
            }

            var signupInterviewer = await _context.SignupInterviewer.FindAsync(id);
            if (signupInterviewer == null)
            {
                return NotFound();
            }

            if(!User.IsInRole(RolesConstants.AdminRole) && signupInterviewer.InterviewerId != _userManager.GetUserId(User))
            {
                return BadRequest(new ForbiddenException());
            }

            var theirTimeslots = await _context.SignupInterviewerTimeslot
                .Include(x => x.Timeslot)
                .ThenInclude(x => x.Event)
                .Where(x => x.InterviewerSignupId == signupInterviewer.Id)
                .Select(x => x.TimeslotId)
                .ToListAsync();

            var timeslots = await _context.Timeslot
                .Include(y => y.Event)
                .Where(x => x.IsInterviewer &&
                    x.Event.IsActive)
                .ToListAsync();

            var dates = await _context.EventDate
                .Where(x => x.IsActive)
                .ToListAsync();

            var shouldBeChecked = new Dictionary<int, bool>();

            foreach (var timeslot in timeslots)
            {
                shouldBeChecked.Add(timeslot.Id, theirTimeslots.Contains(timeslot.Id));
            }

            var vm = new SignupInterviewerTimeslotsViewModel()
            {
                Timeslots = timeslots,
                EventDates = dates,
                SignupInterviewer = signupInterviewer,
                EventDateDictionary = shouldBeChecked,
                SignedUp = false
            };

            return View(vm);
        }

        // POST: SignupInterviewerTimeslots/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        //currently not used -sam
        [Authorize(Roles = RolesConstants.InterviewerRole + "," + RolesConstants.AdminRole)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int[] SelectedEventIds1, int[] SelectedEventIds2, int[] SelectedEventIds3, int[] SelectedEventIds4,
            [Bind("Id,InterviewerId,IsTechnical,IsBehavioral,IsCase,IsVirtual,InPerson")] InterviewerSignup signupInterviewer, bool Lunch)
        {
            if((SelectedEventIds1 == null && SelectedEventIds2 == null && SelectedEventIds3 == null && SelectedEventIds4 == null) || signupInterviewer == null || Lunch == null)
            {
                return NotFound();
            }

            if (!User.IsInRole(RolesConstants.AdminRole) && signupInterviewer.InterviewerId != _userManager.GetUserId(User))
            {
                return BadRequest(new ForbiddenException());
            }

            int[] SelectedEventIds = SelectedEventIds1
                .Concat(SelectedEventIds2)
                .Concat(SelectedEventIds3)
                .Concat(SelectedEventIds4)
                .ToArray();
            var user = await _userManager.FindByIdAsync(signupInterviewer.InterviewerId);
            var theirClass = GetClass(User.IsInRole(RolesConstants.StudentRole));
            var timeslots = await _context.Timeslot
                .Where(x => x.IsInterviewer)
                .Include(y => y.Event)
                .Where(x => _context.SignupInterviewerTimeslot.Any(y => y.TimeslotId == x.Id && y.InterviewerSignup.InterviewerId == signupInterviewer.InterviewerId) &&
                    x.Event.IsActive &&
                    x.Event.For221 != theirClass)
                .ToListAsync();
            var dates = timeslots
                .Where(x => x.Event.For221 != theirClass && SelectedEventIds.Contains(x.Id))
                .Select(t => t.Event.Id)
                .Distinct()
                .ToList();

            //eventdate dictionary setup
            var timeslotsED = await _context.Timeslot
                .Include(y => y.Event)
                .Where(x => x.IsInterviewer &&
                    x.Event.IsActive)
                .ToListAsync();

            var shouldBeChecked = new Dictionary<int, bool>();

            foreach (var timeslot in timeslotsED)
            {
                shouldBeChecked.Add(timeslot.Id, timeslots.Contains(timeslot));
            }

            SignupInterviewerTimeslotsViewModel vm = new()
            {
                Timeslots = timeslotsED,
                SignupInterviewer = new InterviewerSignup
                {
                    InterviewerId = signupInterviewer.InterviewerId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    IsBehavioral = false,
                    IsTechnical = false,
                    IsCase = false,
                    IsVirtual = false,
                    InPerson = false
                },
                EventDates = await _context.EventDate
                    .Where(x => x.IsActive)
                    .ToListAsync(),
                EventDateDictionary = shouldBeChecked,
                SignedUp = false
            };

            // Check whether at least one checkbox is selected
            if (!signupInterviewer.IsTechnical && !signupInterviewer.IsBehavioral && !signupInterviewer.IsCase)
            {
                ModelState.AddModelError("InterviewerSignup.IsTechnical", "Please select at least one checkbox");
                return View(vm);
            }

            // Check whether at least one timeslot is selected
            if (SelectedEventIds == null || SelectedEventIds.Length == 0)
            {
                ModelState.AddModelError("SelectedEventIds", "Please select at least one timeslot");
                return View(vm);
            }

            //Get old interviewer signup
            var existingSignupInterviewer = await _context.SignupInterviewer.FirstOrDefaultAsync(x => x.Id == signupInterviewer.Id);
            var interviewtype = GetType(signupInterviewer.IsBehavioral, signupInterviewer.IsTechnical, signupInterviewer.IsCase);
            var interviewerPreference = GetLocation(signupInterviewer.InPerson);

            _context.Entry(existingSignupInterviewer).CurrentValues.SetValues(new
            {
                InterviewerId = signupInterviewer.InterviewerId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                InPerson = signupInterviewer.InPerson,
                IsVirtual = !signupInterviewer.InPerson,
                IsBehavioral = signupInterviewer.IsBehavioral,
                IsTechnical = signupInterviewer.IsTechnical,
                IsCase = signupInterviewer.IsCase,
                Lunch = Lunch,
                InterviewType = interviewtype
            });

            await _context.SaveChangesAsync();

            //make sure new locations aren't needed
            foreach (int date in dates)
            {
                if (!_context.LocationInterviewer.Any(x => x.InterviewerId == existingSignupInterviewer.InterviewerId && 
                    x.EventId == date))
                {
                    _context.Add(new InterviewerLocation
                    {
                        LocationId = null,
                        InterviewerId = signupInterviewer.InterviewerId,
                        Preference = interviewerPreference,
                        EventId = date
                    });
                    await _context.SaveChangesAsync();
                }
            }

            var existingSits = await _context.SignupInterviewerTimeslot
                .Where(x => x.InterviewerSignupId == signupInterviewer.Id)
                .ToListAsync();

            //add any new timeslots that were checked
            foreach (int id in SelectedEventIds)
            {
                var timeslot = new InterviewerTimeslot
                {
                    TimeslotId = id,
                    InterviewerSignupId = signupInterviewer.Id
                };

                if(!existingSits.Contains(timeslot))
                {
                    _context.Add(timeslot);
                }
                await _context.SaveChangesAsync();
            }

            //remove any timeslots that were unchecked
            foreach(InterviewerTimeslot sit in existingSits)
            {
                if(!SelectedEventIds.Contains(sit.Id))
                {
                    _context.Remove(sit);
                }
                await _context.SaveChangesAsync();
            }

            if (User.IsInRole(RolesConstants.AdminRole))
            {
                return RedirectToAction("Index", "SignupInterviewers");
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> CreateForInterviewer()
        {
            var timeslots = await _context.Timeslot
                   .Where(x => x.IsInterviewer)
                   .Include(y => y.Event)
                   .Where(x => x.Event.IsActive)
                   .ToListAsync();

            var eventdates = await _context.EventDate
                .Where(x => x.IsActive)
                .ToListAsync();

            var users = await _userManager.GetUsersInRoleAsync(RolesConstants.InterviewerRole);

            var interviewers = users
                .Select(x => new SelectListItem 
                {
                    Value = x.Id,
                    Text = x.FirstName + " " + x.LastName
                })
                .OrderBy(x => x.Text)
                .ToList();

            SignupInterviewerTimeslotsViewModel vm = new()
            {
                Timeslots = timeslots,
                SignupInterviewer = new InterviewerSignup
                {
                    InterviewerId = "",
                    FirstName = "",
                    LastName = "",
                    IsBehavioral = false,
                    IsTechnical = false,
                    IsCase = false,
                    IsVirtual = false,
                    InPerson = false
                },
                EventDates = eventdates,
                Interviewers = interviewers,
                SignedUp = false
            };

            return View(vm);
        }

        [HttpPost]
        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> CreateForInterviewer(int[] SelectedEventIds1, int[] SelectedEventIds2, int[] SelectedEventIds3, int[] SelectedEventIds4,
            [Bind("IsTechnical,IsBehavioral,IsCase,IsVirtual,InPerson")] InterviewerSignup signupInterviewer, bool Lunch, string InterviewerId)
        {
            if((SelectedEventIds1 == null && SelectedEventIds2 == null && SelectedEventIds3 == null && SelectedEventIds4 == null) || signupInterviewer == null || Lunch == null)
            {
                return NotFound();
            }

            if(InterviewerId == null || InterviewerId == "")
            {
                throw new Exception("Interviewer Id was not provided.");
            }

            int[] SelectedEventIds = SelectedEventIds1
                .Concat(SelectedEventIds2)
                .Concat(SelectedEventIds3)
                .Concat(SelectedEventIds4)
                .ToArray();

            var timeslots = await _context.Timeslot
                   .Where(x => x.IsInterviewer)
                   .Include(y => y.Event)
                   .Where(x => x.Event.IsActive)
                   .ToListAsync();

            var eventdates = await _context.EventDate
                .Where(x => x.IsActive)
                .ToListAsync();

            var users = await _userManager.GetUsersInRoleAsync(RolesConstants.InterviewerRole);

            var interviewers = users
                .Select(x => new SelectListItem
                {
                    Value = x.Id,
                    Text = x.FirstName + x.LastName
                })
                .OrderBy(x => x.Text)
                .ToList();

            var dates = timeslots
                .Where(x => SelectedEventIds.Contains(x.Id))
                .Select(t => t.Event.Id)
                .Distinct()
                .ToList();

            var user = await _userManager.Users
                .Where(x => x.Id == InterviewerId)
                .FirstOrDefaultAsync();

            SignupInterviewerTimeslotsViewModel vm = new()
            {
                Timeslots = timeslots,
                SignupInterviewer = new InterviewerSignup
                {
                    InterviewerId = "",
                    FirstName = "",
                    LastName = "",
                    IsBehavioral = false,
                    IsTechnical = false,
                    IsCase = false,
                    IsVirtual = false,
                    InPerson = false
                },
                EventDates = eventdates,
                Interviewers = interviewers,
                SignedUp = false
            };

            if (!signupInterviewer.IsTechnical && !signupInterviewer.IsBehavioral && !signupInterviewer.IsCase)
            {
                ModelState.AddModelError("InterviewerSignup.IsTechnical", "Please select at least one checkbox");
                return View(vm);
            }

            // Check whether at least one timeslot is selected
            if (SelectedEventIds == null || SelectedEventIds.Length == 0)
            {
                ModelState.AddModelError("SelectedEventIds", "Please select at least one timeslot");
                return View(vm);
            }

            var existingSignupInterviewer = await _context.SignupInterviewer.FirstOrDefaultAsync(si =>
                    si.IsVirtual == signupInterviewer.IsVirtual &&
                    si.InPerson == signupInterviewer.InPerson &&
                    si.InterviewerId == InterviewerId);

            InterviewerSignup post;
            if (existingSignupInterviewer != null)
            {
                post = existingSignupInterviewer;
                foreach (int date in dates)
                {
                    if (!_context.LocationInterviewer.Any(x => x.InterviewerId == existingSignupInterviewer.InterviewerId && x.EventId == date))
                    {
                        var interviewerPreference = "";
                        if (existingSignupInterviewer.InPerson && existingSignupInterviewer.IsVirtual) {
                            interviewerPreference = InterviewLocationConstants.InPerson + "/" + InterviewLocationConstants.IsVirtual;
                        } else if (existingSignupInterviewer.InPerson)  {
                            interviewerPreference = InterviewLocationConstants.InPerson;
                        } else if (existingSignupInterviewer.IsVirtual)  {
                            interviewerPreference = InterviewLocationConstants.IsVirtual;
                        }

                        _context.Add(new InterviewerLocation
                        {
                            LocationId = null,
                            InterviewerId = InterviewerId,
                            Preference = interviewerPreference,
                            EventId = date
                        });

                        await _context.SaveChangesAsync();
                    }
                }
            }
            else
            {
                var interviewtype = "";
                if (signupInterviewer.IsBehavioral && signupInterviewer.IsTechnical && signupInterviewer.IsCase) {
                    interviewtype = InterviewTypeConstants.Behavioral + ", " + InterviewTypeConstants.Technical + ", " + InterviewTypeConstants.Case;
                } else if (signupInterviewer.IsBehavioral && signupInterviewer.IsTechnical) {
                    interviewtype = InterviewTypeConstants.Behavioral + ", " + InterviewTypeConstants.Technical;
                } else if (signupInterviewer.IsBehavioral && signupInterviewer.IsCase) {
                    interviewtype = InterviewTypeConstants.Behavioral + ", " + InterviewTypeConstants.Case;
                } else if (signupInterviewer.IsTechnical && signupInterviewer.IsCase) {
                    interviewtype = InterviewTypeConstants.Technical + ", " + InterviewTypeConstants.Case;
                } else if (signupInterviewer.IsBehavioral) {
                    interviewtype = InterviewTypeConstants.Behavioral;
                } else if (signupInterviewer.IsTechnical) {
                    interviewtype = InterviewTypeConstants.Technical;
                } else if (signupInterviewer.IsCase) {
                    interviewtype = InterviewTypeConstants.Case;
                }

                if (Lunch == null)
                {
                    Lunch = false;
                }

                post = new InterviewerSignup
                {
                    InterviewerId = InterviewerId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    InPerson = signupInterviewer.InPerson,
                    IsVirtual = signupInterviewer.IsVirtual,
                    IsBehavioral = signupInterviewer.IsBehavioral,
                    IsTechnical = signupInterviewer.IsTechnical,
                    IsCase = signupInterviewer.IsCase,
                    Lunch = Lunch,
                    Type = interviewtype
                };

                var interviewerPreference = "";
                if (signupInterviewer.InPerson && signupInterviewer.IsVirtual) {
                    interviewerPreference = InterviewLocationConstants.InPerson + "/" + InterviewLocationConstants.IsVirtual;
                } else if (signupInterviewer.InPerson) {
                    interviewerPreference = InterviewLocationConstants.InPerson;
                } else if (signupInterviewer.IsVirtual) {
                    interviewerPreference = InterviewLocationConstants.IsVirtual;
                }

                if (ModelState.IsValid)
                {
                    _context.Add(post);
                    await _context.SaveChangesAsync();

                    foreach (int date in dates)
                    {
                        _context.Add(new InterviewerLocation
                        {
                            LocationId = null,
                            InterviewerId = InterviewerId,
                            Preference = interviewerPreference,
                            EventId = date
                        });
                        await _context.SaveChangesAsync();
                    }
                }
            }

            var emailTimes = new List<InterviewerTimeslot>();
            foreach (int id in SelectedEventIds)
            {

                var timeslot = new InterviewerTimeslot
                {
                    TimeslotId = id,
                    InterviewerSignupId = post.Id
                };

                if (ModelState.IsValid)
                {
                    _context.Add(timeslot);
                    await _context.SaveChangesAsync();
                }
                emailTimes.Add(timeslot);
            }

            //var sortedTimes = emailTimes
            //    .OrderBy(ve => ve.TimeslotId)
            //    .ToList();

            //ComposeEmail(user, sortedTimes);

            return RedirectToAction("Index", "SignupInterviewers");
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
                .Include(s => s.InterviewerSignup)
                .Include(s => s.Timeslot).ThenInclude(y => y.Event)
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
                return Problem("Entity set 'MockInterviewDataDbContext.InterviewerTimeslot'  is null.");
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

        private async Task ComposeEmail(string fn, string ln, string email, List<InterviewerTimeslot> emailTimes)
        {
			var timeRanges = new ControlBreakInterviewer(_userManager);
			var groupedEvents = await timeRanges.ToTimeRanges(emailTimes);
            List<string> calendarEvents = new();

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
            await emailer.SendEmailAsync(_sendGridClient, "Interviewer Sign-Up Confirmation", email, fn, times, calendarEvents);

            string fullName = fn + " " + ln;

            ASendAnEmail emailNotification = new InterviewerSignupNotification();
            await emailNotification.SendEmailAsync(_sendGridClient, "Interviewer Sign-Up Notification: " + fullName, SuperUser.Email, fullName, times, null);
        }

        [Authorize(Roles = RolesConstants.InterviewerRole)]
        public async Task<IActionResult> UserDelete(int? id)
        {
            if (id == null || _context.SignupInterviewerTimeslot == null)
            {
                return NotFound();
            }

            var signupInterviewerTimeslot = await _context.SignupInterviewerTimeslot
                .Include(s => s.InterviewerSignup)
                .Include(s => s.Timeslot).ThenInclude(y => y.Event)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (signupInterviewerTimeslot == null)
            {
                return NotFound();
            }

            if (signupInterviewerTimeslot.InterviewerSignup.InterviewerId != User.FindFirstValue(ClaimTypes.NameIdentifier))
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
                return Problem("Entity set 'MockInterviewDataDbContext.InterviewerTimeslot'  is null.");
            }
            var signupInterviewerTimeslot = await _context.SignupInterviewerTimeslot
                .Include(s => s.InterviewerSignup)
                .Include(s => s.Timeslot)
                .ThenInclude(y => y.Event)
                .FirstOrDefaultAsync(m => m.Id == id);
            ;
            if (signupInterviewerTimeslot != null && signupInterviewerTimeslot.InterviewerSignup.InterviewerId == User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                _context.SignupInterviewerTimeslot.Remove(signupInterviewerTimeslot);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }

        [Authorize(Roles = RolesConstants.InterviewerRole + "," + RolesConstants.AdminRole)]
        public async Task<IActionResult> UserDeleteRange(int id)
        {
            // Check if the timeslotIds array is empty or null
            if (id == 0)
            {
                return NotFound();
            }

            // Get the timeslots to delete
            var timeslotsToDelete = await _context.SignupInterviewerTimeslot
                .Include(x => x.InterviewerSignup)             
                .Include(x => x.Timeslot)
                .ThenInclude(x => x.Event)
                .Where(t => t.InterviewerSignupId == id)
                .ToListAsync();

            // Check if the query worked
            if (timeslotsToDelete == null || timeslotsToDelete.Count == 0)
            {
                return NotFound();
            }

            //Make sure the person trying to delete the timeslots is an admin or the user themselves
            if (timeslotsToDelete.All(e => e.InterviewerSignup.InterviewerId == User.FindFirstValue(ClaimTypes.NameIdentifier)) || 
                User.IsInRole(RolesConstants.AdminRole))
            {
                var viewModel = new TimeRangeViewModel
                {
                    StartTime = timeslotsToDelete.First().Timeslot.Time.ToString(@"h\:mm tt"),
                    EndTime = timeslotsToDelete.Last().Timeslot.Time.AddMinutes(30).ToString(@"h\:mm tt"),
                    SignupInterviewerId = id
                };

                return View(viewModel);
            }
            else
            {
                return NotFound();
            }            
        }

        [Authorize(Roles = RolesConstants.InterviewerRole + "," + RolesConstants.AdminRole)]
        public async Task<IActionResult> UserDeleteRangeConfirmed(int id)
        {
            if(id == null || id == 0)
            {
                return NotFound();
            }

            // Get the timeslots to delete
            var timeslotsToDelete = await _context.SignupInterviewerTimeslot
                .Include(x => x.InterviewerSignup)
                .Include(x => x.Timeslot)
                .Where(t => t.InterviewerSignupId == id)
                .ToListAsync();

                if (timeslotsToDelete.All(e => e.InterviewerSignup.InterviewerId == User.FindFirstValue(ClaimTypes.NameIdentifier)) ||
                User.IsInRole(RolesConstants.AdminRole))
            {
                // Delete the timeslots
                _context.SignupInterviewerTimeslot.RemoveRange(timeslotsToDelete);

                await _context.SaveChangesAsync();

                if (!_context.SignupInterviewerTimeslot.Any(x => x.InterviewerSignup.InterviewerId == timeslotsToDelete[0].InterviewerSignup.InterviewerId &&
                    x.Timeslot.EventId == timeslotsToDelete[0].Timeslot.EventId))
                {
                    var locationInterviewersToDelete = _context.LocationInterviewer
                        .Where(li => li.InterviewerId == timeslotsToDelete[0].InterviewerSignup.InterviewerId &&
                            li.EventId == timeslotsToDelete[0].Timeslot.EventId);

                    _context.LocationInterviewer.RemoveRange(locationInterviewersToDelete);
                    await _context.SaveChangesAsync();

                    if (!_context.SignupInterviewerTimeslot.Any(x => x.InterviewerSignup.InterviewerId == timeslotsToDelete[0].InterviewerSignup.InterviewerId))
                    {
                        var signupInterviewersToDelete = _context.SignupInterviewer
                            .Where(li => li.InterviewerId == timeslotsToDelete[0].InterviewerSignup.InterviewerId);

                        _context.SignupInterviewer.RemoveRange(signupInterviewersToDelete);
                        await _context.SaveChangesAsync();
                    }
                }

                if (User.IsInRole(RolesConstants.AdminRole))
                {
                    return RedirectToAction("Index", "SignupInterviewers");
                }
                return RedirectToAction("Index", "Home");
            }

            return NotFound();
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
                .Include(x => x.InterviewerSignup)
                .Include(x => x.Timeslot)
                .ThenInclude(x => x.Event)
                .Where(t => timeslots.Contains(t.Id))
                .ToListAsync();

            // Check if any of the timeslots to delete are null
            if (timeslotsToDelete == null || timeslotsToDelete.Count == 0)
            {
                return NotFound();
            }

            var date = timeslotsToDelete.First().Timeslot.Event.Date;
            if (timeslotsToDelete.Any(t => t.Timeslot.Event.Date != date))
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
                .Include(x => x.InterviewerSignup)
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

        private static string CreateCalendarEvent(DateTime start, DateTime end)
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
        private static For221 GetClass(bool c)
        {
            if(c)
            {
                return For221.n;
            }
            return For221.y;
        }
        private static string GetLocation(bool l)
        {
            if(l)
            {
                return InterviewLocationConstants.InPerson;
            }
            return InterviewLocationConstants.IsVirtual;
        }
        private static string GetType(bool b, bool t, bool c)
        {
            if (b && t && c) {
                return InterviewTypeConstants.Behavioral + ", " + InterviewTypeConstants.Technical + ", " + InterviewTypeConstants.Case;
            } else if (b && t) {
                return InterviewTypeConstants.Behavioral + ", " + InterviewTypeConstants.Technical;
            } else if (b && c) {
                return InterviewTypeConstants.Behavioral + ", " + InterviewTypeConstants.Case;
            } else if (t && c) {
                return InterviewTypeConstants.Technical + ", " + InterviewTypeConstants.Case;
            } else if (b) {
                return InterviewTypeConstants.Behavioral;
            } else if (t) {
                return InterviewTypeConstants.Technical;
            } else if (c) {
                return InterviewTypeConstants.Case;
            }
            return "";
        }
    }
}
