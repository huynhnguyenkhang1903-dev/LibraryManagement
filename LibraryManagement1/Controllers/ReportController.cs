using LibraryManagement1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LibraryManagement1.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class ReportController : Controller
    {
        private readonly LibraryDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ReportController(LibraryDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Report/CategoryReport (Báo cáo mượn sách theo thể loại)
        public async Task<IActionResult> CategoryReport()
        {
            var totalBorrows = await _context.BorrowDetails.CountAsync();
            
            var categoryStats = await _context.BorrowDetails
                .Include(d => d.Book)
                .ThenInclude(b => b!.Category)
                .Where(d => d.Book != null && d.Book.Category != null)
                .GroupBy(d => d.Book!.Category!.Name)
                .Select(g => new CategoryReportRow
                {
                    CategoryName = g.Key,
                    BorrowCount = g.Count()
                })
                .ToListAsync();

            foreach (var item in categoryStats)
            {
                item.Percentage = totalBorrows > 0 ? Math.Round((double)item.BorrowCount * 100 / totalBorrows, 2) : 0;
            }

            ViewBag.TotalBorrows = totalBorrows;
            return View(categoryStats);
        }

        // GET: Report/OverdueReport (Báo cáo sách trễ hạn)
        public async Task<IActionResult> OverdueReport()
        {
            // Read settings for fine rate
            double finePerDay = 1000.0;
            var settingsPath = Path.Combine(_env.WebRootPath, "settings.json");
            if (System.IO.File.Exists(settingsPath))
            {
                try
                {
                    var json = System.IO.File.ReadAllText(settingsPath);
                    var settings = JsonSerializer.Deserialize<Dictionary<string, double>>(json);
                    if (settings != null && settings.ContainsKey("FinePerDay"))
                    {
                        finePerDay = settings["FinePerDay"];
                    }
                }
                catch { }
            }

            var today = DateTime.Today;
            var overdueItems = await _context.BorrowDetails
                .Include(d => d.Book)
                .Include(d => d.BorrowTicket)
                    .ThenInclude(t => t!.Reader)
                .Where(d => d.ReturnDate == null && d.BorrowTicket!.DueDate < today)
                .OrderBy(d => d.BorrowTicket!.DueDate)
                .Select(d => new OverdueReportRow
                {
                    BookCode = d.Book!.BookCode,
                    BookTitle = d.Book.Title,
                    ReaderName = d.BorrowTicket!.Reader!.Name,
                    ReaderCode = d.BorrowTicket.Reader.ReaderCode,
                    BorrowDate = d.BorrowTicket.BorrowDate,
                    DueDate = d.BorrowTicket.DueDate,
                    TicketCode = d.BorrowTicket.TicketCode
                })
                .ToListAsync();

            foreach (var item in overdueItems)
            {
                item.LateDays = (today - item.DueDate.Date).Days;
                item.EstimatedFine = (decimal)(item.LateDays * finePerDay);
            }

            return View(overdueItems);
        }
    }

    public class CategoryReportRow
    {
        public string CategoryName { get; set; } = string.Empty;
        public int BorrowCount { get; set; }
        public double Percentage { get; set; }
    }

    public class OverdueReportRow
    {
        public string BookCode { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public string ReaderName { get; set; } = string.Empty;
        public string ReaderCode { get; set; } = string.Empty;
        public string TicketCode { get; set; } = string.Empty;
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public int LateDays { get; set; }
        public decimal EstimatedFine { get; set; }
    }
}
