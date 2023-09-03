using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using sp2023_mis421_mockinterviews.Data;
using sp2023_mis421_mockinterviews.Data.Constants;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;
using sp2023_mis421_mockinterviews.Models.UserDb;
using sp2023_mis421_mockinterviews.Models.ViewModels;

namespace sp2023_mis421_mockinterviews.Controllers
{
    [Authorize(Roles = RolesConstants.AdminRole)]
    public class TimeslotsController : Controller
    {
        private readonly MockInterviewDataDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TimeslotsController(MockInterviewDataDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Timeslots
        public async Task<IActionResult> Index()
        {
            var timeslots = await _context.Timeslot
                .Include(t => t.EventDate)
                .ToListAsync();
            var eventdates = await _context.EventDate
                .ToListAsync();

            var countlist = new List<ParticipantCountViewModel>();
            foreach (Timeslot timeslot in timeslots)
            {
                countlist.Add(new ParticipantCountViewModel
                {
                    Timeslot = timeslot,
                    StudentCount = 0,
                    InterviewerCount = 0,
                    VolunteerCount = 0
                });
            }

            var viewModel = new TimeslotViewModel()
            {
                Timeslots = countlist,
                EventDates = eventdates
            };

            return View(viewModel);

            //return _context.Timeslot != null ?
            //            View(await _context.Timeslot.Include(t => t.EventDate).ToListAsync()):
            //              Problem("Entity set 'MockInterviewDataDbContext.Timeslot'  is null.");
        }
        public async Task<IActionResult> SignupReport()
        {
            var timeslots = await _context.Timeslot
                .Include(t => t.EventDate)
                .ToListAsync();
            var eventdates = await _context.EventDate
                .ToListAsync();

            var countlist = new List<ParticipantCountViewModel>();
            foreach(Timeslot timeslot in timeslots)
            {
                var studentCount = await _context.InterviewEvent.Where(x => x.TimeslotId == timeslot.Id).CountAsync();
                var volunteerCount = await _context.VolunteerEvent.Where(x => x.TimeslotId == timeslot.Id).CountAsync();
                var interviewerCount = await _context.SignupInterviewerTimeslot.Where(x => x.TimeslotId == timeslot.Id).CountAsync();
                countlist.Add(new ParticipantCountViewModel
                {
                    Timeslot = timeslot,
                    StudentCount = studentCount,
                    InterviewerCount = interviewerCount,
                    VolunteerCount = volunteerCount
                });
            }

            var viewModel = new TimeslotViewModel()
            {
                Timeslots = countlist,
                EventDates = eventdates
            };

            return View("SignupReport", viewModel);

            //return _context.Timeslot != null ?
            //            View(await _context.Timeslot.Include(t => t.EventDate).ToListAsync()):
            //              Problem("Entity set 'MockInterviewDataDbContext.Timeslot'  is null.");
        }

        public async Task<IActionResult> AllocationReport()
        {
            var timeslots = await _context.Timeslot
                .Include(t => t.EventDate)
                .Where(x => x.EventDate.For221 == false && x.IsInterviewer == true)
                .ToListAsync();

            var countlist = new List<ParticipantCountViewModel>();
            foreach (Timeslot timeslot in timeslots)
            {
                var studentCount = await _context.InterviewEvent.Where(x => x.TimeslotId == timeslot.Id).CountAsync();
                var volunteerCount = await _context.VolunteerEvent.Where(x => x.TimeslotId == timeslot.Id).CountAsync();
                var interviewerCount = await _context.SignupInterviewerTimeslot.Where(x => x.TimeslotId == timeslot.Id).CountAsync();
                countlist.Add(new ParticipantCountViewModel
                {
                    Timeslot = timeslot,
                    StudentCount = studentCount,
                    InterviewerCount = interviewerCount,
                    VolunteerCount = volunteerCount,
                    Difference = studentCount - interviewerCount
                });
            }

            var top10underserved = countlist
                .OrderByDescending(x => x.Difference)
                .Take(10)
                .ToList();

            var top10available = countlist
                .OrderByDescending(x => x.Difference)
                .TakeLast(10)
                .ToList();

            var viewModel = new AllocationReportViewModel()
            {
                Top10Available = top10available,
                Top10Underserved = top10underserved,
            };

            return View("AllocationReport", viewModel);
        }

        // GET: Timeslots/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Timeslot == null)
            {
                return NotFound();
            }

            var timeslot = await _context.Timeslot.Include(t => t.EventDate)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (timeslot == null)
            {
                return NotFound();
            }

            var interviewerList = await _context.SignupInterviewerTimeslot
                .Include(x => x.SignupInterviewer)
                .Where(x => x.TimeslotId == timeslot.Id)
                .ToListAsync();
            var interviewerNamesList = new List<string>();
            if (interviewerList != null && interviewerList.Count != 0)
            {
                var interviewerIds = interviewerList
                .Select(x => x.SignupInterviewer.InterviewerId)
                .ToList();
                foreach (var interviewerid in interviewerIds)
                {
                    var interviewer = await _userManager.FindByIdAsync(interviewerid);
                    interviewerNamesList.Add(interviewer.FirstName + " " + interviewer.LastName);
                }
            }

            var studentList = await _context.InterviewEvent
               .Where(x => x.TimeslotId == timeslot.Id)
               .ToListAsync();
            var studentNamesList = new List<string>();
            if (studentList != null && studentList.Count != 0)
            {
                var studentIds = studentList
                .Select(x => x.StudentId)
                .ToList();
                foreach (var interviewerid in studentIds)
                {
                    var student = await _userManager.FindByIdAsync(interviewerid);
                    studentNamesList.Add(student.FirstName + " " + student.LastName);
                }
            }

            var volunteerList = await _context.VolunteerEvent
               .Where(x => x.TimeslotId == timeslot.Id)
               .ToListAsync();
            var volunteerNamesList = new List<string>();
            if (volunteerList != null && volunteerList.Count != 0)
            {
                var volunteerIds = volunteerList
                .Select(x => x.StudentId)
                .ToList();
                foreach (var interviewerid in volunteerIds)
                {
                    var volunteer = await _userManager.FindByIdAsync(interviewerid);
                    volunteerNamesList.Add(volunteer.FirstName + " " + volunteer.LastName);
                }
            }

            var viewModel = new TimeslotDetailsViewModel
            {
                Timeslot = timeslot,
                InterviewerNames = interviewerNamesList,
                StudentNames = studentNamesList,
                VolunteerNames = volunteerNamesList
            };

            return View(viewModel);
        }

