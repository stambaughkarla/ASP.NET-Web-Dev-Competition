using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinalProject.Models;
using FinalProject.DAL;

namespace FinalProject.Controllers
{
    public class ReportController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private const decimal COMMISSION_RATE = 0.10m;

        public ReportController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Admin")]
        public IActionResult AdminReport()
        {
            var viewModel = new ReportViewModel
            {
                IsAllTime = true  // Flag to indicate showing all-time data
            };

            return AdminReport(viewModel).Result;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminReport(ReportViewModel viewModel)
        {
            IQueryable<Reservation> query = _context.Reservations
                .Include(r => r.Property)
                .Where(r => r.ReservationStatus); // Only valid reservations

            if (viewModel.StartDate.HasValue && viewModel.EndDate.HasValue)
            {
                query = query.Where(r => (r.CheckIn <= viewModel.EndDate.Value &&
                                         r.CheckOut >= viewModel.StartDate.Value) ||
                                        (r.CheckIn <= viewModel.StartDate.Value &&
                                         r.CheckOut >= viewModel.StartDate.Value) ||
                                        (r.CheckIn <= viewModel.EndDate.Value &&
                                         r.CheckOut >= viewModel.EndDate.Value));
            }

            var reservations = await query.ToListAsync();

            decimal totalStayRevenue = 0;
            foreach (var reservation in reservations)
            {
                var nights = (reservation.CheckOut - reservation.CheckIn).Days;
                var weekdayNights = Enumerable.Range(0, nights)
                    .Count(offset => !new[] { DayOfWeek.Friday, DayOfWeek.Saturday }
                    .Contains(reservation.CheckIn.AddDays(offset).DayOfWeek));
                var weekendNights = nights - weekdayNights;

                decimal stayPrice = (weekdayNights * reservation.WeekdayPrice) +
                                   (weekendNights * reservation.WeekendPrice);

                if (nights >= reservation.Property.MinNightsForDiscount &&
                    reservation.Property.DiscountRate.HasValue)
                {
                    stayPrice -= stayPrice * (reservation.Property.DiscountRate.Value / 100m);
                }

                totalStayRevenue += stayPrice;
            }

            viewModel.TotalRevenue = totalStayRevenue;
            viewModel.TotalCommission = totalStayRevenue * COMMISSION_RATE;
            viewModel.TotalCompletedReservations = reservations.Count;
            viewModel.AverageCommissionPerReservation = viewModel.TotalCompletedReservations > 0
                ? viewModel.TotalCommission / viewModel.TotalCompletedReservations
                : 0;
            viewModel.TotalProperties = reservations
                .Select(r => r.Property)
                .Where(p => p.PropertyStatus)
                .Select(p => p.PropertyID)
                .Distinct()
                .Count();

            return View(viewModel);
        }

    }
}