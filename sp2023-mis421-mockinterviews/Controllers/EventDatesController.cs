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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Date,EventName")] EventDate eventDate)
        {
            if (id != eventDate.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
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
