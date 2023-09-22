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
using Microsoft.AspNetCore.Razor.Language;
using sp2023_mis421_mockinterviews.Data.Constants;
using sp2023_mis421_mockinterviews.Interfaces;
using sp2023_mis421_mockinterviews.Data.Access;
using sp2023_mis421_mockinterviews.Data.Access.Emails;

namespace sp2023_mis421_mockinterviews.Controllers
{
    public class InterviewEventsController : Controller
    {
        private readonly MockInterviewDataDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISendGridClient _sendGridClient;

        public InterviewEventsController(MockInterviewDataDbContext context, 
            UserManager<ApplicationUser> userManager, 
            ISendGridClient sendGridClient)
        {
            _context = context;
            _userManager = userManager;
            _sendGridClient = sendGridClient;
        }

        [Authorize(Roles = RolesConstants.AdminRole)]
        // GET: InterviewEvents
        public async Task<IActionResult> Index()
        {         
            var currdate = DateTime.Now.Date;

            var eventdate = await _context.EventDate
                .Where(x => x.Date.Date == currdate)
                .FirstOrDefaultAsync();
            var for221 = eventdate.For221;
            var date = eventdate.Date;
            if(eventdate == null)
            {
                for221 = false;
                date = currdate;

            }

            //Gets list of all distinct interviewers that are in an InterviewEvent with a status of checked in or ongoing
            var selectedStatus = await _context.InterviewEvent
                .Include(x => x.SignupInterviewerTimeslot)
                .ThenInclude(x => x.SignupInterviewer)
                .Include(x => x.Timeslot)
                .ThenInclude(x => x.EventDate)
                .Where(u => (u.Status == StatusConstants.CheckedIn || u.Status == StatusConstants.Ongoing) &&
                    u.SignupInterviewerTimeslotId != null)
                .Select(x => x.SignupInterviewerTimeslot.SignupInterviewer.InterviewerId)
                .Distinct()
                .ToListAsync();

            //Get list of all distinct interviewers that are assigned to a location
            var selectedLocation = await _context.LocationInterviewer
                .Where(x => x.LocationId != null && 
                    x.EventDate.Date == date && 
                    x.EventDate.For221 == for221)
                .Select(x => x.InterviewerId)
                .Distinct()
                .ToListAsync();

            var selectedinterviewers = selectedLocation
                .Except(selectedStatus)
                .ToList();


            var interviewers = new List<AvailableInterviewer>();
            foreach(var interviewer in selectedinterviewers)
            {
                var name = await _context.SignupInterviewer.Where(x => x.InterviewerId == interviewer)
                    .Select(x => x.FirstName + " " + x.LastName)
                    .FirstOrDefaultAsync();
                var interviewertype = await _context.SignupInterviewer.Where(x => x.InterviewerId == interviewer)
                    .Select(x => x.InterviewType)
                    .FirstOrDefaultAsync();
                var room = await _context.LocationInterviewer
                    .Where(x => x.InterviewerId == interviewer)
                    .Select(x => x.Location.Room)
                    .FirstOrDefaultAsync();
                interviewers.Add(new AvailableInterviewer
                {
                    Name = name,
                    InterviewType = interviewertype,
                    Room = room
                });
            }

            var timeslot = await _context.Timeslot
                .OrderByDescending(x => x.MaxSignUps)
                .FirstOrDefaultAsync();
            var maxsignups = timeslot.MaxSignUps * HoursInAdvanceConstant.HoursInAdvance * 2; //* 2 because there are two interviews per hour
            var interviewEvents = await _context.InterviewEvent
                .Include(i => i.Location)
                .Include(i => i.SignupInterviewerTimeslot)
                .ThenInclude(i => i.SignupInterviewer)
                .Include(i => i.Timeslot)
                .ThenInclude(j => j.EventDate)
                .Where(i => i.Status != StatusConstants.Completed && i.Status != StatusConstants.NoShow)
                .OrderBy(i => i.TimeslotId)
                .Take(maxsignups)
                .ToListAsync();

            // 1. Collect unique StudentIds
            var studentIds = interviewEvents
                .Select(ie => ie.StudentId)
                .Distinct().ToList();

            // 2. Fetch student names for all unique StudentIds
            var studentNames = await _userManager.Users
                .Where(u => studentIds.Contains(u.Id))
                .Select(u => new { u.Id, FullName = $"{u.FirstName} {u.LastName}" })
                .ToDictionaryAsync(u => u.Id, u => u.FullName);

            var model = new InterviewEventIndexViewModel();
            var eventslist = new List<InterviewEventViewModel>();
            foreach(InterviewEvent interviewEvent in interviewEvents)
            {
                //var student = await _userManager.Users
                //    .Where(u => u.Id == interviewEvent.StudentId)
                //    .Select(u => new { u.FirstName, u.LastName })
                //    .FirstOrDefaultAsync();

                //var studentname = student != null ? $"{student.FirstName} {student.LastName}" : null;
                var interviewEventViewModel = new InterviewEventViewModel();


                if (studentNames.TryGetValue(interviewEvent.StudentId, out var studentName))
                {
                    interviewEventViewModel.StudentName = studentName;
                }

                if (interviewEvent.SignupInterviewerTimeslot != null)
                {
                    var interviewer = await _userManager.Users
                        .Where(x => x.Id == interviewEvent.SignupInterviewerTimeslot.SignupInterviewer.InterviewerId)
                        .Select(x => new { x.FirstName, x.LastName})
                        .FirstOrDefaultAsync();

                    interviewEventViewModel.InterviewerName = interviewer.FirstName + " " + interviewer.LastName;
                    interviewEventViewModel.InterviewEvent = interviewEvent;

                    eventslist.Add(interviewEventViewModel);
                }
                else
                {
                    interviewEventViewModel.InterviewerName = "Not Assigned";
                    interviewEventViewModel.InterviewEvent = interviewEvent;


                    eventslist.Add(interviewEventViewModel);
                }
            }

            model.Interviews = eventslist;
            model.AvailableInterviewers = interviewers;

            return View(model);
        }

        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> AttendanceReport()
        {
            //can't find my other attendance report method for some reason
            var uniqueStudentIds = await _context.InterviewEvent
                .Select(e => e.StudentId)
                .Distinct()
                .ToListAsync();
            
            if(uniqueStudentIds.Count == 0 || uniqueStudentIds == null)
            {
                return BadRequest("There are no students signed up yet.");
            }

            var students = await _userManager.Users
                .Where(u => uniqueStudentIds.Contains(u.Id))
                .Select(u => new
                {
                    u.FirstName,
                    u.LastName,
                    u.Class // Replace with the actual property name
                })
                .ToListAsync();

            var total = 0;
            var signedup221 = 0;
            var classReports = new List<ClassReport>();
            var classes = ClassConstants.GetClassOptions();
            foreach(SelectListItem item in classes)
            {
                var studentsCount = students
                    .Where(x => x.Class == item.Value)
                    .Count();

                if(item.Value == ClassConstants.FirstSemester)
                {
                    signedup221 = studentsCount;
                }

                if(studentsCount > 0)
                {
                    var classReport = new ClassReport
                    {
                        ClassName = item.Value,
                        StudentCount = studentsCount
                    };

                    total+= studentsCount;
                    classReports.Add(classReport);
                }
            }

            var summaries = new List<ClassReport>
            {
                new ClassReport
                {
                    ClassName = "Total",
                    StudentCount = total
                }
            };

            var entireProgram = await _context.MSTeamsStudentUpload.CountAsync();
            var entire221 = await _context.MSTeamsStudentUpload.Where(x => x.In221 == true).CountAsync();

            double percentEntireProgram = (double)total / entireProgram;
            double percentEntire221 = (double)signedup221 / entire221;

            // Round to two decimal places
            percentEntireProgram = Math.Round(percentEntireProgram, 2) * 100;
            percentEntire221 = Math.Round(percentEntire221, 2) * 100;

            summaries.Add(new ClassReport
            {
                ClassName = "Total % Signed Up",
                StudentCount = (int)percentEntireProgram
            });
            summaries.Add(new ClassReport
            {
                ClassName = "221 % Signed Up",
                StudentCount = (int)percentEntire221
            });


            var viewModel = new AttendanceReportViewModel()
            {
                ClassReports = classReports,
                SummaryStats = summaries
            };

            return View("AttendanceReport", viewModel);
        }

        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> AssessFeedback()
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
            foreach (InterviewEvent interviewEvent in interviewEvents)
            {
                var student = await _userManager.FindByIdAsync(interviewEvent.StudentId);

                if (interviewEvent.SignupInterviewerTimeslot != null)
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

            return View("Feedback",model);
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

            if(interviewEvent.SignupInterviewerTimeslot == null)
            {
                var viewModel = new InterviewEventViewModel
                {
                    InterviewEvent = interviewEvent,
                    InterviewerName = "Not Assigned",
                    StudentName = student.FirstName + " " + student.LastName
                };

                return View(viewModel);
            }


            var interviewer = await _userManager.FindByIdAsync(interviewEvent.SignupInterviewerTimeslot.SignupInterviewer.InterviewerId);

            var secondViewModel = new InterviewEventViewModel
            {
                InterviewEvent = interviewEvent,
                InterviewerName = interviewer.FirstName + " " + interviewer.LastName,
                StudentName = student.FirstName + " " + student.LastName
            };

            return View(secondViewModel);
        }

