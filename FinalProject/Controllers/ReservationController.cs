using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FinalProject.Models;
using FinalProject.DAL;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

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


        if (userId == null)
        {
            return Unauthorized();
        }

            var reservations = await _context.Reservations
                .Include(r => r.Property)
                .Where(r => r.CustomerID == userId)
                .OrderBy(r => r.CheckIn)
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
                .Include(p => p.Reservations)
                .FirstOrDefaultAsync(p => p.PropertyID == id);

            if (property == null)
            {
                return NotFound();
            }

            // Fetch existing reservations for the property
            var reservedDates = await _context.Reservations
                .Where(r => r.PropertyID == id && r.ReservationStatus == true) // Only active reservations
                .Select(r => new
                {
                    start = r.CheckIn,
                    end = r.CheckOut
                })
                .ToListAsync();

            // Fetch unavailability dates for the property
            var unavailabilityDates = await _context.Unavailabilities
                .Where(u => u.PropertyID == id) // Filter by property ID
                .Select(u => u.Date) // Select only the date
                .ToListAsync();

            ViewBag.ReservedDates = reservedDates;
            ViewBag.UnavailabilityDates = unavailabilityDates;

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


                var overlappingReservations = await _context.Reservations
                    .Where(r => r.CustomerID == currentUser.Id) // Only for the current user
                    .Where(r => r.ReservationStatus == true)
                    .Where(r => r.CheckOut > reservation.CheckIn && r.CheckIn < reservation.CheckOut) // Overlapping dates
                    .ToListAsync();



                if (overlappingReservations.Any())
                {
                    TempData["ErrorMessage"] = "The selected dates overlap with a reservation you already made. Please choose different dates.";
                    return View(reservation);
                }

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


            // Check for overlaps within the cart itself
            for (int i = 0; i < cart.Count; i++)
            {
                for (int j = i + 1; j < cart.Count; j++)
                {
                    // Check for overlapping dates between reservations in the cart
                    if (cart[i].CheckOut > cart[j].CheckIn && cart[i].CheckIn < cart[j].CheckOut)
                    {
                        TempData["ErrorMessage"] = "The selected dates for your stays overlap. Please modify your cart.";
                        return RedirectToAction(nameof(Cart));
                    }
                }
            }

            decimal subtotal = 0;
            decimal cleaningFees = 0;
            const decimal TAX_RATE = 0.07m;
            decimal tax = 0;
            decimal totaltax = 0;
            decimal grandtotal = 0;
            decimal totaldiscountamount = 0;

            // Final validation
            foreach (var reservation in cart)
            {
                decimal discountamount = 0;

                // Check for existing reservation
                bool exists = await _context.Reservations
                    .AnyAsync(r => r.ConfirmationNumber == reservation.ConfirmationNumber);

                if (exists)
                {
                    ModelState.AddModelError("",
                        $"Reservation with Confirmation Number {reservation.ConfirmationNumber} already exists.");
                    return RedirectToAction(nameof(Cart)); // Redirect back to cart for correction
                }

                var nights = (reservation.CheckOut - reservation.CheckIn).Days;
                var weekdayNights = Enumerable.Range(0, nights)
                .Count(offset => !new[] { DayOfWeek.Friday, DayOfWeek.Saturday }
                .Contains(reservation.CheckIn.AddDays(offset).DayOfWeek));
                var weekendNights = nights - weekdayNights;

                decimal stayPrice = (weekdayNights * reservation.WeekdayPrice) +
                (weekendNights * reservation.WeekendPrice);

                if (nights >= reservation.Property.MinNightsForDiscount)
                {
                    decimal discountRate = reservation.Property.DiscountRate ?? 0;
                    discountamount = stayPrice * discountRate;
                }


                subtotal += stayPrice;
                cleaningFees += reservation.CleaningFee;
                tax = (stayPrice + reservation.CleaningFee - discountamount) * TAX_RATE;
                totaldiscountamount += discountamount;
                totaltax = (subtotal + cleaningFees) * TAX_RATE;
                grandtotal = subtotal - discountamount + cleaningFees + tax;

                decimal reservationTax = (stayPrice + reservation.CleaningFee) * TAX_RATE;
                decimal reservationTotal = stayPrice - discountamount + reservation.CleaningFee + tax;

                // Attach existing entities to avoid duplication
                _context.Attach(reservation.Property);
                _context.Attach(reservation.Customer);

                reservation.ReservationStatus = true;
                _context.Reservations.Add(reservation);
            }

            await _context.SaveChangesAsync();
            HttpContext.Session.Remove("Cart");

            TempData["Subtotal"] = subtotal.ToString("C");
            if (totaldiscountamount > 0)
            {
                TempData["TotalDiscount"] = totaldiscountamount.ToString("C");
            }
            TempData["CleaningFee"] = cleaningFees.ToString("C");
            TempData["Tax"] = tax.ToString("C");
            TempData["GrandTotal"] = grandtotal.ToString("C");

            // Redirect to confirmation view
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> CancelConfirmed(int id)
        {
            var reservation = await _context.Reservations
            .Include(r => r.Property)
            .FirstOrDefaultAsync(r => r.ReservationID == id);

            if (reservation == null)
            {
                TempData["InfoMessage"] = "Reservation not found.";
                return RedirectToAction(nameof(Index));
            }

            // Mark the reservation as cancelled
            reservation.ReservationStatus = false; // Assuming false indicates cancellation
            _context.Reservations.Update(reservation);
            await _context.SaveChangesAsync();

            // Redirect back to the index page to show all reservations
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