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
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.SignalR;

namespace sp2023_mis421_mockinterviews.Controllers
{
    public class InterviewEventsController : Controller
    {
        private readonly MockInterviewDataDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISendGridClient _sendGridClient;
        private readonly IHubContext<AssignInterviewsHub> _hubContext;

        public InterviewEventsController(MockInterviewDataDbContext context, 
            UserManager<ApplicationUser> userManager, 
            ISendGridClient sendGridClient,
            IHubContext<AssignInterviewsHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _sendGridClient = sendGridClient;
            _hubContext = hubContext;
        }
	    // adding a dummy comment bc I feel like it
        //--Dalton Wright, Fall 2023

        [Authorize(Roles = RolesConstants.AdminRole)]
        // GET: InterviewEvents
        public async Task<IActionResult> Index()
        {         
            var currdate = DateTime.Now.Date;

            var eventdate = await _context.EventDate
                .Where(x => x.Date.Date == currdate)
                .FirstOrDefaultAsync();

            //var for221 = For221Constants.For321andAbove;
            var for221 = For221.b;
            var date = currdate;
            if(eventdate != null)
            {
                for221 = eventdate.For221;
                date = eventdate.Date;
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
                var name = await _context.SignupInterviewer
                    .Where(x => x.InterviewerId == interviewer)
                    .Select(x => x.FirstName + " " + x.LastName)
                    .FirstOrDefaultAsync();
                var interviewertype = await _context.SignupInterviewer
                    .Where(x => x.InterviewerId == interviewer)
                    .Select(x => x.InterviewType)
                    .FirstOrDefaultAsync();
                var room = await _context.LocationInterviewer
                    .Where(x => x.InterviewerId == interviewer && x.EventDate.Date == date)
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
            var maxsignups = timeslot.MaxSignUps * 3 * 2; //* 2 because there are two interviews per hour, 3 hours is how far in advance we can see right now, should be update-able
            var interviewEvents = await _context.InterviewEvent
                .Include(i => i.Location)
                .Include(i => i.SignupInterviewerTimeslot)
                .ThenInclude(i => i.SignupInterviewer)
                .Include(i => i.Timeslot)
                .ThenInclude(j => j.EventDate)
                .Where(i => i.Status != StatusConstants.Completed && 
                    i.Status != StatusConstants.NoShow && 
                    i.Timeslot.EventDate.IsActive == true)
                .OrderBy(i => i.TimeslotId)
                .Take(maxsignups)
                .ToListAsync();

            // 1. Collect unique StudentIds
            var studentIds = interviewEvents
                .Select(ie => ie.StudentId)
                .Distinct()
                .ToList();

            // 2. Fetch student names for all unique StudentIds
            var studentNames = await _userManager.Users
                .Where(u => studentIds.Contains(u.Id))
                .Select(u => new { u.Id, FullName = $"{u.FirstName} {u.LastName}" })
                .ToDictionaryAsync(u => u.Id, u => u.FullName);

            //3. Collect unique InterviewerIds
            var interviewerIds = await _context.SignupInterviewer
                .Select(ie => ie.InterviewerId)
                .Distinct()
                .ToListAsync();

            // 4. Fetch interviewer names for all unique InterviewerIds
            var interviewerNames = await _userManager.Users
                .Where(u => interviewerIds.Contains(u.Id))
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
                    //var interviewer = await _userManager.Users
                    //    .Where(x => x.Id == interviewEvent.SignupInterviewerTimeslot.SignupInterviewer.InterviewerId)
                    //    .Select(x => new { x.FirstName, x.LastName})
                    //    .FirstOrDefaultAsync();

                    if (interviewerNames.TryGetValue(interviewEvent.SignupInterviewerTimeslot.SignupInterviewer.InterviewerId, out var interviewerName))
                    {
                        interviewEventViewModel.InterviewerName = interviewerName;
                    }

                    //interviewEventViewModel.InterviewerName = interviewer.FirstName + " " + interviewer.LastName;
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
            
            var classReports = students
                .GroupBy(x => x.Class)
                .Select(g => new ClassReport
                {
                    ClassName = ClassConstants.GetClassText((Classes)g.Key),
                    StudentCount = g.Count()
                })
                .Where(r => r.StudentCount > 0)
                .ToList();

            var total = classReports
                .Select(x => x.StudentCount)
                .Sum();

            var signedup221 = classReports
                .Where(x => x.ClassName == ClassConstants.GetClassText(Classes.FirstSem))
                .Select(x => x.StudentCount)
                .FirstOrDefault();

            //foreach (SelectListItem item in classes)
            //{
            //    var studentsCount = students
            //        .Where(x => x.Class == (Classes)int.Parse(item.Value))
            //        .Count();

            //    if(item.Value == ClassConstants.FirstSemester)
            //    {
            //        signedup221 = studentsCount;
            //    }

            //    if(studentsCount > 0)
            //    {
            //        var classReport = new ClassReport
            //        {
            //            ClassName = item.Value,
            //            StudentCount = studentsCount
            //        };

            //        total+= studentsCount;
            //        classReports.Add(classReport);
            //    }
            //}

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

            var student = await _userManager.Users
                .Where(x => x.Id == interviewEvent.StudentId)
                .Select(x => new {x.FirstName, x.LastName})
                .FirstOrDefaultAsync();

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


            var interviewer = await _userManager.Users
                .Where(x => x.Id == interviewEvent.SignupInterviewerTimeslot.SignupInterviewer.InterviewerId)
                .Select(x => new { x.FirstName, x.LastName })
                .FirstOrDefaultAsync();

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
        public async Task<IActionResult> Create()
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var user = await _userManager.Users
                .Where(x => x.Id == userId)
                .FirstOrDefaultAsync();

            var timeslots = new List<Timeslot>();
            if (user.Class == Classes.NotYetMIS || user.Class == Classes.FirstSem)
            {
                timeslots = await _context.Timeslot
                .Where(x => x.IsStudent)
                .Include(y => y.EventDate)
                .Where(x => _context.InterviewEvent.Count(y => y.TimeslotId == x.Id) < x.MaxSignUps)
                .Where(x => x.EventDate.For221 != For221.n && x.EventDate.IsActive == true)
                .ToListAsync();
            }
            else
            {
                timeslots = await _context.Timeslot
                .Where(x => x.IsStudent)
                .Include(y => y.EventDate)
                .Where(x => _context.InterviewEvent.Count(y => y.TimeslotId == x.Id) < x.MaxSignUps)
                .Where(x => x.EventDate.For221 != For221.y && x.EventDate.IsActive == true)
                .ToListAsync();
            }

            var interviewEvents = await _context.InterviewEvent
                .Include(x => x.Timeslot)
                .ThenInclude(x => x.EventDate)
                .Where(x => x.StudentId == userId
                    && x.Timeslot.EventDate.IsActive)
                .ToListAsync();

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
            if (user.Class == Classes.NotYetMIS|| user.Class == Classes.FirstSem)
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
                List<string> calendarEvents = new List<string>();

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
                    var plainBytes = Encoding.UTF8.GetBytes(CreateCalendarEvent(interview.Timeslot.Time, interview.Timeslot.Time.AddMinutes(30)));
                    string tempEvent = Convert.ToBase64String(plainBytes);
                    calendarEvents.Add(tempEvent);
                    interviewDetails += interview.ToString();
                }

                ASendAnEmail emailer = new StudentSignupEmail();
                await emailer.SendEmailAsync(_sendGridClient, "UA MIS Mock Interview Sign-Up Confirmation", user.Email, user.FirstName, interviewDetails, calendarEvents); ;

				return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // GET: InterviewEvents/Edit/5
        [Authorize(Roles = RolesConstants.AdminRole +","+RolesConstants.InterviewerRole)]
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
                    var user = await _userManager.Users
                        .Where(x => x.Id == sit)
                        .Select(x => new { x.Id, x.FirstName, x.LastName })
                        .FirstOrDefaultAsync();

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

            var studentname = await _userManager.Users
                .Where(x => x.Id == interviewEvent.StudentId)
                .Select(x => x.FirstName + " " + x.LastName)
                .FirstOrDefaultAsync();

            var interviewEventManageViewModel = new InterviewEventManageViewModel
            {
                InterviewEvent = interviewEvent,
                InterviewerNames = selectedInterviewersNames,
                StudentName = studentname
            };

            return View(interviewEventManageViewModel);
        }

