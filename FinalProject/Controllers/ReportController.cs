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
        private const decimal COMMISSION_RATE = 0.10m; // 10% commission

        public ReportController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Host")]
        public IActionResult HostReport()
        {
            var viewModel = new ReportViewModel();
            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Host")]
        public async Task<IActionResult> HostReport(ReportViewModel viewModel)
        {
            // Get the current user
            var user = await _userManager.GetUserAsync(User);

            // Query reservations for this host
            var query = _context.Reservations
                .Include(r => r.Property)
                .Where(r => r.Property.Host.Id == user.Id &&
                           r.ReservationStatus == true);

            // Apply date filters if provided
            if (viewModel.StartDate.HasValue)
            {
                query = query.Where(r => r.CheckOut >= viewModel.StartDate);
            }

            if (viewModel.EndDate.HasValue)
            {
                query = query.Where(r => r.CheckIn <= viewModel.EndDate);
            }

            var reservations = await query.ToListAsync();

            // Calculate totals for each property
            foreach (var reservation in reservations)
            {
                var propertyDetail = viewModel.PropertyDetails
                    .FirstOrDefault(p => p.PropertyID == reservation.Property.PropertyID);

                if (propertyDetail == null)
                {
                    propertyDetail = new PropertyReportDetail
                    {
                        PropertyID = reservation.Property.PropertyID,
                        PropertyAddress = $"{reservation.Property.Street}, {reservation.Property.City}, {reservation.Property.State} {reservation.Property.Zip}"
                    };
                    viewModel.PropertyDetails.Add(propertyDetail);
                }

                // Calculate revenue (90% of stay revenue goes to host, 100% of cleaning fees)
                var stayRevenue = reservation.SubTotal - reservation.CleaningFee;
                propertyDetail.StayRevenue += stayRevenue * (1 - COMMISSION_RATE);
                propertyDetail.CleaningFees += reservation.CleaningFee;
                propertyDetail.TotalRevenue = propertyDetail.StayRevenue + propertyDetail.CleaningFees;
                propertyDetail.CompletedReservations++;
            }

            // Calculate summary totals
            viewModel.TotalStayRevenue = viewModel.PropertyDetails.Sum(p => p.StayRevenue);
            viewModel.TotalCleaningFees = viewModel.PropertyDetails.Sum(p => p.CleaningFees);
            viewModel.TotalRevenue = viewModel.TotalStayRevenue + viewModel.TotalCleaningFees;
            viewModel.TotalCompletedReservations = viewModel.PropertyDetails.Sum(p => p.CompletedReservations);

            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult AdminReport()
        {
            var viewModel = new ReportViewModel();
            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminReport(ReportViewModel viewModel)
        {
            // Query all valid reservations
            var query = _context.Reservations
                .Include(r => r.Property)
                .Where(r => r.ReservationStatus == true);

            // Apply date filters if provided
            if (viewModel.StartDate.HasValue)
            {
                query = query.Where(r => r.CheckOut >= viewModel.StartDate);
            }

            if (viewModel.EndDate.HasValue)
            {
                query = query.Where(r => r.CheckIn <= viewModel.EndDate);
            }

            var reservations = await query.ToListAsync();

            // Calculate commission and totals
            viewModel.TotalCommission = 0;
            viewModel.TotalCompletedReservations = 0;

            foreach (var reservation in reservations)
            {
                var stayRevenue = reservation.SubTotal - reservation.CleaningFee;
                viewModel.TotalCommission += stayRevenue * COMMISSION_RATE; // BevoBnB gets 10%
                viewModel.TotalCompletedReservations++;
            }

            // Calculate average commission per reservation
            viewModel.AverageCommissionPerReservation = viewModel.TotalCompletedReservations > 0
                ? viewModel.TotalCommission / viewModel.TotalCompletedReservations
                : 0;

            // Get total number of approved properties
            viewModel.TotalProperties = await _context.Properties
                .Where(p => p.PropertyStatus == true)
                .CountAsync();

            viewModel.TotalRevenue = viewModel.TotalCommission; // For admin, total revenue is commission

            return View(viewModel);
        }
    }
}