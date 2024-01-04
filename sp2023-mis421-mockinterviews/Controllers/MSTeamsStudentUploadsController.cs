using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using sp2023_mis421_mockinterviews.Data;
using sp2023_mis421_mockinterviews.Data.Constants;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;
using sp2023_mis421_mockinterviews.Models.ViewModels;

namespace sp2023_mis421_mockinterviews.Controllers
{
    public class MSTeamsStudentUploadsController : Controller
    {
        private readonly MockInterviewDataDbContext _context;

        public MSTeamsStudentUploadsController(MockInterviewDataDbContext context)
        {
            _context = context;
        }

        // GET: MSTeamsStudentUploads
        [Authorize(Roles = RolesConstants.AdminRole)]

        public async Task<IActionResult> Index()
        {
              return _context.MSTeamsStudentUpload != null ? 
                          View(await _context.MSTeamsStudentUpload.ToListAsync()) :
                          Problem("Entity set 'MockInterviewDataDbContext.MSTeamsStudentUpload'  is null.");
        }

        // GET: MSTeamsStudentUploads/Details/5
        [Authorize(Roles = RolesConstants.AdminRole)]

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.MSTeamsStudentUpload == null)
            {
                return NotFound();
            }

            var mSTeamsStudentUpload = await _context.MSTeamsStudentUpload
                .FirstOrDefaultAsync(m => m.Id == id);
            if (mSTeamsStudentUpload == null)
            {
                return NotFound();
            }

