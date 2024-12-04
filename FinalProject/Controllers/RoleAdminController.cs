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
        private readonly ReservationController _reservationController;


        public RoleAdminController(AppDbContext context,
                                 UserManager<AppUser> userManager,
                                 RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _reservationController = new ReservationController(context);

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
                    HireStatus = user.HireStatus ?? true,
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

        // GET: RoleAdmin/ManageReservations
        public async Task<IActionResult> ManageReservations()
        {
            var reservations = await _context.Reservations
                .Include(r => r.Property)
                .Include(r => r.Customer)
                .OrderByDescending(r => r.CheckIn)
                .ToListAsync();

            // Populate customer dropdown with proper value - ensure it matches UserId
            ViewBag.Customers = new SelectList(
                await _userManager.Users
                    .Where(u => !u.Email.EndsWith("@bevobnb.com"))
                    .Select(u => new
                    {
                        Id = u.Id,  // This should match CustomerID in Reservation
                        Text = $"{u.Email} ({u.FirstName} {u.LastName})"
                    })
                    .ToListAsync(),
                "Id",
                "Text"
            );

            // Populate property dropdown
            ViewBag.Properties = new SelectList(
                await _context.Properties
                    .Where(p => p.PropertyStatus)
                    .Select(p => new
                    {
                        Id = p.PropertyID,  // This should match PropertyID in Reservation
                        Text = $"{p.Street}, {p.City}"
                    })
                    .ToListAsync(),
                "Id",
                "Text"
            );

            return View(reservations);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReservation([Bind("CustomerID,PropertyID,CheckIn,CheckOut,NumOfGuests")] Reservation reservation)
        {
            // Explicitly check if CustomerID and PropertyID were received
            if (string.IsNullOrEmpty(reservation.CustomerID) || reservation.PropertyID == 0)
            {
                TempData["ErrorMessage"] = "Must select both a customer and property.";
                return RedirectToAction(nameof(ManageReservations));
            }

            // Get property details
            var property = await _context.Properties.FindAsync(reservation.PropertyID);
            if (property == null)
            {
                TempData["ErrorMessage"] = "Selected property not found.";
                return RedirectToAction(nameof(ManageReservations));
            }

            // Set the basic reservation details
            reservation.WeekdayPrice = property.WeekdayPrice;
            reservation.WeekendPrice = property.WeekendPrice;
            reservation.CleaningFee = property.CleaningFee;
            reservation.DiscountRate = 0m; // Default to no discount
            reservation.ReservationStatus = true;

            // Get next confirmation number
            int lastConfirmationNumber = await _context.Reservations
                .MaxAsync(r => (int?)r.ConfirmationNumber) ?? 21900;
            reservation.ConfirmationNumber = lastConfirmationNumber + 1;

            try
            {
                _context.Reservations.Add(reservation);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Reservation #{reservation.ConfirmationNumber} created successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to create reservation.";
            }

            return RedirectToAction(nameof(ManageReservations));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelReservation(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                TempData["ErrorMessage"] = "Reservation not found.";
                return RedirectToAction(nameof(ManageReservations));
            }

            if (reservation.CheckIn <= DateTime.Now.AddDays(1))
            {
                TempData["ErrorMessage"] = "Cannot cancel reservations within 24 hours of check-in.";
                return RedirectToAction(nameof(ManageReservations));
            }

            try
            {
                reservation.ReservationStatus = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Reservation cancelled successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to cancel reservation.";
            }

            return RedirectToAction(nameof(ManageReservations));
        }



    }

}