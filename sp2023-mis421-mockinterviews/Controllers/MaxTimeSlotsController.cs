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
    public class MaxTimeSlotsController : Controller
    {
        private readonly MockInterviewDataDbContext _context;

        public MaxTimeSlotsController(MockInterviewDataDbContext context)
        {
            _context = context;
        }

        // GET: MaxTimeSlots
        public async Task<IActionResult> Index()
        {
              return _context.MaxTimeSlots != null ? 
                          View(await _context.MaxTimeSlots.ToListAsync()) :
                          Problem("Entity set 'MockInterviewDataDbContext.MaxTimeSlots'  is null.");
        }

        // GET: MaxTimeSlots/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.MaxTimeSlots == null)
            {
                return NotFound();
            }

            var maxTimeSlots = await _context.MaxTimeSlots
                .FirstOrDefaultAsync(m => m.Id == id);
            if (maxTimeSlots == null)
            {
                return NotFound();
            }

            return View(maxTimeSlots);
        }

        // GET: MaxTimeSlots/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: MaxTimeSlots/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Limit")] MaxTimeSlots maxTimeSlots)
        {
            if (ModelState.IsValid)
            {
                _context.Add(maxTimeSlots);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(maxTimeSlots);
        }

        // GET: MaxTimeSlots/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.MaxTimeSlots == null)
            {
                return NotFound();
            }

            var maxTimeSlots = await _context.MaxTimeSlots.FindAsync(id);
            if (maxTimeSlots == null)
            {
                return NotFound();
            }
            return View(maxTimeSlots);
        }

        // POST: MaxTimeSlots/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Limit")] MaxTimeSlots maxTimeSlots)
        {
            if (id != maxTimeSlots.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(maxTimeSlots);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MaxTimeSlotsExists(maxTimeSlots.Id))
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
            return View(maxTimeSlots);
        }

        // GET: MaxTimeSlots/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.MaxTimeSlots == null)
            {
                return NotFound();
            }

            var maxTimeSlots = await _context.MaxTimeSlots
                .FirstOrDefaultAsync(m => m.Id == id);
            if (maxTimeSlots == null)
            {
                return NotFound();
            }

            return View(maxTimeSlots);
        }

        // POST: MaxTimeSlots/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.MaxTimeSlots == null)
            {
                return Problem("Entity set 'MockInterviewDataDbContext.MaxTimeSlots'  is null.");
            }
            var maxTimeSlots = await _context.MaxTimeSlots.FindAsync(id);
            if (maxTimeSlots != null)
            {
                _context.MaxTimeSlots.Remove(maxTimeSlots);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MaxTimeSlotsExists(int id)
        {
          return (_context.MaxTimeSlots?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
