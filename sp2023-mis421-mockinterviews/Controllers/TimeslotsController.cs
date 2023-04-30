using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using sp2023_mis421_mockinterviews.Data;
using sp2023_mis421_mockinterviews.Data.Constants;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;
using sp2023_mis421_mockinterviews.Models.ViewModels;

namespace sp2023_mis421_mockinterviews.Controllers
{
    [Authorize(Roles = RolesConstants.AdminRole)]
    public class TimeslotsController : Controller
    {
        private readonly MockInterviewDataDbContext _context;

        public TimeslotsController(MockInterviewDataDbContext context)
        {
            _context = context;
        }

        // GET: Timeslots
        public async Task<IActionResult> Index()
        {
            var timeslots = await _context.Timeslot.Include(t => t.EventDate).ToListAsync();
            var eventdates = await _context.EventDate.ToListAsync();

            var viewModel = new TimeslotViewModel()
            {
                Timeslots = timeslots,
                EventDates = eventdates
            };

            return View(viewModel);

            //return _context.Timeslot != null ?
            //            View(await _context.Timeslot.Include(t => t.EventDate).ToListAsync()):
            //              Problem("Entity set 'MockInterviewDataDbContext.Timeslot'  is null.");
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

            return View(timeslot);
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Time,IsActive,IsVolunteer,IsInterviewer,IsStudent,MaxSignUps")] Timeslot timeslot)
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