        // POST: InterviewEvents/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = RolesConstants.AdminRole +","+RolesConstants.InterviewerRole)]
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
                        .Where(x => x.TimeslotId == interviewEvent.TimeslotId && 
                            x.SignupInterviewer.InterviewerId == InterviewerId)
                        .FirstOrDefaultAsync();

                    if(signupInterviewTimeslot != null && interviewEvent.Status == StatusConstants.CheckedIn)
                    {
                        interviewEvent.Status = StatusConstants.Ongoing;
                    }

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
                        .Where(x => x.InterviewerId == InterviewerId && 
                            x.LocationPreference == interviewerPreference && 
                            x.EventDateId == signupInterviewTimeslot.Timeslot.EventDateId && 
                            x.LocationId != null)
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
                if (User.IsInRole(RolesConstants.AdminRole))
                {
                    var newInterviewEvent = await _context.InterviewEvent
                        .Include(x => x.Location)
                        .Include(x => x.SignupInterviewerTimeslot)
                        .ThenInclude(x => x.SignupInterviewer)
                        .Include(x => x.Timeslot)
                        .ThenInclude(x => x.EventDate)
                        .FirstOrDefaultAsync(x => x.Id == id);

                    var studentname = await _userManager.Users
                        .Where(x => x.Id == newInterviewEvent.StudentId)
                        .Select(x => x.FirstName + " " + x.LastName)
                        .FirstOrDefaultAsync();

                    var interviewername = "Not Assigned";

                    if(newInterviewEvent.SignupInterviewerTimeslot != null)
                    {
                        interviewername = await _userManager.Users
                            .Where(x => x.Id == newInterviewEvent.SignupInterviewerTimeslot.SignupInterviewer.InterviewerId)
                            .Select(x => x.FirstName + " " + x.LastName)
                            .FirstOrDefaultAsync();
                    }
                    
                    if(newInterviewEvent.Location == null)
                    {
                        newInterviewEvent.Location = new Location()
                        {
                            Room = "Not Assigned"
                        };
                    }

                    var time = $"{ newInterviewEvent.Timeslot.Time:hh:mm tt}";
                    var date = $"{newInterviewEvent.Timeslot.EventDate.Date:M/d/yyyy}";

                    await _hubContext.Clients.All.SendAsync("ReceiveInterviewEventUpdate", newInterviewEvent, studentname, interviewername, time, date);

                    return RedirectToAction(nameof(Index));
                }
                return RedirectToAction("Index", "Home");
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
                .Where(x => x.Timeslot.EventDateId == interviewEvent.Timeslot.EventDateId && 
                    x.TimeslotId == interviewEvent.TimeslotId && 
                    x.Timeslot.EventDate.IsActive)
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

