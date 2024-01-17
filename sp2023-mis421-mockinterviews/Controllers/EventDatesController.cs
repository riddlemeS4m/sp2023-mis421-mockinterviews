using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using sp2023_mis421_mockinterviews.Data;
using sp2023_mis421_mockinterviews.Data.Access;
using sp2023_mis421_mockinterviews.Data.Constants;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;
using sp2023_mis421_mockinterviews.Models.ViewModels;

namespace sp2023_mis421_mockinterviews.Controllers
{
    [Authorize(Roles = RolesConstants.AdminRole)]
    public class EventDatesController : Controller
    {
        private readonly MockInterviewDataDbContext _context;

        public EventDatesController(MockInterviewDataDbContext context)
        {
            _context = context;
        }

        // GET: EventDates
        public async Task<IActionResult> Index()
        {
              return _context.EventDate != null ? 
                          View(await _context.EventDate.ToListAsync()) :
                          Problem("Entity set 'MockInterviewDataDbContext.EventDate'  is null.");
        }

        public async Task<IActionResult> EventStatistics()
        {
            var eventDates = await _context.EventDate.ToListAsync();

            var participantCounts = new List<ParticipantCountPerDateViewModel>();

            foreach (var eventDate in eventDates)
            {
                var studentCount = await _context.InterviewEvent
                    .Where(e => e.Timeslot.EventDateId == eventDate.Id)
                    .Select(e => e.StudentId)
                    .Distinct()
                    .CountAsync();

                var interviewerCount = await _context.SignupInterviewerTimeslot
                    //.Include(s => s.SignupInterviewer)
                    .Where(s => s.Timeslot.EventDateId == eventDate.Id)
                    .Select(s => s.SignupInterviewer.InterviewerId)
                    .Distinct()
                    .CountAsync();

                var volunteerCount = await _context.VolunteerEvent
                    .Where(v => v.Timeslot.EventDateId == eventDate.Id)
                    .Select(v => v.StudentId)
                    .Distinct()
                    .CountAsync();

                var countViewModel = new ParticipantCountPerDateViewModel
                {
                    EventDate = eventDate,
                    StudentCount = studentCount,
                    InterviewerCount = interviewerCount,
                    VolunteerCount = volunteerCount
                };

                participantCounts.Add(countViewModel);
            }

            var uniqueStudentCount = await _context.InterviewEvent
                .Where(x => x.Timeslot.EventDate.IsActive)
                .Select(e => e.StudentId)
                .Distinct()
                .CountAsync();

            var uniqueInterviewerCount = await _context.SignupInterviewerTimeslot
                .Where(s => s.Timeslot.EventDate.IsActive)
                .Select(s => s.SignupInterviewer.InterviewerId)
                .Distinct()
                .CountAsync();

            var uniqueVolunteerCount = await _context.VolunteerEvent
                .Where(v => v.Timeslot.EventDate.IsActive)
                .Select(v => v.StudentId)
                .Distinct()
                .CountAsync();


            var eventStatisticsVM = new EventStatisticsViewModel
            {
                EventStatistics = participantCounts,
                TotalStudents = uniqueStudentCount,
                TotalInterviewers = uniqueInterviewerCount,
                TotalVolunteers = uniqueVolunteerCount
            };

            return View("EventStatistics", eventStatisticsVM);
        }


        // GET: EventDates/Details/5
        public async Task<IActionResult> Details(int id)
        {
            if (id == null || _context.EventDate == null)
            {
                return NotFound();
            }

            var eventDate = await _context.EventDate
                .FirstOrDefaultAsync(m => m.Id == id);
            if (eventDate == null)
            {
                return NotFound();
            }

            return View(eventDate);
        }

        // GET: EventDates/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: EventDates/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Date,EventName,For221")] EventDate eventDate)
        {
            if (ModelState.IsValid)
            {
                // Check if both checkboxes are selected
                bool isFor221True = Request.Form["For221True"].Count > 0;
                bool isFor221False = Request.Form["For221False"].Count > 0;

                // Set the value of the "For221" field based on the checkboxes
                if (isFor221True && isFor221False)
                {
                    eventDate.For221 = For221.b;
                }
                else if (isFor221True)
                {
                    eventDate.For221 = For221.y;
                }
                else if (isFor221False)
                {
                    eventDate.For221 = For221.n;
                }

                _context.Add(eventDate);
                await _context.SaveChangesAsync();

                var dates = new List<EventDate>
                {
                    eventDate
                };
                var timeslots = TimeslotSeed.SeedTimeslots(dates);

                foreach (Timeslot timeslot in timeslots)
                {
                    _context.Add(timeslot);
                    await _context.SaveChangesAsync();
                }
           
                return RedirectToAction("Index","EventDates");
            }

            return View(eventDate);
        }

        // GET: EventDates/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            //if (id == null || _context.EventDate == null)
            //{
            //    return NotFound();
            //}

            var eventDate = await _context.EventDate.FindAsync(id);
            if (eventDate == null)
            {
                return NotFound();
            }
            return View(eventDate);
        }

        // POST: EventDates/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Date,EventName,IsActive")] EventDate eventDate)
        {
            if (id != eventDate.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    bool isFor221True = Request.Form["For221True"].Count > 0;
                    bool isFor221False = Request.Form["For221False"].Count > 0;

                    // Set the value of the "For221" field based on the checkboxes
                    if (isFor221True && isFor221False)
                    {
                        eventDate.For221 = For221.b;
                    }
                    else if (isFor221True)
                    {
                        eventDate.For221 = For221.y;
                    }
                    else if (isFor221False)
                    {
                        eventDate.For221 = For221.n;
                    }

                    _context.Update(eventDate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventDateExists(eventDate.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index","EventDates");
            }
            return View(eventDate);
        }

        // GET: EventDates/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            //if (id == null || _context.EventDate == null)
            //{
            //    return NotFound();
            //}

            var eventDate = await _context.EventDate
                .FirstOrDefaultAsync(m => m.Id == id);
            if (eventDate == null)
            {
                return NotFound();
            }

            return View(eventDate);
        }

        // POST: EventDates/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.EventDate == null)
            {
                return Problem("Entity set 'MockInterviewDataDbContext.EventDate'  is null.");
            }
            var eventDate = await _context.EventDate.FindAsync(id);
            if (eventDate != null)
            {
                _context.EventDate.Remove(eventDate);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Timeslots");
        }

        private bool EventDateExists(int id)
        {
          return (_context.EventDate?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
