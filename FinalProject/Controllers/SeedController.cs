using Microsoft.AspNetCore.Identity;  // Required for UserManager and RoleManager
using FinalProject.DAL;               // Your data access layer (AppDbContext)
using FinalProject.Models;            // Your models (AppUser, etc.)
using FinalProject.Seeding;           // For seeding methods (SeedRoles, SeedUsers)
using Microsoft.AspNetCore.Mvc;       // For Controller and IActionResult
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FinalProject.Controllers
{
    public class SeedController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public SeedController(AppDbContext db, UserManager<AppUser> um, RoleManager<IdentityRole> rm)
        {
            _context = db;
            _userManager = um;
            _roleManager = rm;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> SeedRoles() // Updated name to avoid conflict
        {
            try
            {
                await Seeding.SeedRoles.AddAllRoles(_roleManager);
            }
            catch (Exception ex)
            {
                List<string> errorList = new List<string>
                {
                    ex.Message
                };

                if (ex.InnerException != null)
                {
                    errorList.Add(ex.InnerException.Message);

                    if (ex.InnerException.InnerException != null)
                    {
                        errorList.Add(ex.InnerException.InnerException.Message);
                    }
                }

                return View("Error", errorList);
            }

            return View("Confirm");
        }

        public async Task<IActionResult> SeedPeople()
        {
            try
            {
                // Call the SeedUsers method to seed users
                await SeedUsers.SeedAllUsers(_userManager, _context);
            }
            catch (Exception ex)
            {
                List<string> errorList = new List<string>
                {
                    ex.Message
                };

                if (ex.InnerException != null)
                {
                    errorList.Add(ex.InnerException.Message);

                    if (ex.InnerException.InnerException != null)
                    {
                        errorList.Add(ex.InnerException.InnerException.Message);
                    }
                }

                return View("Error", errorList);
            }

            return View("Confirm");
        }
    }
}
