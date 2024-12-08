using FinalProject.DAL;
using FinalProject.Models;
using FinalProject.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FinalProject.Controllers
{
    public class PropertyController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public PropertyController(AppDbContext context, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }


        // GET: /Property/
        public async Task<IActionResult> Index()
        {
            var properties = await _context.Properties
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .Include(p => p.Host)
                .Where(p => p.PropertyStatus)
                .ToListAsync();


            ViewBag.TotalCount = await _context.Properties.CountAsync();
            ViewBag.Categories = await _context.Categories.ToListAsync();

            return View(properties);
        }


        // GET: Property/ManageProperties  
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ManageProperties()
        {
            var properties = await _context.Properties
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .Include(p => p.Host)
                .ToListAsync();

            ViewBag.TotalProperties = properties.Count;
            ViewBag.ActiveProperties = properties.Count(p => p.PropertyStatus);
            ViewBag.PendingProperties = properties.Count(p => !p.PropertyStatus);

            return View(properties);
        }

        // GET: Property/PendingApprovals
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PendingApprovals()
        {
            var pendingProperties = await _context.Properties
                .Include(p => p.Host)
                .Include(p => p.Category)
                .Where(p => !p.PropertyStatus)
                .ToListAsync();

            return View(pendingProperties);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveProperty(int id)
        {
            var property = await _context.Properties.FindAsync(id);

            if (property == null)
            {
                return NotFound();
            }

            property.PropertyStatus = true; // Mark as approved
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Property has been approved successfully.";
            return RedirectToAction(nameof(PendingApprovals));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectProperty(int id)
        {
            var property = await _context.Properties.FindAsync(id);

            if (property == null)
            {
                return NotFound();
            }

            // We don't delete the property, just mark it as inactive
            property.PropertyStatus = false;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Property has been rejected successfully.";
            return RedirectToAction(nameof(PendingApprovals));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePropertyStatus(int id)
        {
            var property = await _context.Properties.FindAsync(id);

            if (property == null)
            {
                return NotFound();
            }

            // Toggle the status
            property.PropertyStatus = !property.PropertyStatus;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Property has been {(property.PropertyStatus ? "activated" : "deactivated")} successfully.";
            return RedirectToAction(nameof(ManageProperties));
        }


        [HttpGet]
        [Authorize(Roles = "Host")]
        public async Task<IActionResult> EditProperty(int id)
        {
            var property = await _context.Properties.FindAsync(id);

            if (property == null)
            {
                return NotFound();
            }

            var model = new UpdatePropertyViewModel
            {
                PropertyID = property.PropertyID,
                WeekdayPrice = property.WeekdayPrice,
                WeekendPrice = property.WeekendPrice,
                CleaningFee = property.CleaningFee,
                MinNights = property.MinNightsForDiscount,
                Discount = property.DiscountRate
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Host")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProperty(UpdatePropertyViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var property = await _context.Properties.FindAsync(model.PropertyID);

            if (property == null)
            {
                return NotFound();
            }

            // Validatio!!
            if (model.Discount.HasValue && (!model.MinNights.HasValue || model.MinNights <= 0))
            {
                ModelState.AddModelError("MinNights", "Minimum nights must be set before a discount can be applied.");
                return View(model);
            }

            property.WeekdayPrice = model.WeekdayPrice;
            property.WeekendPrice = model.WeekendPrice;
            property.CleaningFee = model.CleaningFee;
            property.MinNightsForDiscount = model.MinNights;
            property.DiscountRate = model.Discount;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Pricing updated successfully!";
            return RedirectToAction("Reports", "Account");
        }

        // GET: Property/Categories
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Categories
                .Include(c => c.Properties)
                .ToListAsync();
            return View(categories);
        }

        

        // POST: Property/CreateCategory
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory([Bind("CategoryName")] Category category)
        {
            if (ModelState.IsValid)
            {
                
                if (await _context.Categories.AnyAsync(c =>
                    c.CategoryName.ToLower() == category.CategoryName.ToLower()))
                {
                    ModelState.AddModelError("CategoryName", "This category already exists.");
                    return RedirectToAction(nameof(Categories));
                }

                _context.Add(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Category created successfully.";
            }
            return RedirectToAction(nameof(Categories));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, string categoryName)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            if (await _context.Categories.AnyAsync(c =>
                c.CategoryName.ToLower() == categoryName.ToLower() &&
                c.CategoryID != id))
            {
                TempData["ErrorMessage"] = "This category name already exists.";
                return RedirectToAction(nameof(Categories));
            }

            category.CategoryName = categoryName;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Category updated successfully.";
            return RedirectToAction(nameof(Categories));
        }

        // GET: Property/Create
        [Authorize(Roles = "Host")]
        public IActionResult Create()
        {

            
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }
            
            var currentUser = _userManager.GetUserAsync(User).Result;
            if (currentUser == null)
            {
                
                return RedirectToAction("Login", "Account"); 
            }
            if (currentUser != null)
            {
                ViewBag.Host = currentUser;  
                
            }

            ViewBag.CategorySelectList = new SelectList(_context.Categories, "CategoryID", "CategoryName");

            return View();
        }

        [Authorize(Roles = "Host")]
        // POST: Property/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Property property)
        {
            
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }
            if (ModelState.IsValid)
            {
                
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Unauthorized();

                property.Host = currentUser;  


                property.Category = _context.Categories.Find(property.CategoryID);

                
                int lastpropNumber = await _context.Properties
                .MaxAsync(r => (int?)r.PropertyNumber) ?? 9999;
                property.PropertyNumber = lastpropNumber + 1;

                
                _context.Properties.Add(property);
                _context.SaveChanges();
                return RedirectToAction("HostDashboard", "Account");
            }

            
            ViewBag.CategorySelectList = new SelectList(_context.Categories, "CategoryID", "CategoryName");

            return BadRequest(ModelState); 
        }




        private async Task<AppUser> GetCurrentUser()
        {
            return await _userManager.GetUserAsync(HttpContext.User);
        }

        [Authorize(Roles = "Host")]
        [HttpPost]
        public IActionResult ToggleStatus(int propertyId)
        {
            var property = _context.Properties.FirstOrDefault(p => p.PropertyID == propertyId);
            if (property != null)
            {
                property.IsActive = !property.IsActive; // Toggle IsActive instead of PropertyStatus
                _context.SaveChanges();
            }
            return RedirectToAction("Reports", "Account");
        }


    }
}
