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
    public class LocationInterviewersController : Controller
    {
        private readonly MockInterviewDataDbContext _context;

        public LocationInterviewersController(MockInterviewDataDbContext context)
        {
            _context = context;
        }

        // GET: LocationInterviewers
        public async Task<IActionResult> Index()
        {
            var mockInterviewDataDbContext = _context.LocationInterviewer.Include(l => l.Location);
            return View(await mockInterviewDataDbContext.ToListAsync());
        }

        // GET: LocationInterviewers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.LocationInterviewer == null)
            {
                return NotFound();
            }

            var locationInterviewer = await _context.LocationInterviewer
                .Include(l => l.Location)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (locationInterviewer == null)
            {
                return NotFound();
            }

            return View(locationInterviewer);
        }

        // GET: LocationInterviewers/Create
        public IActionResult Create()
        {
            //ViewData["InterviewerId"] = new SelectList(_context.Interviewer, "Id", "Id");
            ViewData["LocationId"] = new SelectList(_context.Location, "Id", "Id");
            return View();
        }

        // POST: LocationInterviewers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,InterviewerId,LocationId")] LocationInterviewer locationInterviewer)
        {
            if (ModelState.IsValid)
            {
                _context.Add(locationInterviewer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            //ViewData["InterviewerId"] = new SelectList(_context.Interviewer, "Id", "Id", locationInterviewer.InterviewerId);
            ViewData["LocationId"] = new SelectList(_context.Location, "Id", "Id", locationInterviewer.LocationId);
            return View(locationInterviewer);
        }

        // GET: LocationInterviewers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.LocationInterviewer == null)
            {
                return NotFound();
            }

            var locationInterviewer = await _context.LocationInterviewer.FindAsync(id);
            if (locationInterviewer == null)
            {
                return NotFound();
            }
            //ViewData["InterviewerId"] = new SelectList(_context.Interviewer, "Id", "Id", locationInterviewer.InterviewerId);
            ViewData["LocationId"] = new SelectList(_context.Location, "Id", "Id", locationInterviewer.LocationId);
            return View(locationInterviewer);
        }

        // POST: LocationInterviewers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,InterviewerId,LocationId")] LocationInterviewer locationInterviewer)
        {
            if (id != locationInterviewer.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(locationInterviewer);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LocationInterviewerExists(locationInterviewer.Id))
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
            //ViewData["InterviewerId"] = new SelectList(_context.Interviewer, "Id", "Id", locationInterviewer.InterviewerId);
            ViewData["LocationId"] = new SelectList(_context.Location, "Id", "Id", locationInterviewer.LocationId);
            return View(locationInterviewer);
        }

        // GET: LocationInterviewers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.LocationInterviewer == null)
            {
                return NotFound();
            }

            var locationInterviewer = await _context.LocationInterviewer
                .Include(l => l.Location)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (locationInterviewer == null)
            {
                return NotFound();
            }

            return View(locationInterviewer);
        }

        // POST: LocationInterviewers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.LocationInterviewer == null)
            {
                return Problem("Entity set 'MockInterviewDataDbContext.LocationInterviewer'  is null.");
            }
            var locationInterviewer = await _context.LocationInterviewer.FindAsync(id);
            if (locationInterviewer != null)
            {
                _context.LocationInterviewer.Remove(locationInterviewer);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LocationInterviewerExists(int id)
        {
          return (_context.LocationInterviewer?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
