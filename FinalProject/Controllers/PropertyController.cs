using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FinalProject.Models;
using FinalProject.DAL;

namespace FinalProject.Controllers
{
    public class PropertyController : Controller
    {
        private readonly AppDbContext _context;

        public PropertyController(AppDbContext context)
        {
            _context = context;
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

        // GET: /Property/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var property = await _context.Properties
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.Customer)
                .Include(p => p.Host)
                .FirstOrDefaultAsync(p => p.PropertyID == id);

            if (property == null)
            {
                return NotFound();
            }

            return View(property);
        }

        // GET: /Property/Search
        public async Task<IActionResult> Search(
            string location = null,
            DateTime? checkIn = null,
            DateTime? checkOut = null,
            int? guests = null,
            int? categoryId = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int? minBedrooms = null,
            int? minBathrooms = null,
            decimal? minRating = null,
            bool? petsAllowed = null,
            bool? freeParking = null)
        {
            var query = _context.Properties
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .Include(p => p.Host)
                .Where(p => p.PropertyStatus);



            // Apply search filters function
            if (!string.IsNullOrEmpty(location))
            {
                var searchTerms = location.ToLower()
                    .Split(',')
                    .SelectMany(term => term.Trim().Split(' '))
                    .Where(term => !string.IsNullOrWhiteSpace(term))
                    .ToArray();

                // If single search term, use original logic
                if (searchTerms.Length == 1)
                {
                    var singleTerm = searchTerms[0];
                    query = query.Where(p =>
                        p.City.ToLower().Contains(singleTerm) ||
                        p.State.ToLower().Contains(singleTerm) ||
                        p.Street.ToLower().Contains(singleTerm) ||
                        p.Zip.Contains(singleTerm)
                    );
                }
                // If multiple terms, check each part separately
                else
                {
                    query = query.Where(p =>
                        searchTerms.All(term =>
                            p.Street.ToLower().Contains(term) ||
                            p.City.ToLower().Contains(term) ||
                            p.State.ToLower().Contains(term) ||
                            p.Zip.Contains(term)
                        )
                    );
                }
            }

            if (guests.HasValue)
            {
                query = query.Where(p => p.GuestsAllowed >= guests.Value);
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.Category.CategoryID == categoryId.Value);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.WeekdayPrice >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.WeekdayPrice <= maxPrice.Value);
            }

            if (minBedrooms.HasValue)
            {
                query = query.Where(p => p.Bedrooms >= minBedrooms.Value);
            }

            if (minBathrooms.HasValue)
            {
                query = query.Where(p => p.Bathrooms >= minBathrooms.Value);
            }

            if (petsAllowed.HasValue)
            {
                query = query.Where(p => p.PetsAllowed == petsAllowed.Value);
            }

            if (freeParking.HasValue)
            {
                query = query.Where(p => p.FreeParking == freeParking.Value);
            }

            // Check availability if dates are provided
            if (checkIn.HasValue && checkOut.HasValue)
            {
                query = query.Where(p => !p.Reservations.Any(r =>
                    r.ReservationStatus && // Only consider active reservations
                    ((checkIn >= r.CheckIn && checkIn < r.CheckOut) || // Check-in during existing reservation
                     (checkOut > r.CheckIn && checkOut <= r.CheckOut) || // Check-out during existing reservation
                     (checkIn <= r.CheckIn && checkOut >= r.CheckOut)))); // Existing reservation within requested dates
            }

            // Apply rating filter if specified
            if (minRating.HasValue)
            {
                query = query.Where(p =>
                    p.Reviews.Any() &&
                    p.Reviews.Where(r => !r.DisputeStatus)
                             .Average(r => (decimal)r.Rating) >= minRating.Value);
            }

            var properties = await query.ToListAsync();



            // Populate ViewBag data for the view
            ViewBag.TotalCount = await _context.Properties.CountAsync();
            ViewBag.FilteredCount = properties.Count;
            ViewBag.Categories = await _context.Categories.ToListAsync();

            // Return the search view with results
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
                // Check if category already exists
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

        // POST: Property/EditCategory/5
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

            // Check if category name already exists (excluding current category)
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
    }
}