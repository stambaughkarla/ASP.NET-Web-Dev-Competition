using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using FinalProject.Models;
using FinalProject.DAL;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace FinalProject.Controllers
{
    public class ReviewController : Controller
    {
        private readonly AppDbContext _context;

        public ReviewController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Reviews for a property
        public async Task<IActionResult> Index(int? propertyId)
        {
            if (propertyId == null)
            {
                return NotFound();
            }

            var reviews = await _context.Reviews
                .Include(r => r.Customer)
                .Include(r => r.Property)
                .Where(r => r.PropertyID == propertyId &&
                           r.DisputeStatus != DisputeStatus.ValidDispute)
                .ToListAsync();

            return View(reviews);
        }

        [Authorize]
        public async Task<IActionResult> Create(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            // Fetch reservation based on ReservationID or ConfirmationNumber
            var reservation = await _context.Reservations
                .Include(r => r.Property)
                .FirstOrDefaultAsync(r => r.ConfirmationNumber == id &&
                                          r.CustomerID == userId);

            if (reservation == null)
            {
                // Redirect if no reservation found or if it does not belong to the current user
                return RedirectToAction("Index", "Home");
            }

            // Check if the user has already reviewed this property
            var existingReview = await _context.Reviews
            .Include(r => r.Property)
                .FirstOrDefaultAsync(r => r.PropertyID == reservation.PropertyID && r.CustomerID == userId);


            var model = new ReviewCreateEditViewModel
            {
                PropertyID = reservation.PropertyID,
                CustomerID = userId,
                ReservationID = reservation.ReservationID,
                ExistingReview = existingReview
            };

            return View(model);
        }


        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ReservationID,PropertyID,CustomerID,Rating,ReviewText")] Review review)
        {
            ModelState.Remove("CustomerID");
            ModelState.Remove("Property");
            ModelState.Remove("Customer");
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            // Fetch reservation and make sure it's valid
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.PropertyID == review.PropertyID &&
                                          r.CustomerID == userId &&
                                          r.CheckIn <= DateTime.Now);

            if (reservation == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Check if this is an update or a new review
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.PropertyID == review.PropertyID && r.CustomerID == userId);

            if (existingReview != null)
            {
               
                    // Attach the entity to the context if it's not tracked
                    _context.Reviews.Attach(existingReview);
                    existingReview.Rating = review.Rating;
                    existingReview.ReviewText = review.ReviewText;
                    existingReview.DisputeStatus = DisputeStatus.NoDispute;

                    await _context.SaveChangesAsync();
                }

                else
                {
                // If no review exists, create a new one
                review.CustomerID = userId;
                review.DisputeStatus = DisputeStatus.NoDispute;

                _context.Add(review);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ReviewID == id && r.CustomerID == userId);

            if (review == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(review);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int reviewId, [Bind("ReviewID, PropertyID,CustomerID, Rating, ReviewText")] Review review)
        {
            if (review.ReviewID == 0)
            {
                return NotFound();
            }

            string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ReviewID == review.ReviewID && r.CustomerID == userId);

            if (existingReview == null)
            {
                return RedirectToAction("Index", "Reservation");
            }

            if (ModelState.IsValid)
            {
                existingReview.Rating = review.Rating;
                existingReview.ReviewText = review.ReviewText;
                existingReview.DisputeStatus = DisputeStatus.NoDispute; // Reset or set based on your logic

                _context.Update(existingReview);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Reservation");
            }

            return View(review);
        }



        // POST: Reviews/Dispute/5
        [HttpPost]
        [Authorize(Roles = "Host")]
        public async Task<IActionResult> Dispute(int id, string disputeDescription)
        {
            var review = await _context.Reviews
                .Include(r => r.Property)
                .FirstOrDefaultAsync(r => r.ReviewID == id);

            if (review == null)
            {
                return NotFound();
            }

            // Verify the host owns this property
            string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (review.Property.Host.Id != userId)
            {
                return Forbid();
            }

            // Change the status to Disputed when a host disputes it
            review.DisputeStatus = DisputeStatus.Disputed;
            review.HostComments = disputeDescription;
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Properties", new { id = review.PropertyID });
        }
    }
}