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
    public class EmailTemplatesController : Controller
    {
        private readonly MockInterviewDataDbContext _context;

        public EmailTemplatesController(MockInterviewDataDbContext context)
        {
            _context = context;
        }

        // GET: EmailTemplates
        public async Task<IActionResult> Index()
        {
              return View(await _context.EmailTemplate.ToListAsync());
        }

        // GET: EmailTemplates/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.EmailTemplate == null)
            {
                return NotFound();
            }

            var emailTemplate = await _context.EmailTemplate
                .FirstOrDefaultAsync(m => m.Id == id);
            if (emailTemplate == null)
            {
                return NotFound();
            }

            return View(emailTemplate);
        }

        // GET: EmailTemplates/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: EmailTemplates/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,SubjectLine,Body")] EmailTemplate emailTemplate)
        {
            if (ModelState.IsValid)
            {
                _context.Add(emailTemplate);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(emailTemplate);
        }

        // GET: EmailTemplates/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.EmailTemplate == null)
            {
                return NotFound();
            }

            var emailTemplate = await _context.EmailTemplate.FindAsync(id);
            if (emailTemplate == null)
            {
                return NotFound();
            }
            return View(emailTemplate);
        }

        // POST: EmailTemplates/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,SubjectLine,Body")] EmailTemplate emailTemplate)
        {
            if (id != emailTemplate.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(emailTemplate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmailTemplateExists(emailTemplate.Id))
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
            return View(emailTemplate);
        }

        // GET: EmailTemplates/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.EmailTemplate == null)
            {
                return NotFound();
            }

            var emailTemplate = await _context.EmailTemplate
                .FirstOrDefaultAsync(m => m.Id == id);
            if (emailTemplate == null)
            {
                return NotFound();
            }

            return View(emailTemplate);
        }

        // POST: EmailTemplates/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.EmailTemplate == null)
            {
                return Problem("Entity set 'MockInterviewDataDbContext.EmailTemplate'  is null.");
            }
            var emailTemplate = await _context.EmailTemplate.FindAsync(id);
            if (emailTemplate != null)
            {
                _context.EmailTemplate.Remove(emailTemplate);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EmailTemplateExists(int id)
        {
          return _context.EmailTemplate.Any(e => e.Id == id);
        }
    }
}
