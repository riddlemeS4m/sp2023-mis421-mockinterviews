using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using sp2023_mis421_mockinterviews.Data;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;
using sp2023_mis421_mockinterviews.Models.UserDb;
using sp2023_mis421_mockinterviews.Models.ViewModels;

namespace sp2023_mis421_mockinterviews.Controllers
{
    [Authorize(Roles = RolesConstants.StudentRole + "," + RolesConstants.AdminRole)]
    public class VolunteerEventsController : Controller
    {
        private readonly MockInterviewDataDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
    
        public VolunteerEventsController(MockInterviewDataDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: VolunteerEvents
        public async Task<IActionResult> Index()
        {
            var mockInterviewDataDbContext = _context.VolunteerEvent.Include(v => v.Timeslot);

            return View(await mockInterviewDataDbContext.ToListAsync());
        }

        // GET: VolunteerEvents/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.VolunteerEvent == null)
            {
                return NotFound();
            }

            var volunteerEvent = await _context.VolunteerEvent
                .Include(v => v.Timeslot)
                .FirstOrDefaultAsync(m => m.Id == id);
            var specificTimeslot = await _context.Timeslot
                .Include(v => v.EventDate)
                .FirstOrDefaultAsync(m => m.Id == volunteerEvent.Timeslot.Id);
            volunteerEvent.Timeslot = specificTimeslot;
            if (volunteerEvent == null)
            {
                return NotFound();
            }

            return View(volunteerEvent);
        }

        // GET: VolunteerEvents/Create
        public IActionResult Create()
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var timeslotsTask = _context.Timeslot
                .Where(x => x.IsVolunteer == true)
                .Include(y => y.EventDate)
                .Where(x => !_context.VolunteerEvent.Any(y => y.TimeslotId == x.Id && y.StudentId == userId))
                .ToListAsync();
            timeslotsTask.GetAwaiter().GetResult();
            var timeslots = timeslotsTask.Result;
            VolunteerEventsViewModel volunteerEventsViewModel = new VolunteerEventsViewModel
            {
                Timeslots = timeslots
            };
            return View(volunteerEventsViewModel);
        }

        // POST: VolunteerEvents/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int[] SelectedEventIds)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            foreach(int id in SelectedEventIds)
            {
                VolunteerEvent volunteerEvent = new VolunteerEvent
                {
                    TimeslotId = id,
                    StudentId = userId
                };
                if (ModelState.IsValid)
                {
                    _context.Add(volunteerEvent);
                    await _context.SaveChangesAsync();
                    //return RedirectToAction(nameof(Index));
                }
            }

            //var timeslots = await _context.Timeslot
            //    .Where(x => x.IsVolunteer == true)
            //    .Include(y => y.EventDate)
            //    .Where(x => !_context.VolunteerEvent.Any(y => y.TimeslotId == x.Id && y.StudentId == userId))
            //    .ToListAsync();
            //VolunteerEventsViewModel volunteerEventsViewModel = new VolunteerEventsViewModel
            //{
            //    Timeslots = timeslots
            //};
            return RedirectToAction("Index", "Home");
        }

        // GET: VolunteerEvents/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.VolunteerEvent == null)
            {
                return NotFound();
            }

            var volunteerEvent = await _context.VolunteerEvent.FindAsync(id);
            if (volunteerEvent == null)
            {
                return NotFound();
            }
            ViewData["TimeslotId"] = new SelectList(_context.Timeslot, "Id", "Id", volunteerEvent.TimeslotId);
            return View(volunteerEvent);
        }

        // POST: VolunteerEvents/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,StudentId,TimeslotId")] VolunteerEvent volunteerEvent)
        {
            if (id != volunteerEvent.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(volunteerEvent);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VolunteerEventExists(volunteerEvent.Id))
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
            //ViewData["StudentId"] = new SelectList(_context.Student, "Id", "Id", volunteerEvent.StudentId);
            ViewData["TimeslotId"] = new SelectList(_context.Timeslot, "Id", "Id", volunteerEvent.TimeslotId);
            return View(volunteerEvent);
        }

        // GET: VolunteerEvents/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.VolunteerEvent == null)
            {
                return NotFound();
            }

            var volunteerEvent = await _context.VolunteerEvent
                .Include(v => v.Timeslot)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (volunteerEvent == null)
            {
                return NotFound();
            }

            return View(volunteerEvent);
        }

        // POST: VolunteerEvents/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.VolunteerEvent == null)
            {
                return Problem("Entity set 'MockInterviewDataDbContext.VolunteerEvent'  is null.");
            }
            var volunteerEvent = await _context.VolunteerEvent.FindAsync(id);
            if (volunteerEvent != null)
            {
                _context.VolunteerEvent.Remove(volunteerEvent);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool VolunteerEventExists(int id)
        {
          return (_context.VolunteerEvent?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        private async Task<ActionResult> GetUserId()
        {
            var user = await _userManager.GetUserAsync(User);
            return Content(user.Id);
        }
    }
}
