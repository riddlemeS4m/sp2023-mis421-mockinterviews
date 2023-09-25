using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using sp2023_mis421_mockinterviews.Data;
using sp2023_mis421_mockinterviews.Data.Constants;
using sp2023_mis421_mockinterviews.Models.MockInterviewDb;
using sp2023_mis421_mockinterviews.Models.UserDb;
using sp2023_mis421_mockinterviews.Models.ViewModels;

namespace sp2023_mis421_mockinterviews.Controllers
{
    [Authorize(Roles = RolesConstants.AdminRole)]
    public class LocationInterviewersController : Controller
    {
        private readonly MockInterviewDataDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LocationInterviewersController(MockInterviewDataDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: LocationInterviewers
        public async Task<IActionResult> Index()
        {
            var locationInterviewers = await _context.LocationInterviewer
                .Include(v => v.Location)
                .Include(v => v.EventDate)
                .Where(v => v.EventDate.IsActive == true)
                .ToListAsync();

            var interviewerIds = locationInterviewers
                .Select(v => v.InterviewerId)
                .Distinct()
                .ToList();

            var interviewers = await _userManager.Users
                .Where(u => interviewerIds.Contains(u.Id))
                .Select(x => new {x.FirstName, x.LastName, x.Id})
                .ToListAsync();            

            var query = from locationInterviewer in locationInterviewers
                        join interviewer in interviewers on locationInterviewer.InterviewerId equals interviewer.Id
                        select new LocationInterviewerWithName
                        {
                            LocationInterviewer = locationInterviewer,
                            InterviewerName = interviewer.FirstName + " " + interviewer.LastName,
                            InterviewerPreference = locationInterviewer.LocationPreference
                        };

            var locationInterviewersWithNames = query.ToList();
            var locations = await _context.Location
                .OrderBy(u => u.Room)
                .ToListAsync();

            var viewModel = new LocationInterviewerViewModel
            {
                Locations = locations,
                LocationInterviewerWithNames = locationInterviewersWithNames
            };
            return View(viewModel);
        }

        // GET: LocationInterviewers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.LocationInterviewer == null)
            {
                return NotFound();
            }

            var locationInterviewer = await _context.LocationInterviewer
                .Include(l => l.Location)
                .Include(l => l.EventDate)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (locationInterviewer == null)
            {
                return NotFound();
            }

            var interviewer = await _userManager.FindByIdAsync(locationInterviewer.InterviewerId);
            var locationInterviewerWithName = new LocationInterviewerWithName
            {
                LocationInterviewer = locationInterviewer,
                InterviewerName = interviewer.FirstName + " " + interviewer.LastName,
            };

            return View(locationInterviewerWithName);
        }

        // GET: LocationInterviewers/Create
        public async Task<IActionResult> Create()
        {
            // Get a list of all Interviewers
            var availableInterviewers = await _context.SignupInterviewerTimeslot
                .Include(i => i.SignupInterviewer)
                .Include(i => i.Timeslot)
                .ThenInclude(i => i.EventDate)
                .Where(i => i.Timeslot.EventDate.IsActive == true)
                .Select(i => new SelectListItem
                {
                    Value = i.SignupInterviewer.InterviewerId,
                    Text = $"{i.SignupInterviewer.FirstName} {i.SignupInterviewer.LastName}"
                })
                .Distinct()
                .ToListAsync();


            var availableLocations = await _context.Location
                .Select(i => new SelectListItem
                {
                    Value = i.Id.ToString(),
                    Text = $"{i.Room}"
                })
                .ToListAsync();

            var availableDates = await _context.EventDate
                .Where(i => i.IsActive)
                .Select(i => new SelectListItem
                {
                        Value = i.Id.ToString(),
                        Text = $"{i.Date:d}"
                })
                .ToListAsync();

            // Add message to dropdown list if no InterviewerIds or LocationIds are available
            if (availableInterviewers.Count == 0)
            {
                availableInterviewers.Add(new SelectListItem { Text = "No Interviewers available", Value = "" });
            }
            else
            {
                availableInterviewers = availableInterviewers.OrderBy(item => item.Text).ToList();
            }

            if (availableLocations.Count == 0)
            {
                availableLocations.Add(new SelectListItem { Text = "No Locations available", Value = "" });
            }
            else
            {
                availableLocations = availableLocations.OrderBy(item => item.Text).ToList();
            }

            // Create the view model
            var viewModel = new LocationInterviewerCreateViewModel
            {
                LocationInterviewer = new LocationInterviewer(),
                InterviewerNames = availableInterviewers,
                Dates = availableDates,
                Locations = availableLocations
            };


            return View(viewModel);
        }