        // GET: InterviewEvents/Create
        [Authorize(Roles = RolesConstants.StudentRole)]
        public IActionResult Create()
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userTask = _userManager.FindByIdAsync(userId);
            userTask.GetAwaiter().GetResult();
            var user = userTask.Result;

            var for221 = false;
            if(user.Class == ClassConstants.PreMIS || user.Class == ClassConstants.FirstSemester)
            {
                for221 = true;
            }
            else
            {
                for221 = false;
            }

            var timeslotsTask = _context.Timeslot
                .Where(x => x.IsStudent)
                .Include(y => y.EventDate)
                .Where(x => _context.InterviewEvent.Count(y => y.TimeslotId == x.Id) < x.MaxSignUps)
                .Where(x => x.EventDate.For221 == for221)
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

            var interviewTypeTwo = InterviewTypeConstants.Technical;
            if (user.Class == ClassConstants.PreMIS || user.Class == ClassConstants.FirstSemester)
            {
                interviewTypeTwo = InterviewTypeConstants.Behavioral;
            }

            var interviewEvents = new List<InterviewEvent>
            {
                new InterviewEvent 
                {
                    TimeslotId = SelectedEventIds,
                    StudentId = userId,
                    Status = StatusConstants.Default,
                    InterviewType = InterviewTypeConstants.Behavioral
                },
                new InterviewEvent 
                {
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
                    .Where(v => v.TimeslotId == SelectedEventIds)
                    .FirstOrDefaultAsync();
                emailTimes.Add(newEvent);
                newEvent = await _context.InterviewEvent
                    .Include(v => v.Timeslot)
                    .ThenInclude(y => y.EventDate)
                    .Where(v => v.TimeslotId == SelectedEventIds + 1)
                    .FirstOrDefaultAsync();
                emailTimes.Add(newEvent);

                string interviewDetails = "";
                foreach (var interview in emailTimes)
                {
                    interviewDetails += interview.ToString();
                }

                ASendAnEmail emailer = new StudentSignupEmail();
                await emailer.SendEmailAsync(_sendGridClient, SubjectLineConstants.StudentSignupEmail, user.Email, user.FirstName, interviewDetails);

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
                selectedInterviewersNames.Insert(0, new SelectListItem
                {
                    Value = "0",
                    Text = "Unassigned"
                });
                
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
                        .ThenInclude(x => x.EventDate)
                        .Where(x => x.TimeslotId == interviewEvent.TimeslotId && x.SignupInterviewer.InterviewerId == InterviewerId)
                        .FirstOrDefaultAsync();

                    var interviewerPreference = "";
                    if(signupInterviewTimeslot.SignupInterviewer.IsVirtual && signupInterviewTimeslot.SignupInterviewer.InPerson)
                    {
                        interviewerPreference = InterviewLocationConstants.InPerson + "/" + InterviewLocationConstants.IsVirtual;
                    }
                    else if(signupInterviewTimeslot.SignupInterviewer.IsVirtual)
                    {
                        interviewerPreference = InterviewLocationConstants.IsVirtual;
                    }
                    else if(signupInterviewTimeslot.SignupInterviewer.InPerson)
                    {
                        interviewerPreference = InterviewLocationConstants.InPerson;
                    }

                    var location = await _context.LocationInterviewer
                        .Include(x => x.Location)
                        .Where(x => x.InterviewerId == InterviewerId && x.LocationPreference == interviewerPreference && x.EventDateId == signupInterviewTimeslot.Timeslot.EventDateId && x.LocationId != null)
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

        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> Override(int? id)
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

            interviewEvent = await _context.InterviewEvent
                .Include(x => x.Timeslot)
                .ThenInclude(x => x.EventDate)
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync();

            //var selectedInterviewers = await _userManager.GetUsersInRoleAsync(RolesConstants.InterviewerRole);
            //var interviewers = selectedInterviewers.ToList();

            var selectedInterviewers = await _context.SignupInterviewerTimeslot
                .Include(x => x.SignupInterviewer)
                .Include(x => x.Timeslot)
                .ThenInclude(x => x.EventDate)
                .Where(x => x.Timeslot.EventDateId == interviewEvent.Timeslot.EventDateId && x.TimeslotId == interviewEvent.TimeslotId)
                .Select(x => x.SignupInterviewer.InterviewerId)
                .Distinct()
                .ToListAsync();

            var selectedInterviewersNames = new List<SelectListItem>();
            if (selectedInterviewers.Count == 0)
            {
                if (interviewEvent.SignupInterviewerTimeslot != null)
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
                    var user = await _userManager.Users
                        .Where(u => u.Id == sit)
                        .Select(u => new { u.Id, u.FirstName, u.LastName })
                        .FirstOrDefaultAsync();

                    if (user != null)
                    {
                        selectedInterviewersNames.Add(new SelectListItem
                        {
                            Value = user.Id,
                            Text = user.FirstName + " " + user.LastName
                        });
                    }
                }
                selectedInterviewersNames = selectedInterviewersNames.OrderBy(item => item.Text).ToList();
                selectedInterviewersNames.Insert(0, new SelectListItem
                {
                    Value = "0",
                    Text = "Unassigned"
                });

            }


            var interviewEventManageViewModel = new InterviewEventManageViewModel
            {
                InterviewEvent = interviewEvent,
                InterviewerNames = selectedInterviewersNames
            };

            return View(interviewEventManageViewModel);
        }

