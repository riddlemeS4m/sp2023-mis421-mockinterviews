using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sp2023_mis421_mockinterviews.Data.Constants;
using sp2023_mis421_mockinterviews.Models.UserDb;
using sp2023_mis421_mockinterviews.Models.ViewModels;
using System.Data;

namespace sp2023_mis421_mockinterviews.Controllers
{
    [Authorize(Roles = RolesConstants.AdminRole)]
    public class UserRolesController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserRolesController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRolesViewModel = new List<UserRolesViewModel>();
            foreach (ApplicationUser user in users)
            {
                var thisViewModel = new UserRolesViewModel();
                thisViewModel.UserId = user.Id;
                thisViewModel.Email = user.Email;
                thisViewModel.FirstName = user.FirstName;
                thisViewModel.LastName = user.LastName;
                thisViewModel.Roles = await GetUserRoles(user);
                userRolesViewModel.Add(thisViewModel);
            }
            return View(userRolesViewModel);
        }
        private async Task<List<string>> GetUserRoles(ApplicationUser user)
        {
            return new List<string>(await _userManager.GetRolesAsync(user));
        }

        public async Task<IActionResult> Manage(string userId)
        {
            ViewBag.userId = userId;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"User with Id = {userId} cannot be found";
                return View("NotFound");
            }
            ViewBag.UserName = user.UserName;
            var model = new List<ManageUserRolesViewModel>();
            var roles = await _roleManager.Roles.ToListAsync();
            foreach (var role in roles)
            {
                var userRolesViewModel = new ManageUserRolesViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name
                };
                if (await _userManager.IsInRoleAsync(user, role.Name))
                {
                    userRolesViewModel.Selected = true;
                }
                else
                {
                    userRolesViewModel.Selected = false;
                }
                model.Add(userRolesViewModel);
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Manage(List<ManageUserRolesViewModel> model, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return View();
            }
            var roles = await _userManager.GetRolesAsync(user);
            var result = await _userManager.RemoveFromRolesAsync(user, roles);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Cannot remove user existing roles");
                return View(model);
            }
            result = await _userManager.AddToRolesAsync(user, model.Where(x => x.Selected).Select(y => y.RoleName));
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Cannot add selected roles to user");
                return View(model);
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> MassAssign()
        {
            var users = await _userManager.Users.OrderBy(x => x.FirstName).ToListAsync();
            var userRolesViewModel = new List<MassAssignRolesViewModel>();
            foreach (ApplicationUser user in users)
            {
                var alreadyinrole = await _userManager.IsInRoleAsync(user, RolesConstants.InterviewerRole);
                if(!alreadyinrole)
                {
                    var thisViewModel = new MassAssignRolesViewModel
                    {
                        Name = user.FirstName + " " + user.LastName,
                        Email = user.Email,
                        IsAlreadyInRole = alreadyinrole,
                        OriginalIsAlreadyInRole = alreadyinrole,
                        UpdatedIsAlreadyInRole = alreadyinrole
                    };
                    userRolesViewModel.Add(thisViewModel);
                }
            }
            return View("MassAssign",userRolesViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> MassAssignPost(string[] SelectedEventIds1)
        {
            //var count = 0;
            foreach (string x in SelectedEventIds1)
            {
                    //++;
                    //Console.WriteLine(x);
                    
                    await _userManager.AddToRoleAsync(await _userManager.FindByEmailAsync(x), RolesConstants.InterviewerRole);
            }

            return RedirectToAction("Index", "UserRoles");
        }

        public async Task<IActionResult> MassAssignAdmin()
        {
            var users = await _userManager.Users.OrderBy(x => x.FirstName).ToListAsync();
            var userRolesViewModel = new List<MassAssignRolesViewModel>();
            foreach (ApplicationUser user in users)
            {
                var alreadyinrole = await _userManager.IsInRoleAsync(user, RolesConstants.AdminRole);
                if (!alreadyinrole)
                {
                    var thisViewModel = new MassAssignRolesViewModel
                    {
                        Name = user.FirstName + " " + user.LastName,
                        Email = user.Email,
                        IsAlreadyInRole = alreadyinrole,
                        OriginalIsAlreadyInRole = alreadyinrole,
                        UpdatedIsAlreadyInRole = alreadyinrole
                    };
                    userRolesViewModel.Add(thisViewModel);
                }
            }
            return View("MassAssignAdmin", userRolesViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> MassAssignAdmin(string[] SelectedEventIds1)
        {
            //var count = 0;
            foreach (string x in SelectedEventIds1)
            {
                //++;
                //Console.WriteLine(x);

                await _userManager.AddToRoleAsync(await _userManager.FindByEmailAsync(x), RolesConstants.AdminRole);
            }

            return RedirectToAction("Index", "UserRoles");
        }
    }
}
