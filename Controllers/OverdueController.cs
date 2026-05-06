using LibraryManagement.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class OverdueController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly LibraryManagement.Services.INotificationService _notificationService;

        public OverdueController(ApplicationDbContext context, LibraryManagement.Services.INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            var overdueRecords = await _context.BorrowRecords
                .Include(b => b.Book)
                .Include(b => b.User)
                .Where(b => !b.IsReturned && b.DueDate < DateTime.Now)
                .ToListAsync();

            return View(overdueRecords);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendReminder(int id)
        {
            var record = await _context.BorrowRecords
                .Include(b => b.User)
                .Include(b => b.Book)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (record == null) return NotFound();

            // Send notification
            await _notificationService.SendNotificationAsync(
                record.UserId,
                "Nhắc trả sách quá hạn",
                $"Sách '{record.Book?.Title}' của bạn đã quá hạn {record.OverdueDays} ngày. Vui lòng hoàn trả sớm nhất có thể.",
                Url.Action("Index", "BorrowRecords")
            );

            TempData["ReminderStatus"] = $"Đã gửi thông báo nhắc nhở tới độc giả {record.User?.FullName} cho cuốn sách '{record.Book?.Title}'.";
            
            return RedirectToAction(nameof(Index));
        }
    }
}
