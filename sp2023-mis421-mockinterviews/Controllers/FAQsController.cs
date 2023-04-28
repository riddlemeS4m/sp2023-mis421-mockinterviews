using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using sp2023_mis421_mockinterviews.Data;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;

namespace sp2023_mis421_mockinterviews.Controllers
{
    public class FAQsController : Controller
    {
        private readonly MockInterviewDataDbContext _context;
        private readonly string endpointUrl = "https://api.openai.com/v1/engines/davinci/completions";
        public FAQsController(MockInterviewDataDbContext context)
        {
            _context = context;
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

        // POST: FAQs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Question,Answer")] FAQs fAQs)
        {
            if (ModelState.IsValid)
            {
                _context.Add(fAQs);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(fAQs);
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

        // POST: FAQs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Question,Answer")] FAQs fAQs)
        {
            if (id != fAQs.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(fAQs);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FAQsExists(fAQs.Id))
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

        [HttpPost]
        public async Task<ActionResult<string>> Chat(string prompt)
        {
            try
            {
                Console.WriteLine("Made it");
			    var api = new OpenAI_API.OpenAIAPI("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");
                var chat = api.Chat.CreateConversation();
                chat.AppendUserInput(prompt);
                string textResponse = await chat.GetResponseFromChatbotAsync();
                Console.WriteLine(textResponse);
			    return Json(new { success = true, response = textResponse });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }



        private bool FAQsExists(int id)
        {
          return (_context.FAQs?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
