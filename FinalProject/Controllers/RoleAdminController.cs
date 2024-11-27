using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FinalProject.Models;
using FinalProject.DAL;

namespace FinalProject.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RoleAdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public RoleAdminController(AppDbContext context,
                                 UserManager<AppUser> userManager,
                                 RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: RoleAdmin/Index
        public async Task<IActionResult> Index()
        {
            List<AdminUserViewModel> users = new List<AdminUserViewModel>();
            foreach (AppUser user in await _userManager.Users.ToListAsync())
            {
                AdminUserViewModel model = new AdminUserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    Address = user.Address,
                    HireStatus = user.HireStatus ?? false,
                    Roles = (await _userManager.GetRolesAsync(user)).ToList()
                };
                users.Add(model);
            }
            return View(users);
        }

        // GET: RoleAdmin/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: RoleAdmin/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Verify age requirement
                var age = DateTime.Today.Year - model.Birthday.Year;
                if (model.Birthday.Date > DateTime.Today.AddYears(-age)) age--;

                if (age < 18)
                {
                    ModelState.AddModelError("Birthday", "Administrator must be at least 18 years old.");
                    return View(model);
                }

                var user = new AppUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address,
                    Birthday = model.Birthday,
                    HireStatus = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Add to Admin role
                    await _userManager.AddToRoleAsync(user, "Admin");
                    return RedirectToAction(nameof(Index));
                }

                AddErrorsFromResult(result);
            }

            return View(model);
        }

        // GET: RoleAdmin/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            AppUser user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new IndexViewModel
            {
                UserID = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                Birthday = user.Birthday,
                Email = user.Email,
                Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault()
            };

            return View(model);
        }

        // POST: RoleAdmin/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, IndexViewModel model)
        {
            if (id != model.UserID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                // Verify age requirement
                var age = DateTime.Today.Year - model.Birthday.Year;
                if (model.Birthday.Date > DateTime.Today.AddYears(-age)) age--;

                if (age < 18)
                {
                    ModelState.AddModelError("Birthday", "User must be at least 18 years old.");
                    return View(model);
                }

                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;
                user.Address = model.Address;
                user.Birthday = model.Birthday;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    return RedirectToAction(nameof(Index));
                }
                AddErrorsFromResult(result);
            }
            return View(model);
        }

        // POST: RoleAdmin/Deactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.HireStatus = false;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                AddErrorsFromResult(result);
                return RedirectToAction(nameof(Index));
            }

            // If user is an admin, remove role
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                await _userManager.RemoveFromRoleAsync(user, "Admin");
            }

            TempData["SuccessMessage"] = "User has been deactivated.";
            return RedirectToAction(nameof(Index));
        }

        // POST: RoleAdmin/Activate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.HireStatus = true;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                AddErrorsFromResult(result);
                return RedirectToAction(nameof(Index));
            }

            // If user was previously an admin, restore role
            if (string.Equals(user.Email.Split('@')[1], "bevobnb.com", StringComparison.OrdinalIgnoreCase))
            {
                await _userManager.AddToRoleAsync(user, "Admin");
            }

            TempData["SuccessMessage"] = "User has been activated.";
            return RedirectToAction(nameof(Index));
        }

        // POST: RoleAdmin/ResetPassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id, string newPassword)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(newPassword))
            {
                return BadRequest();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            string resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (!result.Succeeded)
            {
                AddErrorsFromResult(result);
            }

            return RedirectToAction(nameof(Index));
        }

        // Helper method to add Identity errors to ModelState
        private void AddErrorsFromResult(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }
    }
}