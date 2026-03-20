using TenderSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TenderSystem.Controllers
{
    [Authorize]
    public class TransactionController : Controller
    {
        private readonly TenderSystemContext _context;

        public TransactionController(TenderSystemContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string searchString,
            string paymentStatus,
            DateTime? startDate,
            DateTime? endDate)
        {
            int userId = Convert.ToInt16(User.Identity!.Name);

            var query = _context.Payments
                .Where(p => (p.PayByUser == userId || p.PayToUser == userId)
                            && p.PaymentMethod == "eSewa")
                .Include(p => p.PayTender)
                .Include(p => p.PayByUserNavigation)
                .Include(p => p.PayToUserNavigation)
                .Include(p => p.PayCompany)
                .AsQueryable();

            // Search by tender title or user name
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p =>
                    (p.PayTender != null && p.PayTender.Title.Contains(searchString)) ||
                    p.PayByUserNavigation.FirstName.Contains(searchString) ||
                    p.PayToUserNavigation.FirstName.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(paymentStatus))
            {
                query = query.Where(p => p.PaymentStatus == paymentStatus);
            }

            if (startDate.HasValue)
            {
                query = query.Where(p => p.PaymentDate >= startDate);
            }

            if (endDate.HasValue)
            {
                query = query.Where(p => p.PaymentDate <= endDate);
            }

            var payments = await query
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();

            // Totals
            var totalEarnings = payments
                .Where(p => p.PayToUser == userId && p.PaymentStatus == "Verified")
                .Sum(p => p.PaymentAmount);

            var totalSpent = payments
                .Where(p => p.PayByUser == userId && p.PaymentStatus == "Verified")
                .Sum(p => p.PaymentAmount);

            ViewBag.TotalEarnings = totalEarnings;
            ViewBag.TotalSpent = totalSpent;
            ViewBag.CurrentFilter = searchString;
            ViewBag.PaymentStatusFilter = paymentStatus;
            ViewBag.StartDateFilter = startDate;
            ViewBag.EndDateFilter = endDate;

            return View(payments);
        }

        public async Task<IActionResult> Details(int id)
        {
            int userId = Convert.ToInt16(User.Identity!.Name);

            var payment = await _context.Payments
                .Include(p => p.PayTender)
                .Include(p => p.PayByUserNavigation)
                .Include(p => p.PayToUserNavigation)
                .Include(p => p.PayCompany)
                .FirstOrDefaultAsync(p => p.PaymentId == id
                    && (p.PayByUser == userId || p.PayToUser == userId));

            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }
    }
}