using System.Diagnostics;
using LibraryManagement1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement1.Controllers
{
    public class HomeController : Controller
    {
        private readonly LibraryDbContext _context;

        public HomeController(LibraryDbContext context)
        {
            _context = context;
        }

        // GET: Home/Index (Trang chủ công cộng / Web Interface)
        [AllowAnonymous]
        public async Task<IActionResult> Index(string searchString, int? categoryId)
        {
            var query = _context.Books
                .Include(b => b.Category)
                .Include(b => b.Author)
                .Include(b => b.Publisher)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                var lowerSearch = searchString.ToLower();
                query = query.Where(b => 
                    b.BookCode.ToLower().Contains(lowerSearch) || 
                    b.Title.ToLower().Contains(lowerSearch) || 
                    b.Author!.Name.ToLower().Contains(lowerSearch));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(b => b.CategoryId == categoryId.Value);
            }

            ViewData["Categories"] = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(await _context.Categories.ToListAsync(), "Id", "Name", categoryId);
            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentCategory"] = categoryId;

            var books = await query.ToListAsync();
            return View(books);
        }

        // GET: Home/Dashboard (Trang Dashboard quản trị)
        [Authorize]
        public async Task<IActionResult> Dashboard()
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

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [Authorize]
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