        // POST: LocationInterviewers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,InterviewerId,LocationId,EventDateId")] LocationInterviewer locationInterviewer, bool InPerson)
        {
            if(locationInterviewer == null)
            {
                return NotFound();
            }
            
            if(InPerson)
            {
                locationInterviewer.LocationPreference = InterviewLocationConstants.InPerson;
            }
            else
            {
                locationInterviewer.LocationPreference = InterviewLocationConstants.IsVirtual;
            }

            if (ModelState.IsValid)
            {
                _context.Add(locationInterviewer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Get a list of all Interviewers
            var interviewers = await _userManager.GetUsersInRoleAsync(RolesConstants.InterviewerRole);

            //// Get a list of InterviewerIds already assigned to LocationInterviewers
            //var assignedInterviewerIds = _context.LocationInterviewer.Select(li => li.InterviewerId)
            //                                                            .Distinct()
            //                                                            .ToList();

            //// Filter out assigned Interviewers from the list of all Interviewers
            //var availableInterviewers = interviewers.Where(i => !assignedInterviewerIds.Contains(i.Id))
            //                                            .Select(i => new SelectListItem
            //                                            {
            //                                                Value = i.Id,
            //                                                Text = $"{i.FirstName} {i.LastName}"
            //                                            })
            //                                            .ToList();

            var availableInterviewers = interviewers
                .Select(i => new SelectListItem
                {
                    Value = i.Id,
                    Text = $"{i.FirstName} {i.LastName}"
                })
                .ToList();

            // Get a list of LocationIds already assigned to LocationInterviewers
            //var assignedLocationIds = _context.LocationInterviewer.Select(li => li.LocationId)
            //                                                        .Distinct()
            //                                                        .ToList();

            // Get a list of all Locations except those already assigned to LocationInterviewers
            //var availableLocations = _context.Location.Where(l => !assignedLocationIds.Contains(l.Id))
            //                                            .Select(i => new SelectListItem
            //                                            {
            //                                                Value = i.Id.ToString(),
            //                                                Text = $"{i.Room}"
            //                                            })
            //                                            .ToList();

            var availableLocations = _context.Location
                    .Select(i => new SelectListItem
                    {
                        Value = i.Id.ToString(),
                        Text = $"{i.Room}"
                    })
                    .ToList();

            var availableDates = _context.EventDate
                .Select(i => new SelectListItem
                {
                    Value = i.Id.ToString(),
                    Text = $"{i.Date:d}"
                })
                    .ToList();

            // Add message to dropdown list if no InterviewerIds or LocationIds are available
            if (availableInterviewers.Count == 0)
            {
                availableInterviewers.Add(new SelectListItem { Text = "No Interviewers available", Value = "" });
            }
            else
            {
                availableInterviewers = availableInterviewers.OrderBy(item => item.Text).ToList();
            }

            if (availableLocations.Count == 0)
            {
                availableLocations.Add(new SelectListItem { Text = "No Locations available", Value = "" });
            }

            // Create the view model
            var viewModel = new LocationInterviewerCreateViewModel
            {
                LocationInterviewer = new LocationInterviewer(),
                InterviewerNames = availableInterviewers,
                Dates = availableDates,
                Locations = availableLocations
            };


            return View(viewModel);
        }

        // GET: LocationInterviewers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.LocationInterviewer == null)
            {
                return NotFound();
            }

            var locationInterviewer = await _context.LocationInterviewer.FindAsync(id);
            if (locationInterviewer == null)
            {
                return NotFound();
            }

            ////get list of all timeslot ids for the interviewer needing a room
            //var timeslotIds = await _context.SignupInterviewerTimeslot
            //    .Include(x => x.SignupInterviewer)
            //    .Include(x => x.Timeslot)
            //    .ThenInclude(x => x.EventDate)
            //    .Where(x => x.SignupInterviewer.InterviewerId == locationInterviewer.InterviewerId && x.Timeslot.EventDateId == locationInterviewer.EventDateId)
            //    .Select(x => x.TimeslotId)
            //    .Distinct()
            //    .ToListAsync();

            //var allInterviewers = await _context.LocationInterviewer
            //    .Select(x => x.InterviewerId)
            //    .Distinct()
            //    .ToListAsync();

            //foreach(var y in allInterviewers) 
            //{
            //    var timeslots = await _context.SignupInterviewerTimeslot
            //        .Include(x => x.SignupInterviewer)
            //        .Include(x => x.Timeslot)
            //        .ThenInclude(x => x.EventDate)
            //        .Where(x => x.SignupInterviewer.InterviewerId == y)
            //        .Select(x => x.TimeslotId)
            //        .Distinct()
            //        .ToListAsync();
            //}

            // Get a list of LocationIds already assigned to LocationInterviewers for today
            var assignedLocationIds = await _context.LocationInterviewer
                .Where(li => li.EventDateId == locationInterviewer.EventDateId)
                .Select(li => li.LocationId)
                .Distinct()
                .ToListAsync();

            // Get a list of all Locations except those already assigned to LocationInterviewers
            var inPerson = true;
            var isVirtual = false;
            if (locationInterviewer.LocationPreference == InterviewLocationConstants.IsVirtual)
            {
                inPerson = false;
                isVirtual = true;
            }
           

            var availableLocations = await _context.Location
                .Where(l => (!assignedLocationIds.Contains(l.Id) || l.Id == locationInterviewer.LocationId) && (l.InPerson == inPerson && l.IsVirtual == isVirtual))
                .Select(i => new SelectListItem
                {
                    Value = i.Id.ToString(),
                    Text = $"{i.Room}"
                })
                .ToListAsync();

            availableLocations = availableLocations.OrderBy(item => item.Text).ToList();

            //// Add message to dropdown list if no InterviewerIds or LocationIds are available
            //if (availableInterviewers.Count == 0)
            //{
            //    availableInterviewers.Add(new SelectListItem { Text = "No Interviewers available", Value = "" });
            //}

            if (availableLocations.Count == 0)
            {
                availableLocations.Add(new SelectListItem { Text = "No Locations available", Value = "" });
            }
            else
            {
                availableLocations.Insert(0, new SelectListItem { Text = "Unassigned", Value = "" });
            }

            var interviewer = await _userManager.FindByIdAsync(locationInterviewer.InterviewerId);

            // Create the view model
            var viewModel = new LocationInterviewerCreateViewModel
            {
                LocationInterviewer = locationInterviewer,
                InterviewerName = interviewer.FirstName + " " + interviewer.LastName,
                Locations = availableLocations
            };

            return View(viewModel);
        }

        // POST: LocationInterviewers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,InterviewerId,LocationId,EventDateId,LocationPreference")] LocationInterviewer locationInterviewer)
        {
            if (id != locationInterviewer.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(locationInterviewer);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LocationInterviewerExists(locationInterviewer.Id))
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


            // Create the view model
            var viewModel = new LocationInterviewerCreateViewModel
            {
                LocationInterviewer = locationInterviewer
            };

            return View(viewModel);
        }

        // GET: LocationInterviewers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.LocationInterviewer == null)
            {
                return NotFound();
            }

            var locationInterviewer = await _context.LocationInterviewer
                .Include(l => l.Location)
                .Include(e => e.EventDate)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (locationInterviewer == null)
            {
                return NotFound();
            }

            var interviewer = await _userManager.FindByIdAsync(locationInterviewer.InterviewerId);
            var locationInterviewerWithName = new LocationInterviewerWithName
            {
                LocationInterviewer = locationInterviewer,
                InterviewerName = interviewer.FirstName + " " + interviewer.LastName,
            };

            return View(locationInterviewerWithName);
        }

        // POST: LocationInterviewers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.LocationInterviewer == null)
            {
                return Problem("Entity set 'MockInterviewDataDbContext.LocationInterviewer'  is null.");
            }
            var locationInterviewer = await _context.LocationInterviewer.FindAsync(id);
            if (locationInterviewer != null)
            {
                _context.LocationInterviewer.Remove(locationInterviewer);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LocationInterviewerExists(int id)
        {
          return (_context.LocationInterviewer?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
