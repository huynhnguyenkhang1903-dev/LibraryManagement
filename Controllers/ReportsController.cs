using ClosedXML.Excel;
using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace LibraryManagement.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel
            {
                TotalBooks = await _context.Books.CountAsync(),
                TotalReaders = (await _userManager.GetUsersInRoleAsync("User")).Count,
                TotalBorrows = await _context.BorrowRecords.CountAsync(),
                ActiveBorrows = await _context.BorrowRecords.CountAsync(b => !b.IsReturned),
                OverdueBorrows = await _context.BorrowRecords.CountAsync(b => !b.IsReturned && b.DueDate < DateTime.Now)
            };

            // Top borrowed books
            model.TopBorrowedBooks = await _context.BorrowRecords
                .GroupBy(b => b.Book!.Title)
                .Select(g => new BookStats { Title = g.Key, BorrowCount = g.Count() })
                .OrderByDescending(x => x.BorrowCount)
                .Take(5)
                .ToListAsync();

            // Most active readers
            model.MostActiveReaders = await _context.BorrowRecords
                .GroupBy(b => b.User!.FullName)
                .Select(g => new ReaderStats { FullName = g.Key, BorrowCount = g.Count() })
                .OrderByDescending(x => x.BorrowCount)
                .Take(5)
                .ToListAsync();

            // Borrowing trends (last 6 months)
            var sixMonthsAgo = DateTime.Now.AddMonths(-6);
            model.BorrowingTrends = await _context.BorrowRecords
                .Where(b => b.BorrowDate >= sixMonthsAgo)
                .GroupBy(b => new { b.BorrowDate.Year, b.BorrowDate.Month })
                .Select(g => new MonthlyTrend 
                { 
                    Month = $"{g.Key.Month}/{g.Key.Year}", 
                    Count = g.Count() 
                })
                .ToListAsync();

            return View(model);
        }

        public async Task<IActionResult> ExportToExcel()
        {
            var records = await _context.BorrowRecords
                .Include(b => b.Book)
                .Include(b => b.User)
                .OrderByDescending(b => b.BorrowDate)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("BorrowRecords");
                var currentRow = 1;

                // Headers
                worksheet.Cell(currentRow, 1).Value = "ID";
                worksheet.Cell(currentRow, 2).Value = "Sách";
                worksheet.Cell(currentRow, 3).Value = "Độc giả";
                worksheet.Cell(currentRow, 4).Value = "Ngày mượn";
                worksheet.Cell(currentRow, 5).Value = "Hạn trả";
                worksheet.Cell(currentRow, 6).Value = "Trạng thái";
                worksheet.Cell(currentRow, 7).Value = "Tiền phạt";

                // Styling headers
                var headerRange = worksheet.Range(1, 1, 1, 7);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

                // Data
                foreach (var record in records)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = record.Id;
                    worksheet.Cell(currentRow, 2).Value = record.Book?.Title;
                    worksheet.Cell(currentRow, 3).Value = record.User?.FullName;
                    worksheet.Cell(currentRow, 4).Value = record.BorrowDate;
                    worksheet.Cell(currentRow, 5).Value = record.DueDate;
                    worksheet.Cell(currentRow, 6).Value = record.Status;
                    worksheet.Cell(currentRow, 7).Value = record.FineAmount;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BaoCaoMuonTra_{DateTime.Now:yyyyMMdd}.xlsx");
                }
            }
        }
    }
}
