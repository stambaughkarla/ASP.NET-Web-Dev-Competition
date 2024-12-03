using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FinalProject.Models;
using FinalProject.DAL;
using Microsoft.AspNetCore.Mvc.Rendering;

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

        // GET: RoleAdmin/ResetPassword/5
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new IndexViewModel
            {
                UserID = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            };

            return View(model);
        }

        // POST: RoleAdmin/ResetPassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id, string newPassword)
        {
            if (string.IsNullOrEmpty(newPassword))
            {
                ModelState.AddModelError("", "Password is required");
                return View();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            string resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Password has been reset successfully";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            var model = new IndexViewModel { UserID = id, Email = user.Email };
            return View(model);
        }

        // Helper method to add Identity errors to ModelState
        private void AddErrorsFromResult(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }


        public async Task<IActionResult> Dashboard()
        {
            // User Statistics
            var users = await _userManager.Users.ToListAsync();
            ViewBag.TotalUsers = users.Count;
            ViewBag.ActiveUsers = users.Count(u => u.HireStatus ?? false);
            ViewBag.TotalAdmins = users.Count(u => u.Email.EndsWith("@bevobnb.com"));

            // Quick summary stats
            ViewBag.PendingProperties = await _context.Properties.CountAsync(p => !p.PropertyStatus);  // false = unapproved
            ViewBag.DisputedReviews = await _context.Reviews.CountAsync(r => r.DisputeStatus == DisputeStatus.Disputed); 

            // Get current month's stats using existing report logic
            var firstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            var reportQuery = _context.Reservations
                .Include(r => r.Property)
                .Where(r => r.ReservationStatus && // true = valid reservation
                            r.CheckOut >= firstDayOfMonth &&
                            r.CheckIn <= lastDayOfMonth);

            var reservations = await reportQuery.ToListAsync();

            ViewBag.CurrentMonthRevenue = 0m;
            ViewBag.CurrentMonthReservations = reservations.Count;

            foreach (var reservation in reservations)
            {
                var stayRevenue = reservation.SubTotal - reservation.CleaningFee;
                ViewBag.CurrentMonthRevenue += stayRevenue * 0.10m; // 10% commission
            }

            return View();
        }


        // GET: RoleAdmin/DisputedReviews
        public async Task<IActionResult> DisputedReviews()
        {
            var disputedReviews = await _context.Reviews
                .Include(r => r.Property)
                .Include(r => r.Customer)
                .Where(r => r.DisputeStatus == DisputeStatus.Disputed)
                .ToListAsync();

            return View(disputedReviews);
        }

        // POST: RoleAdmin/ResolveDispute
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveDispute(int reviewId, bool acceptDispute)
        {
            var review = await _context.Reviews
                .Include(r => r.Property)
                .FirstOrDefaultAsync(r => r.ReviewID == reviewId);

            if (review == null)
            {
                return NotFound();
            }

            review.DisputeStatus = acceptDispute ?
                DisputeStatus.ValidDispute :
                DisputeStatus.InvalidDispute;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Dispute has been {(acceptDispute ? "accepted" : "rejected")} successfully.";
            return RedirectToAction(nameof(DisputedReviews));
        }

        private async Task<decimal> CalculatePropertyRating(int propertyId)
        {
            var property = await _context.Properties
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.PropertyID == propertyId);

            if (property != null && property.Reviews.Any())
            {
                var validReviews = property.Reviews
                    .Where(r => r.DisputeStatus != DisputeStatus.ValidDispute)
                    .ToList();

                if (validReviews.Any())
                {
                    return Math.Round((decimal)validReviews.Average(r => r.Rating), 1);
                }
            }

            return 0;
        }

        /// <summary>
        /// Displays the reservation management page with a list of all reservations and forms for creating new ones
        /// </summary>
        /// <returns>View with reservations list and dropdowns for customers/properties</returns>
        public async Task<IActionResult> ManageReservations()
        {
            // Get all reservations with related property and customer data
            var reservations = await _context.Reservations
                .Include(r => r.Property)
                .Include(r => r.Customer)
                .OrderByDescending(r => r.CheckIn)
                .ToListAsync();

            // Populate customer dropdown - exclude admin accounts
            ViewBag.Customers = new SelectList(await _context.Users
                .Where(u => !u.Email.EndsWith("@bevobnb.com"))
                .Select(u => new { u.Id, DisplayName = $"{u.Email} ({u.FirstName} {u.LastName})" })
                .ToListAsync(), "Id", "DisplayName");

            // Populate property dropdown - only show approved properties
            ViewBag.Properties = new SelectList(
                await _context.Properties
                    .Where(p => p.PropertyStatus)
                    .Select(p => new { p.PropertyID, DisplayName = $"{p.Street}, {p.City} ({p.PropertyName})" })
                    .ToListAsync(),
                "PropertyID",
                "DisplayName"
            );

            return View(reservations);
        }

        /// <summary>
        /// Creates a new reservation on behalf of a customer
        /// Handles all validation including date conflicts and pricing calculations
        /// </summary>
        /// <param name="reservation">Reservation details from form submission</param>
        /// <returns>Redirects back to management page with success/error message</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReservation(Reservation reservation)
        {
            if (ModelState.IsValid)
            {
                // Validate check-in date is in the future
                if (reservation.CheckIn <= DateTime.Now)
                {
                    ModelState.AddModelError("", "Check-in date must be in the future.");
                    return RedirectToAction(nameof(ManageReservations));
                }

                // Check for overlapping reservations
                bool hasConflict = await _context.Reservations
                    .AnyAsync(r => r.PropertyID == reservation.PropertyID &&
                                  r.ReservationStatus &&
                                  ((reservation.CheckIn >= r.CheckIn && reservation.CheckIn < r.CheckOut) ||
                                   (reservation.CheckOut > r.CheckIn && reservation.CheckOut <= r.CheckOut) ||
                                   (reservation.CheckIn <= r.CheckIn && reservation.CheckOut >= r.CheckOut)));

                if (hasConflict)
                {
                    TempData["ErrorMessage"] = "Selected dates conflict with an existing reservation.";
                    return RedirectToAction(nameof(ManageReservations));
                }

                // Get property details and set pricing
                var property = await _context.Properties.FindAsync(reservation.PropertyID);
                reservation.WeekdayPrice = property.WeekdayPrice;
                reservation.WeekendPrice = property.WeekendPrice;
                reservation.CleaningFee = property.CleaningFee;

                // Check if property has discount and convert to non-nullable decimal
                if (nights >= property.MinNightsForDiscount && property.DiscountRate.HasValue)
                {
                    reservation.DiscountRate = property.DiscountRate.Value / 100m; // Convert percentage to decimal
                }
                else
                {
                    reservation.DiscountRate = 0m; // No discount
                }

                // Generate sequential confirmation number
                int lastConfirmationNumber = await _context.Reservations
                    .MaxAsync(r => (int?)r.ConfirmationNumber) ?? 21900;
                reservation.ConfirmationNumber = lastConfirmationNumber + 1;

                reservation.ReservationStatus = true;

                _context.Add(reservation);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Reservation created successfully.";
            }

            return RedirectToAction(nameof(ManageReservations));
        }

        /// <summary>
        /// Cancels an existing reservation if allowed by cancellation policy
        /// </summary>
        /// <param name="id">ID of the reservation to cancel</param>
        /// <returns>Redirects back to management page with success/error message</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelReservation(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);

            if (reservation == null)
            {
                return NotFound();
            }

            // Check cancellation policy - must be more than 24 hours before check-in
            if (reservation.CheckIn <= DateTime.Now.AddDays(1))
            {
                TempData["ErrorMessage"] = "Cannot cancel reservations within 24 hours of check-in.";
                return RedirectToAction(nameof(ManageReservations));
            }

            // Mark as cancelled rather than deleting
            reservation.ReservationStatus = false;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Reservation cancelled successfully.";

            return RedirectToAction(nameof(ManageReservations));
        }


    }

}