        // GET: Timeslots/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Timeslots/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Time,IsActive,IsVolunteer,IsInterviewer,IsStudent")] Timeslot timeslot)
        {
            if (ModelState.IsValid)
            {
                _context.Add(timeslot);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(timeslot);
        }

        [Authorize(Roles =RolesConstants.AdminRole)]
        // GET: Timeslots/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Timeslot == null)
            {
                return NotFound();
            }

            var timeslot = await _context.Timeslot.FindAsync(id);
            if (timeslot == null)
            {
                return NotFound();
            }
            return View(timeslot);
        }

        // POST: Timeslots/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = RolesConstants.AdminRole)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Time,IsActive,IsVolunteer,IsInterviewer,IsStudent,MaxSignUps,EventDateId")] Timeslot timeslot)
        {
            if (id != timeslot.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(timeslot);
                    await _context.SaveChangesAsync();

                    var pairedtimeslot = await _context.Timeslot.FindAsync(id + 1);
                    if(pairedtimeslot != null)
                    {
                        pairedtimeslot.MaxSignUps = timeslot.MaxSignUps;
                        _context.Update(pairedtimeslot);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TimeslotExists(timeslot.Id))
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
            return View(timeslot);
        }


        [Authorize(Roles = RolesConstants.AdminRole)]
        // GET: Timeslots/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Timeslot == null)
            {
                return NotFound();
            }

            var timeslot = await _context.Timeslot
                .FirstOrDefaultAsync(m => m.Id == id);
            if (timeslot == null)
            {
                return NotFound();
            }

            return View(timeslot);
        }

        // POST: Timeslots/Delete/5
        [Authorize(Roles = RolesConstants.AdminRole)]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Timeslot == null)
            {
                return Problem("Entity set 'MockInterviewDataDbContext.Timeslot'  is null.");
            }
            var timeslot = await _context.Timeslot.FindAsync(id);
            if (timeslot != null)
            {
                _context.Timeslot.Remove(timeslot);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> UpdateMaxTimeslots()
        {
            return View();
        }

        public async Task<IActionResult> UpdateMaxSignupsConfirmed(int maxsignups)
        {
            var timeslots = await _context.Timeslot.ToListAsync();
            foreach (var timeslot in timeslots)
            {
                timeslot.MaxSignUps = maxsignups;
            }
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }


        private bool TimeslotExists(int id)
        {
          return (_context.Timeslot?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
