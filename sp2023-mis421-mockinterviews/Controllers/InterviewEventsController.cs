using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using sp2023_mis421_mockinterviews.Data;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;

namespace sp2023_mis421_mockinterviews.Controllers
{
    public class InterviewEventsController : Controller
    {
        private readonly MockInterviewDataDbContext _context;

        public InterviewEventsController(MockInterviewDataDbContext context)
        {
            _context = context;
        }

        // GET: InterviewEvents
        public async Task<IActionResult> Index()
        {
            var mockInterviewDataDbContext = _context.InterviewEvent.Include(i => i.Location).Include(i => i.SignupInterviewerTimeslot).Include(i => i.Student).Include(i => i.Timeslot);
            return View(await mockInterviewDataDbContext.ToListAsync());
        }

        // GET: InterviewEvents/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.InterviewEvent == null)
            {
                return NotFound();
            }

            var interviewEvent = await _context.InterviewEvent
                .Include(i => i.Location)
                .Include(i => i.SignupInterviewerTimeslot)
                .Include(i => i.Student)
                .Include(i => i.Timeslot)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (interviewEvent == null)
            {
                return NotFound();
            }

            return View(interviewEvent);
        }

        // GET: InterviewEvents/Create
        public IActionResult Create()
        {
            ViewData["LocationId"] = new SelectList(_context.Location, "Id", "Id");
            ViewData["SignupInterviewerTimeslotId"] = new SelectList(_context.Set<SignupInterviewerTimeslot>(), "Id", "Id");
            ViewData["StudentId"] = new SelectList(_context.Set<Student>(), "Id", "Id");
            ViewData["TimeslotId"] = new SelectList(_context.Set<Timeslot>(), "Id", "Id");
            return View();
        }

        // POST: InterviewEvents/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,StudentId,LocationId,TimeslotId,InterviewType,Status,InterviewerRating,InterviewerFeedback,ProcessFeedback,SignupInterviewerTimeslotId")] InterviewEvent interviewEvent)
        {
            if (ModelState.IsValid)
            {
                _context.Add(interviewEvent);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["LocationId"] = new SelectList(_context.Location, "Id", "Id", interviewEvent.LocationId);
            ViewData["SignupInterviewerTimeslotId"] = new SelectList(_context.Set<SignupInterviewerTimeslot>(), "Id", "Id", interviewEvent.SignupInterviewerTimeslotId);
            ViewData["StudentId"] = new SelectList(_context.Set<Student>(), "Id", "Id", interviewEvent.StudentId);
            ViewData["TimeslotId"] = new SelectList(_context.Set<Timeslot>(), "Id", "Id", interviewEvent.TimeslotId);
            return View(interviewEvent);
        }

        // GET: InterviewEvents/Edit/5
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
            ViewData["LocationId"] = new SelectList(_context.Location, "Id", "Id", interviewEvent.LocationId);
            ViewData["SignupInterviewerTimeslotId"] = new SelectList(_context.Set<SignupInterviewerTimeslot>(), "Id", "Id", interviewEvent.SignupInterviewerTimeslotId);
            ViewData["StudentId"] = new SelectList(_context.Set<Student>(), "Id", "Id", interviewEvent.StudentId);
            ViewData["TimeslotId"] = new SelectList(_context.Set<Timeslot>(), "Id", "Id", interviewEvent.TimeslotId);
            return View(interviewEvent);
        }

        // POST: InterviewEvents/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,StudentId,LocationId,TimeslotId,InterviewType,Status,InterviewerRating,InterviewerFeedback,ProcessFeedback,SignupInterviewerTimeslotId")] InterviewEvent interviewEvent)
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
                return RedirectToAction(nameof(Index));
            }
            ViewData["LocationId"] = new SelectList(_context.Location, "Id", "Id", interviewEvent.LocationId);
            ViewData["SignupInterviewerTimeslotId"] = new SelectList(_context.Set<SignupInterviewerTimeslot>(), "Id", "Id", interviewEvent.SignupInterviewerTimeslotId);
            ViewData["StudentId"] = new SelectList(_context.Set<Student>(), "Id", "Id", interviewEvent.StudentId);
            ViewData["TimeslotId"] = new SelectList(_context.Set<Timeslot>(), "Id", "Id", interviewEvent.TimeslotId);
            return View(interviewEvent);
        }

        // GET: InterviewEvents/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.InterviewEvent == null)
            {
                return NotFound();
            }

            var interviewEvent = await _context.InterviewEvent
                .Include(i => i.Location)
                .Include(i => i.SignupInterviewerTimeslot)
                .Include(i => i.Student)
                .Include(i => i.Timeslot)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (interviewEvent == null)
            {
                return NotFound();
            }

            return View(interviewEvent);
        }

        // POST: InterviewEvents/Delete/5
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
            return RedirectToAction(nameof(Index));
        }

        private bool InterviewEventExists(int id)
        {
          return (_context.InterviewEvent?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
