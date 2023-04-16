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
    [Authorize (Roles = RolesConstants.AdminRole + "," + RolesConstants.InterviewerRole)]
    public class SignupInterviewerTimeslotsController : Controller
    {
        private readonly MockInterviewDataDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SignupInterviewerTimeslotsController(MockInterviewDataDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: SignupInterviewerTimeslots
        public async Task<IActionResult> Index()
        {
            var signupInterviewerTimeslots = await _context.SignupInterviewerTimeslot
                .Include(s => s.SignupInterviewer)
                .Include(s => s.Timeslot)
                .ThenInclude(s => s.EventDate)
                .Where(s => s.Timeslot.IsInterviewer)
                .ToListAsync();

            return View(signupInterviewerTimeslots);
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
                .ThenInclude(s => s.EventDate)
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
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userTask = _userManager.FindByIdAsync(userId);
            userTask.GetAwaiter().GetResult();
            var user = userTask.Result;


            var timeslotsTask = _context.Timeslot
                .Where(x => x.IsInterviewer == true)
                .Include(y => y.EventDate)
                .Where(x => !_context.SignupInterviewerTimeslot.Any(y => y.TimeslotId == x.Id && y.SignupInterviewer.InterviewerId == userId))
                .ToListAsync();
            timeslotsTask.GetAwaiter().GetResult();
            var timeslots = timeslotsTask.Result;
            SignupInterviewerTimeslotsViewModel volunteerEventsViewModel = new SignupInterviewerTimeslotsViewModel
            {
                Timeslots = timeslots,
                SignupInterviewer = new SignupInterviewer
                { 
                    InterviewerId = userId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    IsBehavioral = false,
                    IsTechnical = false,
                    IsVirtual = false,
                    InPerson = false
                }
            };
            return View(volunteerEventsViewModel);

        }

        // POST: SignupInterviewerTimeslots/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int[] SelectedEventIds, [Bind("IsTechnical,IsBehavioral,IsVirtual,InPerson")] SignupInterviewer signupInterviewer )
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            var existingSignupInterviewer = await _context.SignupInterviewer.FirstOrDefaultAsync(si =>
                    si.FirstName == user.FirstName &&
                    si.LastName == user.LastName &&
                    si.IsVirtual == signupInterviewer.IsVirtual &&
                    si.InPerson == signupInterviewer.InPerson &&
                    si.IsTechnical == signupInterviewer.IsTechnical &&
                    si.IsBehavioral == signupInterviewer.IsBehavioral &&
                    si.InterviewerId == userId);

            SignupInterviewer post;
            if (existingSignupInterviewer != null)
            {
                post = existingSignupInterviewer;
            }
            else
            {
                post = new SignupInterviewer
                {
                    InterviewerId = userId,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    InPerson = signupInterviewer.InPerson,
                    IsVirtual = !signupInterviewer.InPerson,
                    IsBehavioral = signupInterviewer.IsBehavioral,
                    IsTechnical = signupInterviewer.IsTechnical
                };

                if (ModelState.IsValid)
                {
                    _context.Add(post);
                    await _context.SaveChangesAsync();
                }
            }

            int signupInterviewerId = post.Id;

            foreach (int id in SelectedEventIds)
            {
                var timeslots = new List<SignupInterviewerTimeslot>
                {
                    new SignupInterviewerTimeslot { TimeslotId = id, SignupInterviewerId = signupInterviewerId },
                    new SignupInterviewerTimeslot { TimeslotId = id + 1, SignupInterviewerId = signupInterviewerId }
                };

                foreach (var timeslot in timeslots)
                {
                    if (ModelState.IsValid)
                    {
                        _context.Add(timeslot);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            return RedirectToAction("Index", "Home");
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

            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
           

            var timeslots = await _context.Timeslot
                .Where(x => x.IsInterviewer == true)
                .Include(y => y.EventDate)
                //.Where(x => !_context.SignupInterviewerTimeslot.Any(y => y.TimeslotId == x.Id && y.SignupInterviewer.InterviewerId == userId))
                .ToListAsync();
            
            SignupInterviewerTimeslotsViewModel volunteerEventsViewModel = new SignupInterviewerTimeslotsViewModel
            {
                Timeslots = timeslots,
                SignupInterviewer = signupInterviewerTimeslot.SignupInterviewer
            };
            return View(volunteerEventsViewModel);
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
            //ViewData["SignupInterviewerId"] = new SelectList(_context.SignupInterviewer, "Id", "Id", signupInterviewerTimeslot.SignupInterviewerId);
            //ViewData["TimeslotId"] = new SelectList(_context.Set<Timeslot>(), "Id", "Id", signupInterviewerTimeslot.TimeslotId);
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