        [Authorize(Roles = RolesConstants.AdminRole)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Override(int id, [Bind("Id,StudentId,LocationId,TimeslotId,InterviewType,Status,SignupInterviewerTimeslotId")] InterviewEvent interviewEvent, string InterviewerId)
        {
            if (id != interviewEvent.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                if (InterviewerId == "0")
                {
                    interviewEvent.SignupInterviewerTimeslot = null;
                }
                else
                {
                    var signupInterviewTimeslot = await _context.SignupInterviewerTimeslot
                        .Include(x => x.SignupInterviewer)
                        .Include(x => x.Timeslot)
                        .ThenInclude(x => x.EventDate)
                        .Where(x => x.TimeslotId == interviewEvent.TimeslotId && x.SignupInterviewer.InterviewerId == InterviewerId)
                        .FirstOrDefaultAsync();

                    var interviewerPreference = "";
                    if (signupInterviewTimeslot.SignupInterviewer.IsVirtual && signupInterviewTimeslot.SignupInterviewer.InPerson)
                    {
                        interviewerPreference = InterviewLocationConstants.InPerson + "/" + InterviewLocationConstants.IsVirtual;
                    }
                    else if (signupInterviewTimeslot.SignupInterviewer.IsVirtual)
                    {
                        interviewerPreference = InterviewLocationConstants.IsVirtual;
                    }
                    else if (signupInterviewTimeslot.SignupInterviewer.InPerson)
                    {
                        interviewerPreference = InterviewLocationConstants.InPerson;
                    }

                    var location = await _context.LocationInterviewer
                        .Include(x => x.Location)
                        .Where(x => x.InterviewerId == InterviewerId && x.LocationPreference == interviewerPreference && x.EventDateId == signupInterviewTimeslot.Timeslot.EventDateId && x.LocationId != null)
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

            if (interviewEvent.SignupInterviewerTimeslot == null)
            {
                var viewModel = new InterviewEventViewModel
                {
                    InterviewEvent = interviewEvent,
                    InterviewerName = "Not Assigned",
                    StudentName = student.FirstName + " " + student.LastName
                };

                return View(viewModel);
            }


            var interviewer = await _userManager.FindByIdAsync(interviewEvent.SignupInterviewerTimeslot.SignupInterviewer.InterviewerId);

            var secondViewModel = new InterviewEventViewModel
            {
                InterviewEvent = interviewEvent,
                InterviewerName = interviewer.FirstName + " " + interviewer.LastName,
                StudentName = student.FirstName + " " + student.LastName
            };

            return View(secondViewModel);
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
            //var interviewersTask = await _userManager.GetUsersInRoleAsync(RolesConstants.InterviewerRole);
            //var interviewers = interviewersTask.ToList();

            var interviewers = await _context.SignupInterviewer
                .Select(x => x.InterviewerId)
                .Distinct()
                .ToListAsync();

            //Get list of all distinct interviewers that have signed up to deliver the same type of interview as the student needs
            var selectedTypes = _context.SignupInterviewer
                .Where(u => ((interviewEvent.InterviewType == InterviewTypeConstants.Technical && u.IsTechnical) ||
                             (interviewEvent.InterviewType == InterviewTypeConstants.Behavioral && u.IsBehavioral)))
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
                    i.SignupInterviewerTimeslot.SignupInterviewer.InterviewerId == u &&
                    (i.Status == StatusConstants.CheckedIn || i.Status == StatusConstants.Ongoing)))
                .Distinct()
                .ToList();

            //Get list of all distinct interviewers that are assigned to a location
            var selectedLocation = await _context.LocationInterviewer
                .Where(x => x.LocationId != null &&
                    x.EventDate == timeslot.EventDate)
                .Select(x => x.InterviewerId)
                .Distinct()
                .ToListAsync();



            //Get list of all distinct interviewers that have not interviewed this student
            var haveInterviewed = _context.InterviewEvent
                .Where(x => x.StudentId == interviewEvent.StudentId)
                .Select(x => x.SignupInterviewerTimeslot.SignupInterviewer.InterviewerId)
                .Distinct()
                .ToList();

            //var interviewersIds = interviewers
            //    .Select(x => x.Id)
            //    .Distinct()
            //    .ToList();

            var haveNotInterviewed = interviewers
                .Except(haveInterviewed)
                .ToList();

            //Get list of all distinct interviewers that are not the student
            var notStudent = interviewers
                .Where(x => x != interviewEvent.StudentId)
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
