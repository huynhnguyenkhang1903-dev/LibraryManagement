using System.Diagnostics;
using LibraryManagement1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement1.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly LibraryDbContext _context;

        public HomeController(LibraryDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Stats calculations
            var totalBooks = await _context.Books.SumAsync(b => b.Quantity);
            var totalReaders = await _context.Readers.CountAsync();
            var activeBorrows = await _context.BorrowDetails.CountAsync(d => d.ReturnDate == null);
            var overdueTickets = await _context.BorrowTickets.CountAsync(t => t.Status == "Overdue");

            ViewData["TotalBooks"] = totalBooks;
            ViewData["TotalReaders"] = totalReaders;
            ViewData["ActiveBorrows"] = activeBorrows;
            ViewData["OverdueTickets"] = overdueTickets;

            // 2. Load chart data (Book counts per Category)
            var categoryStats = await _context.Categories
                .Select(c => new 
                {
                    CategoryName = c.Name,
                    BookCount = c.Books.Sum(b => b.Quantity)
                })
                .ToListAsync();

            ViewBag.ChartLabels = categoryStats.Select(c => c.CategoryName).ToArray();
            ViewBag.ChartData = categoryStats.Select(c => c.BookCount).ToArray();

            // 3. Monthly borrowing statistics (Last 6 months)
            var monthlyStats = await _context.BorrowTickets
                .GroupBy(t => new { t.BorrowDate.Year, t.BorrowDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Year).ThenByDescending(x => x.Month)
                .Take(6)
                .ToListAsync();

            // Reverse to ascending order for chronological charts
            monthlyStats.Reverse();
            ViewBag.MonthlyLabels = monthlyStats.Select(m => $"{m.Month}/{m.Year}").ToArray();
            ViewBag.MonthlyData = monthlyStats.Select(m => m.Count).ToArray();

            // 4. Recent activities list (Recent borrow tickets)
            var recentActivities = await _context.BorrowTickets
                .Include(t => t.Reader)
                .Include(t => t.BorrowDetails)
                .OrderByDescending(t => t.BorrowDate)
                .Take(5)
                .ToListAsync();

            return View(recentActivities);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult DatabaseSchema()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
