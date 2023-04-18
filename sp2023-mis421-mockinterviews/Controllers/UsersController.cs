using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using sp2023_mis421_mockinterviews.Areas.Identity.Pages.Account.Manage;
using sp2023_mis421_mockinterviews.Data;
using sp2023_mis421_mockinterviews.Models.UserDb;
using sp2023_mis421_mockinterviews.Models.ViewModels;
using System.Net.Mime;

namespace sp2023_mis421_mockinterviews.Controllers
{
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public UsersController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }
        [Authorize(Roles =RolesConstants.AdminRole)]
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            return View();
        }

        //public async Task<IActionResult> Details(string id)
        //{
        //    var user = await _userManager.FindByIdAsync(id);
        //    if (user == null)
        //    {
        //        ViewBag.ErrorMessage = $"User with Id = {id} cannot be found";
        //        return View("NotFound");
        //    }
        //    var model = new UserDetailsViewModel
        //    {
        //        Id = user.Id,
        //        Email = user.Email,
        //        FirstName = user.FirstName,
        //        LastName = user.LastName,
        //        ProfilePicture = user.ProfilePicture,
        //        Resume = user.Resume
        //    };
        //    return View(model);
        //}

        //public async Task<IActionResult> Edit(string id)
        //{
        //    var user = await _userManager.FindByIdAsync(id);
        //    if (user == null)
        //    {
        //        ViewBag.ErrorMessage = $"User with Id = {id} cannot be found";
        //        return View("NotFound");
        //    }
        //    var model = new EditUserViewModel
        //    {
        //        Id = user.Id,
        //        Email = user.Email,
        //        FirstName = user.FirstName,
        //        LastName = user.LastName,
        //        ProfilePicture = user.ProfilePicture,
        //        Resume = user.Resume
        //    };
        //    return View(model);
        //}

        //[HttpPost]
        //public async Task<IActionResult> Edit(EditUserViewModel model)
        //{
        //    var user = await _userManager.FindByIdAsync(model.Id);
        //    if (user == null)
        //    {
        //        ViewBag.ErrorMessage = $"User with Id = {model.Id} cannot be found";
        //        return View("NotFound");
        //    }
        //    else
        //    {
        //        user.FirstName = model.FirstName;
        //        user.LastName = model.LastName;
        //        user.Email = model.Email;
        //        user.ProfilePicture = model.ProfilePicture;
        //        user.Resume = model.Resume;
        //        var result = await _userManager.UpdateAsync(user);
        //        if (result.Succeeded)
        //        {
        //            return RedirectToAction("Index");
        //        }
        //        foreach (var error in result.Errors)
        //        {
        //            ModelState.AddModelError("", error.Description);
        //        }
        //        return View(model);
        //    }
        //}   

        //public async Task<IActionResult> Delete(string id)
        //{
        //    var user = await _userManager.FindByIdAsync(id);
        //    if (user == null)
        //    {
        //        ViewBag.ErrorMessage = $"User with Id = {id} cannot be found";
        //        return View("NotFound");
        //    }
        //    else
        //    {
        //        var result = await _userManager.DeleteAsync(user);
        //        if (result.Succeeded)
        //        {
        //            return RedirectToAction("Index");
        //        }
        //        foreach (var error in result.Errors)
        //        {
        //            ModelState.AddModelError("", error.Description);
        //        }
        //        return View("Index");
        //    }
        //}   

        //public IActionResult AccessDenied()
        //{
        //    return View();
        //}

        //public IActionResult NotFound()
        //{
        //    return View();
        //}

        //public IActionResult Error()
        //{
        //    return View();
        //}

        //[HttpGet]
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
                Class = user.Class
            };

            // Return the ProfileView with the view model
            return View(viewModel);
        }

        //[HttpGet]
        //[Route("Users/DownloadResume/{userId}")]
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
    }
}
