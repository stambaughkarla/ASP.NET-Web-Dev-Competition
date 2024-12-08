using Microsoft.AspNetCore.Mvc;
using FinalProject.Models;
using FinalProject.DAL;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace FinalProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public HomeController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Home/
        public async Task<IActionResult> Index()
        {
            // Get all active and approved properties with their related data
            var properties = await _context.Properties
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .Include(p => p.Host)
                .Where(p => p.PropertyStatus && p.IsActive)
                .ToListAsync();

            // Get total count of only approved and active properties
            ViewBag.TotalCount = await _context.Properties
                .CountAsync(p => p.PropertyStatus && p.IsActive);

            ViewBag.FilteredCount = properties.Count;

            // Get all categories for the search filters
            ViewBag.Categories = await _context.Categories.ToListAsync();

            return View(properties);
        }

        // GET: /Home/
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var property = await _context.Properties
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.Customer)
                .Include(p => p.Host)
                .FirstOrDefaultAsync(p => p.PropertyID == id);

            if (property == null) return NotFound();

            return View("~/Views/Property/Details.cshtml", property);
        }

        // GET: /Home/
        public async Task<IActionResult> Search(
            string location = null,
            DateTime? checkIn = null,
            DateTime? checkOut = null,
            int? guests = null,
            int? categoryId = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int? minBedrooms = null,
            int? maxBedrooms = null,
            int? minBathrooms = null,
            decimal? minRating = null,
            bool? petsAllowed = null,
            bool? freeParking = null)
        {
            var query = _context.Properties
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .Include(p => p.Host)
                .Where(p => p.PropertyStatus && p.IsActive); // Only show active and approved properties

            // Apply filters one by one
            if (!string.IsNullOrEmpty(location))
            {
                query = ApplyLocationFilter(query, location);
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryID == categoryId.Value);
            }

            if (guests.HasValue)
            {
                query = query.Where(p => p.GuestsAllowed >= guests.Value);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.WeekdayPrice >= minPrice.Value || p.WeekendPrice >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.WeekdayPrice <= maxPrice.Value && p.WeekendPrice <= maxPrice.Value);
            }

            if (minBedrooms.HasValue)
            {
                query = query.Where(p => p.Bedrooms >= minBedrooms.Value);
            }

            if (maxBedrooms.HasValue)
            {
                query = query.Where(p => p.Bedrooms <= maxBedrooms.Value);
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

            // Handle date availability
            if (checkIn.HasValue && checkOut.HasValue)
            {
                query = query.Where(p => !p.Reservations.Any(r =>
                    r.ReservationStatus &&
                    ((checkIn >= r.CheckIn && checkIn < r.CheckOut) ||
                     (checkOut > r.CheckIn && checkOut <= r.CheckOut) ||
                     (checkIn <= r.CheckIn && checkOut >= r.CheckOut))));

                // Also check UnavailableDates
                query = query.Where(p => !p.UnavailableDates.Any(ud =>
                    ud.Date >= checkIn && ud.Date < checkOut));
            }

            // Apply rating filter
            if (minRating.HasValue)
            {
                query = query.Where(p =>
                    p.Reviews.Any() &&
                    p.Reviews.Where(r => r.DisputeStatus != DisputeStatus.ValidDispute)
                             .Average(r => (decimal)r.Rating) >= minRating.Value);
            }

            var properties = await query.ToListAsync();

            // Set ViewBag data for the view
            ViewBag.TotalCount = await _context.Properties.CountAsync(p => p.PropertyStatus && p.IsActive);
            ViewBag.FilteredCount = properties.Count;
            ViewBag.Categories = await _context.Categories.ToListAsync();

            return View("Index", properties);
        }

        private IQueryable<Property> ApplyLocationFilter(IQueryable<Property> query, string location)
        {
            var searchTerms = location.ToLower()
                .Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(term => term.Trim())
                .Where(term => !string.IsNullOrWhiteSpace(term))
                .ToArray();

            return query.Where(p => searchTerms.All(term =>
                p.Street.ToLower().Contains(term) ||
                p.City.ToLower().Contains(term) ||
                p.State.ToLower().Contains(term) ||
                p.Zip.Contains(term)));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}