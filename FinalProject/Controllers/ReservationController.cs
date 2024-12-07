using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FinalProject.Models;
using FinalProject.DAL;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;

namespace FinalProject.Controllers
{
    public class ReservationController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private const Int32 FIRST_CONFIRMATION_NUMBER = 21901;

        public ReservationController(AppDbContext context, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: Reservations - Show user's reservations
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var reservations = await _context.Reservations
                .Include(r => r.Property)
                .Where(r => r.CustomerID == userId && r.ReservationStatus == true)
                .OrderByDescending(r => r.CheckIn)
                .ToListAsync();

            return View(reservations);
        }


        // GET: Reservations/Create/5 (PropertyId)
        [Authorize]
        public async Task<IActionResult> Create(int? id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            if (currentUser != null)
            {
                ViewBag.Customer = currentUser;  // Pass Customer ID to the View

            }

            if (id == null)
            {
                return NotFound();
            }

            // Fetch the property details
            var property = await _context.Properties
                .Include(p => p.Category) // Include related data if necessary
                .FirstOrDefaultAsync(p => p.PropertyID == id);

            if (property == null)
            {
                return NotFound();
            }

            // Create a Reservation model to pass to the view
            var reservation = new Reservation
            {
                PropertyID = property.PropertyID, // Populate PropertyID
                Property = property,             // Include property details
                CustomerID = currentUser.Id      // Set the current user as the customer
            };

            return View(reservation);
        }


        // POST: Reservations/Create
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PropertyID,CheckIn,CheckOut,NumOfGuests")] Reservation reservation)
        {
            // Remove the properties that are not needed for the reservation
            ModelState.Remove("CustomerID");
            ModelState.Remove("Property");
            ModelState.Remove("Customer");

            if (ModelState.IsValid)
            {
                // Fetch the property details
                var property = await _context.Properties
                    .FirstOrDefaultAsync(p => p.PropertyID == reservation.PropertyID);

                if (property == null)
                {
                    return NotFound();
                }

                // Check for conflicts
                bool hasConflict = await _context.Reservations
                    .AnyAsync(r => r.PropertyID == reservation.PropertyID &&
                                   r.ReservationStatus &&
                                   ((reservation.CheckIn >= r.CheckIn && reservation.CheckIn < r.CheckOut) ||
                                    (reservation.CheckOut > r.CheckIn && reservation.CheckOut <= r.CheckOut) ||
                                    (reservation.CheckIn <= r.CheckIn && reservation.CheckOut >= r.CheckOut)));

                if (hasConflict)
                {
                    ModelState.AddModelError("", "Selected dates conflict with an existing reservation.");
                    reservation.Property = property; // Populate the property before returning the view
                    return View(reservation);
                }

                // Assign current user details
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return Unauthorized();

                //reservation.Customer = currentUser;
                reservation.Customer = currentUser;
                reservation.CustomerID = currentUser.Id;
                reservation.Property = property;

                reservation.WeekdayPrice = property.WeekdayPrice;
                reservation.WeekendPrice = property.WeekendPrice;
                reservation.CleaningFee = property.CleaningFee;
                //reservation.CustomerID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;


                // Set confirmation number
                int lastConfirmationNumber = await _context.Reservations
                    .MaxAsync(r => (int?)r.ConfirmationNumber) ?? 9999;
                reservation.ConfirmationNumber = lastConfirmationNumber + 1;

                // Add to cart (session)
                var cart = HttpContext.Session.GetObjectFromJson<List<Reservation>>("Cart") ?? new List<Reservation>();
                cart.Add(reservation);
                HttpContext.Session.SetObjectAsJson("Cart", cart);

                return RedirectToAction(nameof(Cart));
            }

            // Fetch the property details to populate the view again
            reservation.Property = await _context.Properties
                .FirstOrDefaultAsync(p => p.PropertyID == reservation.PropertyID);

            if (reservation.Property == null)
            {
                return NotFound();
            }

            return View(reservation);
        }



        // GET: Reservations/Cart
        private const string CartSessionKey = "Cart";

        
        [Authorize]
        public IActionResult Cart()
        {
            var cart = GetCart();
            return View(cart); // Pass the cart to the view
        }

        private List<Reservation> GetCart()
        {
            return HttpContext.Session.GetObjectFromJson<List<Reservation>>(CartSessionKey) ?? new List<Reservation>();
        }

        // Helper method: Save the cart to the session
        private void SaveCart(List<Reservation> cart)
        {
            HttpContext.Session.SetObjectAsJson(CartSessionKey, cart);
        }

        // POST: Reservations/DeleteFromCart
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteFromCart(int confirmationNumber)
        {
            // Retrieve the cart from the session
            var cart = GetCart();

            // Find the reservation with the matching confirmation number
            var reservationToRemove = cart.FirstOrDefault(r => r.ConfirmationNumber == confirmationNumber);
            if (reservationToRemove != null)
            {
                cart.Remove(reservationToRemove); // Remove the reservation from the cart
                SaveCart(cart); // Save the updated cart back to the session
            }

            // Redirect to the Cart view
            return RedirectToAction(nameof(Cart));
        }


        // POST: Reservations/Checkout
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<Reservation>>("Cart") ?? new List<Reservation>();

            if (!cart.Any())
            {
                return RedirectToAction(nameof(Cart));
            }

            // Final validation
            foreach (var reservation in cart)
            {
                bool exists = await _context.Reservations
            .AnyAsync(r => r.ConfirmationNumber == reservation.ConfirmationNumber);

                if (exists)
                {
                    ModelState.AddModelError("",
                        $"Reservation with Confirmation Number {reservation.ConfirmationNumber} already exists.");
                    return RedirectToAction(nameof(Cart)); // Redirect back to cart for correction
                }

                // Attach existing entities to the context to avoid duplication
                _context.Attach(reservation.Property);
                _context.Attach(reservation.Customer);

                reservation.ReservationStatus = true;
                _context.Reservations.Add(reservation);
            }

            await _context.SaveChangesAsync();
            HttpContext.Session.Remove("Cart");

            return RedirectToAction(nameof(Confirmation), new { id = cart.First().ConfirmationNumber });
        }


        // GET: Reservations/Cancel/5
        [Authorize]
        public async Task<IActionResult> Cancel(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations
                .Include(r => r.Property)
                .FirstOrDefaultAsync(r => r.ReservationID == id);

            if (reservation == null)
            {
                return NotFound();
            }

            // Check if cancellation is allowed (more than 1 day before check-in)
            if (reservation.CheckIn <= DateTime.Now.AddDays(1))
            {
                TempData["ErrorMessage"] = "Cancellation is only allowed more than 1 day before check-in.";
                return RedirectToAction(nameof(Index));
            }

            return View(reservation);
        }

        // POST: Reservations/Cancel/5
        [HttpPost, ActionName("Cancel")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> CancelConfirmed(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            reservation.ReservationStatus = false;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Reservations/Confirmation/5
        public async Task<IActionResult> Confirmation(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations
                .Include(r => r.Property)
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(r => r.ConfirmationNumber == id);

            if (reservation == null)
            {
                return NotFound();
            }

            return View(reservation);
        }
    }

    // Extension methods for Session
    public static class SessionExtensions
    {
        public static void SetObjectAsJson(this ISession session, string key, object value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        public static T GetObjectFromJson<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default(T) : JsonSerializer.Deserialize<T>(value);
        }
    }
}