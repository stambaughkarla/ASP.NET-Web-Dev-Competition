using FinalProject.DAL;
using FinalProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;

namespace FinalProject.Controllers
{
    public class RoleHostController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public RoleHostController(AppDbContext context, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }
        public IActionResult Index()
        {
            return View();
        }
        public ActionResult ViewReviews(int hostId)
        {

            string userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (userId == null)
            {
                return RedirectToAction("Reports", "Account");
            }

            var propertyIds = _context.Properties
                .Where(p => p.Host.Id == userId)
                .Select(p => p. PropertyID)
                .ToList();
            if (propertyIds ==  null)
            {
                return RedirectToAction("Register", "Account");
            }
            var reviews = _context.Reviews
            .Where(r => propertyIds.Contains(r.PropertyID))
            .ToList();

            

            return View(reviews);
        }

        [HttpPost]
        public IActionResult DisputeReview(int reviewId, string reason)
        {
            try
            {
                // lowkey i need to get the ones without space or null or whitespace
                var review = _context.Reviews.FirstOrDefault(r => r.ReviewID == reviewId);
                if (review == null)
                {
                    TempData["ErrorMessage"] = "Review not found.";
                    return RedirectToAction("ViewReviews");
                }

                // Update for reqs in doc
                review.DisputeStatus = DisputeStatus.Disputed; 
                review.HostComments = reason;

                
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Review disputed successfully, check back here soon to see Host Desicion.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while disputing the review.";
            }

            return RedirectToAction("ViewReviews");
        }



    }
}
