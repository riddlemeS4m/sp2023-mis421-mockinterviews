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
using sp2023_mis421_mockinterviews.Services.GoogleDrive;
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
        private readonly GoogleDriveResumeService _driveResumeService;
        private readonly GoogleDrivePfpService _drivePfpService;
        public UsersController(UserManager<ApplicationUser> userManager,
            GoogleDriveResumeService driveResumeService,
            GoogleDrivePfpService drivePfpService)
        {
            _userManager = userManager;
            _driveResumeService = driveResumeService;
            _drivePfpService = drivePfpService;
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
            var user = await _userManager.FindByIdAsync(userId);

            var viewModel = new ExternalUserProfileViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Class = ClassConstants.GetClassText((Classes)user.Class)
            };

            return View(viewModel);
        }

        //[HttpGet]
        //[Route("Users/DownloadResume/{userId}")]
        [AllowAnonymous]
        public async Task<IActionResult> DownloadResume(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var resume = user.Resume;
            if (string.IsNullOrEmpty(resume))
            {
                return NotFound();
            }

            var contentDisposition = new ContentDisposition
            {
                FileName = $"{user.FirstName}_{user.LastName}_Resume.docx",
                Inline = false
            };

            Response.Headers.Add("Content-Disposition", contentDisposition.ToString());

            (Google.Apis.Drive.v3.Data.File file, MemoryStream stream) = await _driveResumeService.GetOneFile(resume, true);
            
            stream.Position = 0;

            return File(stream.ToArray(), file.MimeType);
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
                var user = new ApplicationUser 
                { 
                    FirstName = model.FirstName, 
                    LastName = model.LastName, 
                    Email = model.Email, 
                    UserName = model.Email 
                };
                var result = await _userManager.CreateAsync(user, $"{model.FirstName}Spring2024!");

                if (result.Succeeded)
                {
                    var newUser = await _userManager.FindByEmailAsync(model.Email) ?? throw new Exception($"User with email {model.Email} was not successfully created.");
                    var roleResult = await _userManager.AddToRoleAsync(newUser, RolesConstants.InterviewerRole);

                    if(roleResult.Succeeded)
                    {
                        return RedirectToAction("Index", "UserRoles");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                    }
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }

            // If model state is not valid or user creation fails, return to the creation page with errors
            return View(model);
        }

        [HttpGet]
        [Route("Image/Proxy/{fileId}")]
        public async Task<IActionResult> ProxyImage(string fileId)
        {
            try
            {
                (Google.Apis.Drive.v3.Data.File file, MemoryStream stream) = await _drivePfpService.GetOneFile(fileId, true);
                stream.Position = 0;

                // Set HTTP cache headers
                Response.Headers["Cache-Control"] = "public,max-age=3600"; // Adjust the cache duration as needed

                return File(stream, file.MimeType);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet]
        [Authorize(Roles = RolesConstants.AdminRole)]
        public async Task<IActionResult> ResetUserPassword(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"User with ID {userId} not found");
            }

            var model = new ResetPasswordViewModel { UserId = userId };
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = RolesConstants.AdminRole)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetUserPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                ViewBag.ErrorMessage = "User not found.";
                return View(model);
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "UserRoles");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(model);
        }
    }
}