            return View(mSTeamsStudentUpload);
        }

        // GET: MSTeamsStudentUploads/Create
        [Authorize(Roles = RolesConstants.AdminRole)]

        public IActionResult Create()
        {
            var viewModel = new MSTeamsStudentUploadViewModel();
            return View(viewModel);
        }

        // POST: MSTeamsStudentUploads/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> Create(MSTeamsStudentUploadViewModel viewModel)
        {
            var RosterData = viewModel.RosterData;

            if (ModelState.IsValid)
            {
                if (RosterData == null || RosterData.Length == 0)
                {
                    return RedirectToAction("Index", "Home");
                }

                try
                {
                    _context.MSTeamsStudentUpload.RemoveRange(_context.MSTeamsStudentUpload);
                    await _context.SaveChangesAsync();

                    var records = new List<MSTeamsStudentUpload>();

                    using (var stream = RosterData.OpenReadStream())
                    using (var parser = new TextFieldParser(stream))
                    {
                        parser.TextFieldType = FieldType.Delimited;
                        parser.SetDelimiters(",");

                        while (!parser.EndOfData)
                        {
                            // Read current line as an array of fields
                            string[] fields = parser.ReadFields();

                            if (fields.Length >= 3)
                            {
                                var record = new MSTeamsStudentUpload
                                {
                                    MicrosoftId = fields[0],
                                    Email = fields[1],
                                    Name = fields[2]
                                };
                                records.Add(record);
                            }
                        }
                    }

                    var filteredRecords = records
                        .Where(record => record.Email[(record.Email.IndexOf('@') + 1)..] == "crimson.ua.edu")
                        .ToList();

                    // Now, you have a list of Roster objects (records)
                    // You can save them to the database using Entity Framework Core

                    // Example: Save records to the database
                    await _context.MSTeamsStudentUpload.AddRangeAsync(filteredRecords);
                    await _context.SaveChangesAsync();

                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    return BadRequest($"Error processing CSV file: {ex.Message}");
                }
            }

            return BadRequest("Something went wrong.");
        }

        public IActionResult UploadMastersStudents()
        {
            var viewModel = new MSTeamsStudentUploadViewModel();
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> UploadMastersStudents(MSTeamsStudentUploadViewModel viewModel)
        {
            var RosterData = viewModel.RosterData;

            if (ModelState.IsValid)
            {
                if (RosterData == null || RosterData.Length == 0)
                {
                    return RedirectToAction("Index", "Home");
                }

                try
                {
                    var records = new List<MSTeamsStudentUpload>();

                    using (var stream = RosterData.OpenReadStream())
                    using (var parser = new TextFieldParser(stream))
                    {
                        parser.TextFieldType = FieldType.Delimited;
                        parser.SetDelimiters(",");

                        while (!parser.EndOfData)
                        {
                            //expected format is LastName in column 1, FirstName in column 2, and Email in column 3
                            string[] fields = parser.ReadFields();

                            if (fields.Length >= 3)
                            {
                                var record = new MSTeamsStudentUpload
                                {
                                    Email = fields[2],
                                    Name = fields[1] + " " + fields[0],
                                    InMasters = true
                                };
                                records.Add(record);
                            }
                        }
                    }

                    foreach(var record in records)
                    {
                        var studentExists = await _context.MSTeamsStudentUpload.FirstOrDefaultAsync(x => x.Email == record.Email);
                        if (studentExists == null)
                        {
                            if(record.Email != "Email" && record.Email[(record.Email.IndexOf('@') + 1)..] != "crimson.ua.edu")
                            {
                                await _context.MSTeamsStudentUpload.AddAsync(record);
                            }
                        }
                        else if(studentExists != null)
                        {
                            studentExists.InMasters = true;
                            _context.MSTeamsStudentUpload.Update(studentExists);
                        }
                    }
                    
                    await _context.SaveChangesAsync();

                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    return BadRequest($"Error processing CSV file: {ex.Message}");
                }
            }

            return BadRequest("Something went wrong.");
        }

        public IActionResult Upload221Students()
        {
            var viewModel = new MSTeamsStudentUploadViewModel();
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> Upload221Students(MSTeamsStudentUploadViewModel viewModel)
        {
            var RosterData = viewModel.RosterData;

            if (ModelState.IsValid)
            {
                if (RosterData == null || RosterData.Length == 0)
                {
                    return RedirectToAction("Index", "Home");
                }

                try
                {
                    var records = new List<MSTeamsStudentUpload>();

                    using (var stream = RosterData.OpenReadStream())
                    using (var parser = new TextFieldParser(stream))
                    {
                        parser.TextFieldType = FieldType.Delimited;
                        parser.SetDelimiters(",");

                        while (!parser.EndOfData)
                        {
                            // Expected format is LastName in column 1, Firstname in column 2, Username in column 3, and Email in column 4
                            string[] fields = parser.ReadFields();

                            if (fields.Length >= 3)
                            {
                                var record = new MSTeamsStudentUpload
                                {
                                    Email = fields[3],
                                    Name = fields[1] + " " + fields[0],
                                    In221 = true
                                };
                                records.Add(record);
                            }
                        }
                    }

                    foreach (var record in records)
                    {
                        var studentExists = await _context.MSTeamsStudentUpload.FirstOrDefaultAsync(x => x.Email == record.Email);
                        if (studentExists == null)
                        {
                            if (record.Email != "Email" && record.Email[(record.Email.IndexOf('@') + 1)..] != "crimson.ua.edu")
                            {
                                await _context.MSTeamsStudentUpload.AddAsync(record);
                            }
                        }
                        else if (studentExists != null)
                        {
                            studentExists.In221 = true;
                            _context.MSTeamsStudentUpload.Update(studentExists);
                        }
                    }

                    await _context.SaveChangesAsync();

                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    return BadRequest($"Error processing CSV file: {ex.Message}");
                }
            }

            return BadRequest("Something went wrong.");
        }

        // GET: MSTeamsStudentUploads/Edit/5
        [Authorize(Roles = RolesConstants.AdminRole)]

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.MSTeamsStudentUpload == null)
            {
                return NotFound();
            }

            var mSTeamsStudentUpload = await _context.MSTeamsStudentUpload.FindAsync(id);
            if (mSTeamsStudentUpload == null)
            {
                return NotFound();
            }
            return View(mSTeamsStudentUpload);
        }

        // POST: MSTeamsStudentUploads/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesConstants.AdminRole)]

        public async Task<IActionResult> Edit(int id, [Bind("Id,MicrosoftId,Email,Name")] MSTeamsStudentUpload mSTeamsStudentUpload)
        {
            if (id != mSTeamsStudentUpload.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(mSTeamsStudentUpload);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MSTeamsStudentUploadExists(mSTeamsStudentUpload.Id))
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
            return View(mSTeamsStudentUpload);
        }

        // GET: MSTeamsStudentUploads/Delete/5
        [Authorize(Roles = RolesConstants.AdminRole)]

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.MSTeamsStudentUpload == null)
            {
                return NotFound();
            }

            var mSTeamsStudentUpload = await _context.MSTeamsStudentUpload
                .FirstOrDefaultAsync(m => m.Id == id);
            if (mSTeamsStudentUpload == null)
            {
                return NotFound();
            }

            return View(mSTeamsStudentUpload);
        }

        // POST: MSTeamsStudentUploads/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RolesConstants.AdminRole)]

        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.MSTeamsStudentUpload == null)
            {
                return Problem("Entity set 'MockInterviewDataDbContext.MSTeamsStudentUpload'  is null.");
            }
            var mSTeamsStudentUpload = await _context.MSTeamsStudentUpload.FindAsync(id);
            if (mSTeamsStudentUpload != null)
            {
                _context.MSTeamsStudentUpload.Remove(mSTeamsStudentUpload);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MSTeamsStudentUploadExists(int id)
        {
          return (_context.MSTeamsStudentUpload?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        public async Task<IActionResult> AttendanceReportAll()
        {

            return View("AttendanceReportAll");
        }
    }
}
