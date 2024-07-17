﻿using System;
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
using sp2023_mis421_mockinterviews.Models.ViewModels;
using sp2023_mis421_mockinterviews.Services.GoogleDrive;

namespace sp2023_mis421_mockinterviews.Controllers
{
    public class FAQsController : Controller
    {
        private readonly MockInterviewDataDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISendGridClient _sendGridClient;
        private readonly ILogger<FAQsController> _logger;
        private readonly GoogleDriveSiteContentService _driveService;
        public FAQsController(MockInterviewDataDbContext context, 
            UserManager<ApplicationUser> userManager, 
            ISendGridClient sendGridClient,
            ILogger<FAQsController> logger,
            GoogleDriveSiteContentService googleDriveSiteContentService)
        {
            _context = context;
            _userManager = userManager;
            _sendGridClient = sendGridClient;
            _logger = logger;
            _driveService = googleDriveSiteContentService;
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

        [AllowAnonymous]
        public async Task<ActionResult> DownloadManual()
		{
            try
            {
                var fileId = await _context.GlobalConfigVar
                                .Where(x => x.Name == GoogleDriveServiceSeed.ManualConfigVar)
                                .Select(x => x.Value)
                                .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(fileId))
                {
                    return NotFound("File id not found.");
                }

                (Google.Apis.Drive.v3.Data.File fileMetadata, MemoryStream fileContent) = await _driveService.GetOneFile(fileId, true);

                if (fileMetadata == null || fileContent == null)
                {
                    return NotFound("File not found in remote storage.");
                }

                fileContent.Position = 0;

                return File(fileContent, GoogleDriveUtility.GetMimeType(fileMetadata.Name), fileMetadata.Name);
            }
            catch (Exception ex)
            {
                return BadRequest($"The server sent this message: {ex.Message}");
            }
        }

        [AllowAnonymous]
        public async Task<ActionResult> DownloadParking()
        {
            try
            {
                var fileId = await _context.GlobalConfigVar
                .Where(x => x.Name == GoogleDriveServiceSeed.ParkingPassConfigVar)
                .Select(x => x.Value)
                .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(fileId))
                {
                    return NotFound("File id not found.");
                }

                (Google.Apis.Drive.v3.Data.File fileMetadata, MemoryStream fileContent) = await _driveService.GetOneFile(fileId, true);

                if (fileMetadata == null || fileContent == null)
                {
                    return NotFound("File not found in remote storage.");
                }

                fileContent.Position = 0;

                return File(fileContent, GoogleDriveUtility.GetMimeType(fileMetadata.Name), fileMetadata.Name);
            }
            catch(Exception ex)
            {
                return BadRequest($"The server sent this message: {ex.Message}");
            }
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

        [HttpGet]
        [Authorize(Roles = RolesConstants.AdminRole)]
        public IActionResult UpdateSiteContent()
        {
            var model = new UploadSiteContentViewModel();

            return View("UpdateSiteContent", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> UpdateSiteContent(IFormFile Manual, IFormFile ParkingPass)
        {
            if (Manual == null && ParkingPass == null)
            {
                var vm = new UploadSiteContentViewModel();
                ModelState.AddModelError("Manual", "Please select at least one file.");
                ModelState.AddModelError("ParkingPass", "Please select at least one file.");
                return View("UpdateSiteContent", vm);
            }

            try
            {
                if (Manual != null)
                {
                    var configVar = await _context.GlobalConfigVar
                        .Where(x => x.Name == GoogleDriveServiceSeed.ManualConfigVar)
                        .FirstOrDefaultAsync();

                    if (configVar == null)
                    {
                        return NotFound("File id not found.");
                    }

                    await _driveService.DeleteFile(configVar.Value);

                    _context.Remove(configVar);
                    await _context.SaveChangesAsync();

                    await _driveService.UploadFile(Manual);

                }

                if (ParkingPass != null)
                {
                    var configVar = await _context.GlobalConfigVar
                        .Where(x => x.Name == GoogleDriveServiceSeed.ParkingPassConfigVar)
                        .FirstOrDefaultAsync();

                    if (configVar == null)
                    {
                        return NotFound("File id not found.");
                    }

                    await _driveService.DeleteFile(configVar.Value);

                    _context.Remove(configVar);
                    await _context.SaveChangesAsync();

                    await _driveService.UploadFile(ParkingPass);
                }

                var googleDriveSeed = new GoogleDriveServiceSeed(_driveService, _context);
                await googleDriveSeed.Test();

                return RedirectToAction("Index", "FAQs");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        private bool FAQsExists(int id)
        {
          return (_context.FAQs?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
