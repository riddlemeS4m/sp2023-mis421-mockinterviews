using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using sp2023_mis421_mockinterviews.Data;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;

namespace sp2023_mis421_mockinterviews.Controllers
{
    [Authorize (Roles = RolesConstants.AdminRole + "," + RolesConstants.InterviewerRole)]
    public class SignupInterviewerTimeslotsController : Controller
    {
        private readonly MockInterviewDataDbContext _context;

        public SignupInterviewerTimeslotsController(MockInterviewDataDbContext context)
        {
            _context = context;
        }

        // GET: SignupInterviewerTimeslots
        public async Task<IActionResult> Index()
        {
            var mockInterviewDataDbContext = _context.SignupInterviewerTimeslot.Include(s => s.SignupInterviewer).Include(s => s.Timeslot);
            return View(await mockInterviewDataDbContext.ToListAsync());
        }

        // GET: SignupInterviewerTimeslots/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.SignupInterviewerTimeslot == null)
            {
                return NotFound();
            }

            var signupInterviewerTimeslot = await _context.SignupInterviewerTimeslot
                .Include(s => s.SignupInterviewer)
                .Include(s => s.Timeslot)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (signupInterviewerTimeslot == null)
            {
                return NotFound();
            }

            return View(signupInterviewerTimeslot);
        }

        // GET: SignupInterviewerTimeslots/Create
        public IActionResult Create()
        {
            ViewData["SignupInterviewerId"] = new SelectList(_context.SignupInterviewer, "Id", "Id");
            ViewData["TimeslotId"] = new SelectList(_context.Set<Timeslot>(), "Id", "Id");
            return View();
        }

        // POST: SignupInterviewerTimeslots/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,SignupInterviewerId,TimeslotId")] SignupInterviewerTimeslot signupInterviewerTimeslot)
        {
            if (ModelState.IsValid)
            {
                _context.Add(signupInterviewerTimeslot);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["SignupInterviewerId"] = new SelectList(_context.SignupInterviewer, "Id", "Id", signupInterviewerTimeslot.SignupInterviewerId);
            ViewData["TimeslotId"] = new SelectList(_context.Set<Timeslot>(), "Id", "Id", signupInterviewerTimeslot.TimeslotId);
            return View(signupInterviewerTimeslot);
        }

        // GET: SignupInterviewerTimeslots/Edit/5
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
            ViewData["SignupInterviewerId"] = new SelectList(_context.SignupInterviewer, "Id", "Id", signupInterviewerTimeslot.SignupInterviewerId);
            ViewData["TimeslotId"] = new SelectList(_context.Set<Timeslot>(), "Id", "Id", signupInterviewerTimeslot.TimeslotId);
            return View(signupInterviewerTimeslot);
        }

        // POST: SignupInterviewerTimeslots/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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
            ViewData["SignupInterviewerId"] = new SelectList(_context.SignupInterviewer, "Id", "Id", signupInterviewerTimeslot.SignupInterviewerId);
            ViewData["TimeslotId"] = new SelectList(_context.Set<Timeslot>(), "Id", "Id", signupInterviewerTimeslot.TimeslotId);
            return View(signupInterviewerTimeslot);
        }

        // GET: SignupInterviewerTimeslots/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.SignupInterviewerTimeslot == null)
            {
                return NotFound();
            }

            var signupInterviewerTimeslot = await _context.SignupInterviewerTimeslot
                .Include(s => s.SignupInterviewer)
                .Include(s => s.Timeslot)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (signupInterviewerTimeslot == null)
            {
                return NotFound();
            }

            return View(signupInterviewerTimeslot);
        }

        // POST: SignupInterviewerTimeslots/Delete/5
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
            return RedirectToAction(nameof(Index));
        }

        private bool SignupInterviewerTimeslotExists(int id)
        {
          return (_context.SignupInterviewerTimeslot?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
