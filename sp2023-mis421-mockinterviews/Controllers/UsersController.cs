using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using SendGrid.Helpers.Mail;
using sp2023_mis421_mockinterviews.Areas.Identity.Pages.Account.Manage;
using sp2023_mis421_mockinterviews.Data.Constants;
using sp2023_mis421_mockinterviews.Models.UserDb;
using sp2023_mis421_mockinterviews.Models.ViewModels;
using System.Net.Mime;

namespace sp2023_mis421_mockinterviews.Controllers
{
    public class CreateUserModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
    }
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public UsersController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            return View();
        }

        //[HttpGet]
        [Authorize(Roles = RolesConstants.InterviewerRole)]
        public async Task<IActionResult> ExternalUserProfileView(string userId)
        {
            // Retrieve the current user's information from the database
            var user = await _userManager.FindByIdAsync(userId);

            // Create a view model with the user's data
            var viewModel = new ExternalUserProfileViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Class = ClassConstants.GetClassText((Classes)user.Class)
            };

            // Return the ProfileView with the view model
            return View(viewModel);
        }

        //[HttpGet]
        //[Route("Users/DownloadResume/{userId}")]
        [Authorize(Roles = RolesConstants.InterviewerRole + "," + RolesConstants.AdminRole + "," + RolesConstants.StudentRole)]
        public async Task<IActionResult> DownloadResume(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var resume = user.Resume;
            if (resume == null)
            {
                return NotFound();
            }

            var contentDisposition = new ContentDisposition
            {
                FileName = $"{user.FirstName}_{user.LastName}_Resume.docx",
                Inline = false
            };

            Response.Headers.Add("Content-Disposition", contentDisposition.ToString());
            return File(resume, "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        }

        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            if (userId == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            return View("DeleteUser",user);
        }

        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> DeleteUserConfirmed(string Id)
        {
            var user = await _userManager.FindByIdAsync(Id);
            if (user == null)
            {
                return Problem("User not found.");
            }
            else
            {
                await _userManager.DeleteAsync(user);
            }

            return RedirectToAction("Index", "UserRoles");
        }

        [HttpGet]
        [Authorize(Roles = RolesConstants.AdminRole)]
        public IActionResult CreateProvisionaryUser()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> CreateProvisionaryUser(CreateUserModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { FirstName = model.FirstName, LastName = model.LastName, Email = model.Email, UserName = model.Email };
                var result = await _userManager.CreateAsync(user, $"{model.FirstName}Spring2024!");

                if (result.Succeeded)
                {
                    // User successfully created
                    // Redirect or return appropriate response

                    var newUser = await _userManager.FindByEmailAsync(model.Email) ?? throw new Exception($"User with email {model.Email} was not successfully created.");
                    var roleResult = await _userManager.AddToRoleAsync(newUser, RolesConstants.InterviewerRole);

                    if(roleResult.Succeeded)
                    {
                        return RedirectToAction("Index", "UserRoles");
                    }
                    else
                    {
                        // Handle errors in creating the user
                        // Add model errors if needed
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                    }
                }
                else
                {
                    // Handle errors in creating the user
                    // Add model errors if needed
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }

            // If model state is not valid or user creation fails, return to the creation page with errors
            return View(model);
        }
    }
}