                var newInterviewEvent = await _context.InterviewEvent
                    .Include(x => x.Location)
                    .Include(x => x.SignupInterviewerTimeslot)
                    .ThenInclude(x => x.SignupInterviewer)
                    .Include(x => x.Timeslot)
                    .ThenInclude(x => x.EventDate)
                    .FirstOrDefaultAsync(x => x.Id == id);

                var studentname = await _userManager.Users
                    .Where(x => x.Id == newInterviewEvent.StudentId)
                    .Select(x => x.FirstName + " " + x.LastName)
                    .FirstOrDefaultAsync();

                var interviewername = "Not Assigned";

                if (newInterviewEvent.SignupInterviewerTimeslot != null)
                {
                    interviewername = await _userManager.Users
                        .Where(x => x.Id == newInterviewEvent.SignupInterviewerTimeslot.SignupInterviewer.InterviewerId)
                        .Select(x => x.FirstName + " " + x.LastName)
                        .FirstOrDefaultAsync();
                }

                if (newInterviewEvent.Location == null)
                {
                    newInterviewEvent.Location = new Location()
                    {
                        Room = "Not Assigned"
                    };
                }

                var time = $"{newInterviewEvent.Timeslot.Time:hh:mm tt}";
                var date = $"{newInterviewEvent.Timeslot.EventDate.Date:M/d/yyyy}";

                await _hubContext.Clients.All.SendAsync("ReceiveInterviewEventUpdate", newInterviewEvent, studentname, interviewername, time, date);

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

            await _hubContext.Clients.All.SendAsync("ReceiveInterviewEventUpdate", new InterviewEvent() { Id = (int)id }, "delete", "", "", "");

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
            var timeslot = _context.InterviewEvent
                .Include(i => i.Timeslot.EventDate)
                .Where(i => i.StudentId == interviewEvent.StudentId && i.TimeslotId == interviewEvent.TimeslotId)
                .Select(i => i.Timeslot)
                .FirstOrDefault();

            // Get the SignupInterviewerTimeslots for the same timeslot and event date as the user's interview
            var signupInterviewerTimeslots = _context.SignupInterviewerTimeslot
                .Include(s => s.SignupInterviewer)
                .Where(s => s.TimeslotId == timeslot.Id && 
                    s.Timeslot.EventDate.Date == timeslot.EventDate.Date && 
                    s.Timeslot.EventDate.IsActive == true)
                .ToList();

            var interviewers = _context.SignupInterviewer
                .Select(x => x.InterviewerId)
                .Distinct()
                .ToList();

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

            //get list of all distinct interviewers that are in an interview event with a status of checked in or ongoing
            var selectedStatusNot = _context.InterviewEvent
                .Include(x => x.SignupInterviewerTimeslot)
                .ThenInclude(x => x.Timeslot)
                .ThenInclude(x => x.EventDate)
                .Include(x => x.SignupInterviewerTimeslot.SignupInterviewer)
                .Where(x => x.SignupInterviewerTimeslot.Timeslot.EventDate.IsActive == true)
                .Select(x => x.SignupInterviewerTimeslot.SignupInterviewer.InterviewerId)
                .Distinct()
                .ToList();

            var selectedStatus = interviewers
                .Except(selectedStatusNot)
                .ToList();

            //Get list of all distinct interviewers that are assigned to a location
            var selectedLocation = _context.LocationInterviewer
                .Where(x => x.LocationId != null &&
                    x.EventDate == timeslot.EventDate &&
                    x.EventDate.IsActive == true)
                .Select(x => x.InterviewerId)
                .Distinct()
                .ToList();

            //Get list of all distinct interviewers that have interviewed this student
            var haveInterviewed = _context.InterviewEvent
                .Where(x => x.StudentId == interviewEvent.StudentId)
                .Select(x => x.SignupInterviewerTimeslot.SignupInterviewer.InterviewerId)
                .Distinct()
                .ToList();

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
