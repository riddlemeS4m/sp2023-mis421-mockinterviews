using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SendGrid;
using sp2023_mis421_mockinterviews.Data;
using sp2023_mis421_mockinterviews.Data.Access;
using sp2023_mis421_mockinterviews.Data.Access.Emails;
using sp2023_mis421_mockinterviews.Data.Constants;
using sp2023_mis421_mockinterviews.Interfaces;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;
using sp2023_mis421_mockinterviews.Models.UserDb;

namespace sp2023_mis421_mockinterviews.Controllers
{
    public class FAQsController : Controller
    {
        private readonly MockInterviewDataDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISendGridClient _sendGridClient;
        public FAQsController(MockInterviewDataDbContext context, UserManager<ApplicationUser> userManager, ISendGridClient sendGridClient)
        {
            _context = context;
            _userManager = userManager;
            _sendGridClient = sendGridClient;
        }

        // GET: FAQs
        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> Index()
        {
              return _context.FAQs != null ? 
                          View(await _context.FAQs.ToListAsync()) :
                          Problem("Entity set 'ApplicationDbContext.FAQs'  is null.");
        }
		public async Task<IActionResult> Resources()
		{
			return _context.FAQs != null ?
						View(await _context.FAQs.Where(x => x.Answer != null).ToListAsync()) :
						Problem("Entity set 'ApplicationDbContext.FAQs'  is null.");
		}

        // GET: FAQs/Details/5
        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.FAQs == null)
            {
                return NotFound();
            }

            var fAQs = await _context.FAQs
                .FirstOrDefaultAsync(m => m.Id == id);
            if (fAQs == null)
            {
                return NotFound();
            }

            return View(fAQs);
        }
        [Authorize(Roles = RolesConstants.AdminRole + "," + RolesConstants.StudentRole + "," + RolesConstants.InterviewerRole)]
        public ActionResult DownloadManual()
		{
			string fileName = "MockInterviewManual_Spring2024.docx";
			string filePath = "wwwroot/lib/" + fileName;

			byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
			return File(fileBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
		}

        [Authorize(Roles = RolesConstants.AdminRole + "," + RolesConstants.StudentRole + "," + RolesConstants.InterviewerRole)]
        public ActionResult DownloadParking()
        {
            string fileName = "GuestParking_Spring2024.pdf";
            string filePath = "wwwroot/lib/" + fileName;

            byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "application/pdf", fileName);
        }

        // GET: FAQs/Create
        [Authorize(Roles = RolesConstants.AdminRole + "," + RolesConstants.StudentRole + "," + RolesConstants.InterviewerRole)]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesConstants.AdminRole + "," + RolesConstants.StudentRole + "," + RolesConstants.InterviewerRole)]
        public async Task<IActionResult> Create([Bind("Id, Question, Answer")] FAQs faq)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (ModelState.IsValid)
            {
                
                _context.Add(faq);
                await _context.SaveChangesAsync();
                if(User.IsInRole(RolesConstants.AdminRole))
                {
                    return RedirectToAction("Index", "FAQs");
                }
                else
                {
                    ASendAnEmail emailer = new NewFAQSubmitted();
                    await emailer.SendEmailAsync(_sendGridClient, "Answer Required: Student Submitted New Question", SuperUser.Email, user.FirstName + " " + user.LastName, faq.Question, null);

                    return RedirectToAction("Resources", "FAQs");
                }
                
            }
            return View(faq);
        }

        // GET: FAQs/Edit/5
        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.FAQs == null)
            {
                return NotFound();
            }

            var fAQs = await _context.FAQs.FindAsync(id);
            if (fAQs == null)
            {
                return NotFound();
            }
            return View(fAQs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> Edit(int id, [Bind("Id, Question, Answer")] FAQs faq)
        {
            if (id != faq.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(faq);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FAQsExists(faq.Id))
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
            return View(faq);
        }

        // GET: FAQs/Delete/5
        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.FAQs == null)
            {
                return NotFound();
            }

            var fAQs = await _context.FAQs
                .FirstOrDefaultAsync(m => m.Id == id);
            if (fAQs == null)
            {
                return NotFound();
            }

            return View(fAQs);
        }

        // POST: FAQs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.FAQs == null)
            {
                return Problem("Entity set 'ApplicationDbContext.FAQs'  is null.");
            }
            var fAQs = await _context.FAQs.FindAsync(id);
            if (fAQs != null)
            {
                _context.FAQs.Remove(fAQs);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        private bool FAQsExists(int id)
        {
          return (_context.FAQs?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
