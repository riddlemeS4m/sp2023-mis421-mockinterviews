using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using sp2023_mis421_mockinterviews.Data;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;
using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.AspNetCore.Authorization;
using sp2023_mis421_mockinterviews.Data.Constants;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace sp2023_mis421_mockinterviews.Controllers
{
    public class SignupInterviewersController : Controller
    {
        private readonly MockInterviewDataDbContext _context;
        private readonly ISendGridClient _sendGridClient;

        public SignupInterviewersController(MockInterviewDataDbContext context,
            ISendGridClient sendGridClient)
        {
            _context = context;
            _sendGridClient = sendGridClient;
        }

        // GET: SignupInterviewers
        [Authorize(Roles = RolesConstants.AdminRole + "," + RolesConstants.InterviewerRole)]
        public async Task<IActionResult> Index()
        {
            var sits = await _context.SignupInterviewerTimeslot
                .Include(s => s.SignupInterviewer)
                .Include(s => s.Timeslot)
                .ThenInclude(s => s.EventDate)
                .Where(s => s.Timeslot.EventDate.IsActive)
                .Select(s => s.SignupInterviewerId)
                .Distinct()
                .ToListAsync();

            var sis = await _context.SignupInterviewer
                .Where(s => sits.Contains(s.Id))
                .ToListAsync();

            return View(sis);
        }

        // GET: SignupInterviewers/Details/5
        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.SignupInterviewer == null)
            {
                return NotFound();
            }

            var signupInterviewer = await _context.SignupInterviewer
                .FirstOrDefaultAsync(m => m.Id == id);
            if (signupInterviewer == null)
            {
                return NotFound();
            }

            return View(signupInterviewer);
        }

        // GET: SignupInterviewers/Create
        [Authorize(Roles = RolesConstants.InterviewerRole)]
        public IActionResult Create()
        {
            return View();
        }

        // POST: SignupInterviewers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = RolesConstants.InterviewerRole)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FirstName,LastName,IsVirtual,InPerson,IsTechnical,IsBehavioral,InterviewerId")] SignupInterviewer signupInterviewer)
        {
            if (ModelState.IsValid)
            {
                _context.Add(signupInterviewer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(signupInterviewer);
        }

        // GET: SignupInterviewers/Edit/5
        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.SignupInterviewer == null)
            {
                return NotFound();
            }

            var signupInterviewer = await _context.SignupInterviewer.FindAsync(id);
            if (signupInterviewer == null)
            {
                return NotFound();
            }

            return View(signupInterviewer);
        }

        // POST: SignupInterviewers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = RolesConstants.AdminRole)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,IsVirtual,InPerson,IsTechnical,IsBehavioral,InterviewerId")] SignupInterviewer signupInterviewer)
        {
            if (id != signupInterviewer.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(signupInterviewer);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SignupInterviewerExists(signupInterviewer.Id))
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

            return View(signupInterviewer);
        }

        // GET: SignupInterviewers/Delete/5
        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.SignupInterviewer == null)
            {
                return NotFound();
            }

            var signupInterviewer = await _context.SignupInterviewer
                .FirstOrDefaultAsync(m => m.Id == id);
            if (signupInterviewer == null)
            {
                return NotFound();
            }

            return View(signupInterviewer);
        }

        // POST: SignupInterviewers/Delete/5
        [Authorize(Roles = RolesConstants.AdminRole)]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.SignupInterviewer == null)
            {
                return Problem("Entity set 'MockInterviewDataDbContext.SignupInterviewer'  is null.");
            }
            var signupInterviewer = await _context.SignupInterviewer.FindAsync(id);
            if (signupInterviewer != null)
            {
                _context.SignupInterviewer.Remove(signupInterviewer);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> CheckInInterviewer(int id)
        {
            if (_context.SignupInterviewer == null || id == 0)
            {
                return NotFound();
            }

            var si = await _context.SignupInterviewer.FindAsync(id);

            if(si == null)
            {                 
                return NotFound();  
            }

            try
            {
                si.CheckedIn = !si.CheckedIn;
                _context.Update(si);
                await _context.SaveChangesAsync();

                //return NoContent();
                return RedirectToAction(nameof(Index));
            } 
            catch
            {
                return BadRequest(new InvalidOperationException("Interviewer was unable to be checked in."));
            }
        }
        private bool SignupInterviewerExists(int id)
        {
          return (_context.SignupInterviewer?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
