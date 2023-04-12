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
    public class InterviewersController : Controller
    {
        private readonly MockInterviewDataDbContext _context;

        public InterviewersController(MockInterviewDataDbContext context)
        {
            _context = context;
        }

        // GET: Interviewers
        public async Task<IActionResult> Index()
        {
              return _context.Interviewer != null ? 
                          View(await _context.Interviewer.ToListAsync()) :
                          Problem("Entity set 'MockInterviewDataDbContext.Interviewer'  is null.");
        }

        // GET: Interviewers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Interviewer == null)
            {
                return NotFound();
            }

            var interviewer = await _context.Interviewer
                .FirstOrDefaultAsync(m => m.Id == id);
            if (interviewer == null)
            {
                return NotFound();
            }

            return View(interviewer);
        }

        // GET: Interviewers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Interviewers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FirstName,LastName,Rating,IsActive")] Interviewer interviewer)
        {
            if (ModelState.IsValid)
            {
                _context.Add(interviewer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(interviewer);
        }

        // GET: Interviewers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Interviewer == null)
            {
                return NotFound();
            }

            var interviewer = await _context.Interviewer.FindAsync(id);
            if (interviewer == null)
            {
                return NotFound();
            }
            return View(interviewer);
        }

        // POST: Interviewers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,Rating,IsActive")] Interviewer interviewer)
        {
            if (id != interviewer.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(interviewer);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InterviewerExists(interviewer.Id))
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
            return View(interviewer);
        }

        // GET: Interviewers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Interviewer == null)
            {
                return NotFound();
            }

            var interviewer = await _context.Interviewer
                .FirstOrDefaultAsync(m => m.Id == id);
            if (interviewer == null)
            {
                return NotFound();
            }

            return View(interviewer);
        }

        // POST: Interviewers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Interviewer == null)
            {
                return Problem("Entity set 'MockInterviewDataDbContext.Interviewer'  is null.");
            }
            var interviewer = await _context.Interviewer.FindAsync(id);
            if (interviewer != null)
            {
                _context.Interviewer.Remove(interviewer);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool InterviewerExists(int id)
        {
          return (_context.Interviewer?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
