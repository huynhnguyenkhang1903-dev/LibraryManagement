using LibraryManagement1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement1.Controllers
{
    [Authorize]
    public class BorrowController : Controller
    {
        private readonly LibraryDbContext _context;
        private const decimal FINE_PER_DAY = 5000; // 5,000 VNĐ / day late

        public BorrowController(LibraryDbContext context)
        {
            _context = context;
        }

        // GET: Borrow
        public async Task<IActionResult> Index(string searchString, string statusFilter)
        {
            var query = _context.BorrowTickets
                .Include(t => t.Reader)
                .Include(t => t.BorrowDetails)
                .AsQueryable();

            // Refresh status for active tickets to reflect if they became overdue
            var activeTickets = await _context.BorrowTickets
                .Where(t => t.Status == "Borrowing" && t.DueDate < DateTime.Now)
                .ToListAsync();
            
            if (activeTickets.Any())
            {
                foreach (var ticket in activeTickets)
                {
                    ticket.Status = "Overdue";
                }
                await _context.SaveChangesAsync();
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                var lowerSearch = searchString.ToLower();
                query = query.Where(t => 
                    t.TicketCode.ToLower().Contains(lowerSearch) || 
                    t.Reader!.Name.ToLower().Contains(lowerSearch) ||
                    t.Reader.ReaderCode.ToLower().Contains(lowerSearch));
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                query = query.Where(t => t.Status == statusFilter);
            }

            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentStatus"] = statusFilter;

            return View(await query.OrderByDescending(t => t.BorrowDate).ToListAsync());
        }

        // GET: Borrow/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.BorrowTickets
                .Include(t => t.Reader)
                .Include(t => t.CreatedByUser)
                .Include(t => t.BorrowDetails)
                    .ThenInclude(d => d.Book)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ticket == null) return NotFound();

            return View(ticket);
        }

        // GET: Borrow/Create
        public async Task<IActionResult> Create()
        {
            // Populate drop-down list of readers whose cards are not expired
            var activeReaders = await _context.Readers
                .Where(r => r.ExpiryDate >= DateTime.Now)
                .ToListAsync();
            
            // Populate list of books with available copies
            var availableBooks = await _context.Books
                .Where(b => b.AvailableQuantity > 0)
                .ToListAsync();

            ViewData["ReaderId"] = new SelectList(activeReaders, "Id", "Name");
            ViewData["Books"] = availableBooks;

            return View();
        }

        // POST: Borrow/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int readerId, int[]? selectedBookIds, DateTime dueDate)
        {
            selectedBookIds ??= Array.Empty<int>();

            var reader = await _context.Readers
                .Include(r => r.BorrowTickets)
                    .ThenInclude(t => t.BorrowDetails)
                .FirstOrDefaultAsync(r => r.Id == readerId);

            if (reader == null)
            {
                ModelState.AddModelError("", "Không tìm thấy độc giả.");
                return await RebuildCreateView(readerId, selectedBookIds);
            }

            // 1. Check card expiry
            if (reader.ExpiryDate < DateTime.Now)
            {
                ModelState.AddModelError("", $"Thẻ của độc giả {reader.Name} đã hết hạn vào ngày {reader.ExpiryDate:dd/MM/yyyy}. Không thể mượn sách.");
                return await RebuildCreateView(readerId, selectedBookIds);
            }

            // 2. Check if currently borrows > 5 books
            var activeBorrowedCount = reader.BorrowTickets
                .Where(t => t.Status != "Returned")
                .SelectMany(t => t.BorrowDetails)
                .Count(d => d.ReturnDate == null);

            if (activeBorrowedCount + selectedBookIds.Length > 5)
            {
                ModelState.AddModelError("", $"Độc giả đang mượn {activeBorrowedCount} cuốn sách. Tổng số lượng mượn tối đa là 5 cuốn. Đã chọn thêm {selectedBookIds.Length} cuốn.");
                return await RebuildCreateView(readerId, selectedBookIds);
            }

            // 3. Check if reader has overdue tickets
            var hasOverdue = await _context.BorrowTickets
                .AnyAsync(t => t.ReaderId == readerId && t.Status == "Overdue");
            if (hasOverdue)
            {
                ModelState.AddModelError("", "Độc giả này có phiếu mượn quá hạn chưa trả. Phải trả hết sách quá hạn trước khi mượn mới.");
                return await RebuildCreateView(readerId, selectedBookIds);
            }

            if (selectedBookIds == null || selectedBookIds.Length == 0)
            {
                ModelState.AddModelError("", "Vui lòng chọn ít nhất 1 cuốn sách muốn mượn.");
                return await RebuildCreateView(readerId, selectedBookIds);
            }

            // Generate TicketCode
            var ticketCount = await _context.BorrowTickets.CountAsync();
            var ticketCode = $"PM{DateTime.Now:yyyyMMdd}-{ticketCount + 1:D3}";

            // Fetch user ID
            var userIdStr = User.FindFirst("UserId")?.Value;
            int? userId = string.IsNullOrEmpty(userIdStr) ? null : int.Parse(userIdStr);

            var ticket = new BorrowTicket
                {
                    TicketCode = ticketCode,
                    ReaderId = readerId,
                    BorrowDate = DateTime.Now,
                    DueDate = dueDate,
                    Status = dueDate < DateTime.Now ? "Overdue" : "Borrowing",
                    CreatedByUserId = userId
                };

            _context.BorrowTickets.Add(ticket);
            await _context.SaveChangesAsync(); // Save to generate ticket.Id

            foreach (var bookId in selectedBookIds)
            {
                var book = await _context.Books.FindAsync(bookId);
                if (book != null && book.AvailableQuantity > 0)
                {
                    book.AvailableQuantity--; // Decrement inventory
                    
                    var detail = new BorrowDetail
                    {
                        BorrowTicketId = ticket.Id,
                        BookId = bookId,
                        ReturnDate = null,
                        FineAmount = 0
                    };
                    _context.BorrowDetails.Add(detail);
                }
                else
                {
                    // Book is out of stock (race condition check)
                    ModelState.AddModelError("", $"Sách '{book?.Title}' đã hết hàng khả dụng.");
                    // Rollback and reload
                    return await RebuildCreateView(readerId, selectedBookIds);
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: Borrow/ReturnBook/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnBook(int detailId)
        {
            var detail = await _context.BorrowDetails
                .Include(d => d.BorrowTicket)
                .Include(d => d.Book)
                .FirstOrDefaultAsync(d => d.Id == detailId);

            if (detail == null) return NotFound();
            if (detail.ReturnDate != null)
            {
                return RedirectToAction(nameof(Details), new { id = detail.BorrowTicketId });
            }

            var returnDate = DateTime.Now;
            detail.ReturnDate = returnDate;

            // Calculate Fine if overdue
            if (returnDate > detail.BorrowTicket!.DueDate)
            {
                var lateDays = (returnDate.Date - detail.BorrowTicket.DueDate.Date).Days;
                if (lateDays > 0)
                {
                    detail.FineAmount = lateDays * FINE_PER_DAY;
                }
            }

            // Return book to inventory
            if (detail.Book != null)
            {
                detail.Book.AvailableQuantity++;
                if (detail.Book.AvailableQuantity > detail.Book.Quantity)
                {
                    detail.Book.AvailableQuantity = detail.Book.Quantity;
                }
            }

            await _context.SaveChangesAsync();

            // Check if all books in this ticket are returned
            var allReturned = await _context.BorrowDetails
                .AllAsync(d => d.BorrowTicketId == detail.BorrowTicketId && d.ReturnDate != null);

            if (allReturned)
            {
                detail.BorrowTicket.Status = "Returned";
            }
            else
            {
                // If some returned but overdue remains, keep check, otherwise borrowing
                var hasOverdueUnreturned = await _context.BorrowDetails
                    .AnyAsync(d => d.BorrowTicketId == detail.BorrowTicketId && d.ReturnDate == null && detail.BorrowTicket.DueDate < DateTime.Now);
                
                detail.BorrowTicket.Status = hasOverdueUnreturned ? "Overdue" : "Borrowing";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = detail.BorrowTicketId });
        }

        private async Task<IActionResult> RebuildCreateView(int readerId, int[]? selectedBookIds)
        {
            var activeReaders = await _context.Readers
                .Where(r => r.ExpiryDate >= DateTime.Now)
                .ToListAsync();

            var availableBooks = await _context.Books
                .Where(b => b.AvailableQuantity > 0)
                .ToListAsync();

            ViewData["ReaderId"] = new SelectList(activeReaders, "Id", "Name", readerId);
            ViewData["Books"] = availableBooks;
            ViewData["SelectedBookIds"] = selectedBookIds;

            return View();
        }
    }
}
