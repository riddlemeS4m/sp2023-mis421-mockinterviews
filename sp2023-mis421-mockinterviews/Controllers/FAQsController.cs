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
using sp2023_mis421_mockinterviews.Data;
using sp2023_mis421_mockinterviews.Data.Constants;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;
using sp2023_mis421_mockinterviews.Models.UserDb;

namespace sp2023_mis421_mockinterviews.Controllers
{
    public class FAQsController : Controller
    {
        private readonly MockInterviewDataDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        public FAQsController(MockInterviewDataDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: FAQs
        public async Task<IActionResult> Index()
        {
              return _context.FAQs != null ? 
                          View(await _context.FAQs.ToListAsync()) :
                          Problem("Entity set 'ApplicationDbContext.FAQs'  is null.");
        }
		public async Task<IActionResult> Resources()
		{
			return _context.FAQs != null ?
						View(await _context.FAQs.ToListAsync()) :
						Problem("Entity set 'ApplicationDbContext.FAQs'  is null.");
		}

		// GET: FAQs/Details/5
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
		public ActionResult Download()
		{
			string fileName = "Mock_Interview_Manual.docx"; // replace with your file name
			string filePath = "wwwroot\\lib\\" + fileName; // replace with your file path

			byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
			return File(fileBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
		}


		// GET: FAQs/Create
		public IActionResult Create()
        {
            return View();
        }

        // GET: FAQs/Edit/5
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

        // GET: FAQs/Delete/5
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
