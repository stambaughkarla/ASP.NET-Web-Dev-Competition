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
                .Where(p => p.PropertyStatus)
                .ToListAsync();



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
                    p.Reviews.Where(r => r.DisputeStatus != DisputeStatus.ValidDispute)
                             .Average(r => (decimal)r.Rating) >= minRating.Value);
            }

            var properties = await query.ToListAsync();



            // Populate ViewBag data for the view
            ViewBag.TotalCount = await _context.Properties.CountAsync();
            ViewBag.FilteredCount = properties.Count;
            ViewBag.Categories = await _context.Categories.ToListAsync();

            // Return the search view with results
            return View("Index", properties);
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