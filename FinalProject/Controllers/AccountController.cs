using FinalProject.DAL;
using FinalProject.Models;
using FinalProject.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FinalProject.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [AllowAnonymous]
        public IActionResult Register()
        {
            // Check if user is already logged in
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel rvm)
        {
            if (!ModelState.IsValid)
            {
                return View(rvm);
            }

            // Check if user is at least 18 years old
            var age = DateTime.Today.Year - rvm.Birthday.Year;
            if (rvm.Birthday > DateTime.Today.AddYears(-age)) age--;
            if (age < 18)
            {
                ModelState.AddModelError("", "You must be at least 18 years old to register.");
                return View(rvm);
            }

            // Check if email is already in use
            var existingUser = await _userManager.FindByEmailAsync(rvm.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("", "Email is already registered.");
                return View(rvm);
            }

            AppUser newUser = new AppUser
            {
                UserName = rvm.Email,
                Email = rvm.Email,
                PhoneNumber = rvm.PhoneNumber,
                FirstName = rvm.FirstName,
                LastName = rvm.LastName,
                Address = rvm.Address,
                Birthday = rvm.Birthday
            };

            AddUserModel aum = new AddUserModel()
            {
                User = newUser,
                Password = rvm.Password,
                RoleName = rvm.Role
            };

            IdentityResult result = await AddUser.AddUserWithRoleAsync(aum, _userManager, _context);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(newUser, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(rvm);
        }

        [AllowAnonymous]
        public IActionResult Login(string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // Remove the IsActive check in Login method:
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel lvm, string? returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(lvm);
            }

            var result = await _signInManager.PasswordSignInAsync(lvm.Email, lvm.Password, lvm.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(lvm.Email);
                if (await _userManager.IsInRoleAsync(user, "Host"))
                {
                    return RedirectToAction("HostDashboard"); // Redirect to Host Dashboard
                }
                return Redirect(returnUrl ?? "/");
            }

            ModelState.AddModelError("", "Invalid login attempt.");
            return View(lvm);
        }

        public async Task<IActionResult> HostDashboard()
        {
            // Assuming `User.Identity.Name` holds the current user's identifier (email, username, etc.)
            var user = _context.Users
                .Include(u => u.Properties)    // Eagerly load Properties
                .Include(u => u.Reservations)  // Eagerly load Reservations
                .Include(u => u.Reviews)       // Eagerly load Reviews
                .FirstOrDefault(u => u.Email == User.Identity.Name); // Adjust if not using Email

            if (user == null)
            {
                return NotFound("User not found");
            }

            return View(user);
        }

        [AllowAnonymous]
        public async Task<IActionResult> HostInfo()
        {
            return View();
        }

        // GET: Reviews/Dispute
        [HttpGet]
        [Authorize(Roles = "Host")]
        public async Task<IActionResult> Reports()
        {
            // Get the current user's ID
            string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            // Retrieve all properties owned by the host
            var properties = await _context.Properties
                .Where(p => p.Host.Id == userId)
                .ToListAsync();

            return View(properties);
        }




        public async Task<IActionResult> Index()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var roles = await _userManager.GetRolesAsync(user);

            IndexViewModel ivm = new IndexViewModel
            {
                Email = user.Email,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                Birthday = user.Birthday,
                Role = roles.FirstOrDefault(),
                UserID = user.Id,
                HasPassword = true
            };

            return View(ivm);
        }

        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel cpvm)
        {
            if (!ModelState.IsValid)
            {
                return View(cpvm);
            }

            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var result = await _userManager.ChangePasswordAsync(user, cpvm.OldPassword, cpvm.NewPassword);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(cpvm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOff()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied(string ReturnURL)
        {
            return View("Error", new string[] { "Access is denied" });
        }
    }
}