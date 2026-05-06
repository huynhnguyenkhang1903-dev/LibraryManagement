using LibraryManagement.Data;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers
{
    [Authorize]
    public class BorrowRecordsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly LibraryManagement.Services.INotificationService _notificationService;

        public BorrowRecordsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, LibraryManagement.Services.INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // GET: BorrowRecords
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var query = _context.BorrowRecords
                .Include(b => b.Book)
                .Include(b => b.User)
                .AsQueryable();

            if (!User.IsInRole("Admin") && !User.IsInRole("Staff"))
            {
                // Readers only see their own records
                query = query.Where(b => b.UserId == user.Id);
            }

            return View(await query.OrderByDescending(b => b.BorrowDate).ToListAsync());
        }

        // GET: BorrowRecords/Create
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Create(int? bookId)
        {
            var readers = await _userManager.GetUsersInRoleAsync("User");
            ViewData["UserId"] = new SelectList(readers, "Id", "FullName");
            
            var books = await _context.Books.Where(b => b.StockQuantity > 0).ToListAsync();
            ViewData["BookId"] = new SelectList(books, "Id", "Title", bookId);

            return View(new BorrowRecord { DueDate = DateTime.Now.AddDays(14) });
        }

        // POST: BorrowRecords/Create
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BorrowRecord borrowRecord)
        {
            // Manual validation
            var book = await _context.Books.FindAsync(borrowRecord.BookId);
            if (book == null || book.StockQuantity <= 0)
            {
                ModelState.AddModelError("", "Sách này hiện đã hết trong kho.");
            }

            var activeBorrowsCount = await _context.BorrowRecords
                .CountAsync(b => b.UserId == borrowRecord.UserId && !b.IsReturned);
            
            if (activeBorrowsCount >= 5)
            {
                ModelState.AddModelError("", "Độc giả này đã mượn tối đa 5 cuốn sách.");
            }

            if (ModelState.IsValid)
            {
                borrowRecord.BorrowDate = DateTime.Now;
                borrowRecord.IsReturned = false;
                
                // Update stock
                book!.StockQuantity -= 1;
                
                _context.Add(borrowRecord);
                await _context.SaveChangesAsync();

                // Send notification
                await _notificationService.SendNotificationAsync(
                    borrowRecord.UserId, 
                    "Xác nhận mượn sách", 
                    $"Bạn đã mượn thành công cuốn sách '{book!.Title}'. Hạn trả là ngày {borrowRecord.DueDate.ToShortDateString()}.",
                    Url.Action("Index", "BorrowRecords")
                );

                return RedirectToAction(nameof(Index));
            }

            var readers = await _userManager.GetUsersInRoleAsync("User");
            ViewData["UserId"] = new SelectList(readers, "Id", "FullName", borrowRecord.UserId);
            ViewData["BookId"] = new SelectList(_context.Books.Where(b => b.StockQuantity > 0), "Id", "Title", borrowRecord.BookId);
            return View(borrowRecord);
        }

        // POST: BorrowRecords/Return/5
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Return(int id)
        {
            var record = await _context.BorrowRecords.Include(b => b.Book).FirstOrDefaultAsync(b => b.Id == id);
            if (record == null || record.IsReturned) return NotFound();

            record.IsReturned = true;
            record.ReturnDate = DateTime.Now;
            
            // Calculate final fine
            record.FineAmount = record.CurrentFine;
            
            if (record.Book != null)
            {
                record.Book.StockQuantity += 1;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: BorrowRecords/Renew/5
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Renew(int id)
        {
            var record = await _context.BorrowRecords.FindAsync(id);
            if (record == null || record.IsReturned) return NotFound();

            // Renew for another 7 days from current DueDate
            record.DueDate = record.DueDate.AddDays(7);
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: BorrowRecords/MyHistory
        public async Task<IActionResult> MyHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var history = await _context.BorrowRecords
                .Include(b => b.Book)
                .Where(b => b.UserId == user.Id)
                .OrderByDescending(b => b.BorrowDate)
                .ToListAsync();

            return View(history);
        }
    }
}
