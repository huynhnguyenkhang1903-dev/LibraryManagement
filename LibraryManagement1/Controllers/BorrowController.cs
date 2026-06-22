using LibraryManagement1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LibraryManagement1.Controllers
{
    [Authorize]
    public class BorrowController : Controller
    {
        private readonly LibraryDbContext _context;
        private readonly IWebHostEnvironment _env;

        public BorrowController(LibraryDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private LibrarySettings GetSettings()
        {
            var settingsPath = Path.Combine(_env.WebRootPath, "settings.json");
            if (System.IO.File.Exists(settingsPath))
            {
                try
                {
                    var json = System.IO.File.ReadAllText(settingsPath);
                    var settings = JsonSerializer.Deserialize<LibrarySettings>(json);
                    if (settings != null) return settings;
                }
                catch { }
            }
            return new LibrarySettings(); // default fallback
        }

        // GET: Borrow
        [Authorize(Roles = "Admin,Staff")]
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

            // If not admin/staff, ensure they can only see their own ticket
            if (!User.IsInRole("Admin") && !User.IsInRole("Staff"))
            {
                var username = User.Identity?.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null || ticket.Reader?.Name.ToLower() != user.FullName.ToLower())
                {
                    return RedirectToAction("AccessDenied", "Auth");
                }
            }

            return View(ticket);
        }

        // GET: Borrow/Create
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Create()
        {
            var settings = GetSettings();
            ViewBag.MaxBooks = settings.MaxBooks;
            ViewBag.MaxBorrowDays = settings.MaxBorrowDays;

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
        [Authorize(Roles = "Admin,Staff")]
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

            // 2. Check if currently borrows > MaxBooks
            var settings = GetSettings();
            var activeBorrowedCount = reader.BorrowTickets
                .Where(t => t.Status != "Returned")
                .SelectMany(t => t.BorrowDetails)
                .Count(d => d.ReturnDate == null);

            if (activeBorrowedCount + selectedBookIds.Length > settings.MaxBooks)
            {
                ModelState.AddModelError("", $"Độc giả đang mượn {activeBorrowedCount} cuốn sách. Tổng số lượng mượn tối đa là {settings.MaxBooks} cuốn. Đã chọn thêm {selectedBookIds.Length} cuốn.");
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
        [Authorize(Roles = "Admin,Staff")]
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
                    var settings = GetSettings();
                    detail.FineAmount = lateDays * (decimal)settings.FinePerDay;
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
            var settings = GetSettings();
            ViewBag.MaxBooks = settings.MaxBooks;
            ViewBag.MaxBorrowDays = settings.MaxBorrowDays;

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

        // GET: Borrow/CustomerBorrow?bookId=5
        [HttpGet]
        public async Task<IActionResult> CustomerBorrow(int bookId)
        {
            var book = await _context.Books
                .Include(b => b.Category)
                .Include(b => b.Author)
                .Include(b => b.Publisher)
                .FirstOrDefaultAsync(b => b.Id == bookId);

            if (book == null) return NotFound();
            if (book.AvailableQuantity <= 0)
            {
                TempData["ErrorMessage"] = "Sách hiện tại đã hết bản copy khả dụng.";
                return RedirectToAction("Details", "Book", new { id = bookId });
            }

            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return RedirectToAction("Login", "Auth");

            // Check if reader exists for this user (matching by FullName)
            var reader = await _context.Readers
                .FirstOrDefaultAsync(r => r.Name.ToLower() == user.FullName.ToLower());

            var availableBooks = await _context.Books
                .Where(b => b.AvailableQuantity > 0)
                .ToListAsync();

            ViewBag.Book = book;
            ViewBag.User = user;
            ViewBag.Reader = reader;
            ViewBag.Settings = GetSettings();
            ViewBag.AvailableBooks = availableBooks;

            return View();
        }

        // POST: Borrow/CustomerBorrow
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CustomerBorrow(int[] selectedBookIds, string? phone, DateTime? birthDate, DateTime borrowDate, DateTime dueDate)
        {
            if (selectedBookIds == null || selectedBookIds.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một cuốn sách để mượn.";
                return RedirectToAction("Index", "Home");
            }

            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return RedirectToAction("Login", "Auth");

            var activeSettings = GetSettings();

            // Look up reader by Name
            var reader = await _context.Readers
                .Include(r => r.BorrowTickets)
                    .ThenInclude(t => t.BorrowDetails)
                .FirstOrDefaultAsync(r => r.Name.ToLower() == user.FullName.ToLower());

            if (reader == null)
            {
                // Must activate library card (create reader)
                if (string.IsNullOrEmpty(phone) || !birthDate.HasValue)
                {
                    ModelState.AddModelError("", "Vui lòng nhập đầy đủ Số điện thoại và Ngày sinh để kích hoạt thẻ thư viện.");
                    
                    var firstBook = await _context.Books.FindAsync(selectedBookIds[0]);
                    ViewBag.Book = firstBook;
                    ViewBag.User = user;
                    ViewBag.Reader = null;
                    ViewBag.Settings = activeSettings;
                    ViewBag.AvailableBooks = await _context.Books.Where(b => b.AvailableQuantity > 0).ToListAsync();
                    ViewBag.SelectedBookIds = selectedBookIds;
                    ViewBag.Phone = phone;
                    ViewBag.BirthDate = birthDate?.ToString("yyyy-MM-dd");
                    return View();
                }

                // Create reader
                var readerCount = await _context.Readers.CountAsync();
                reader = new Reader
                {
                    ReaderCode = $"DG{readerCount + 1:D3}",
                    Name = user.FullName,
                    BirthDate = birthDate.Value,
                    Phone = phone,
                    CreatedDate = DateTime.Now,
                    ExpiryDate = DateTime.Now.AddMonths(activeSettings.CardExpiryMonths)
                };

                _context.Readers.Add(reader);
                await _context.SaveChangesAsync();
            }

            // Perform borrowing validation

            // 1. Check card expiry
            if (reader.ExpiryDate < DateTime.Now)
            {
                ModelState.AddModelError("", $"Thẻ thư viện của bạn đã hết hạn vào ngày {reader.ExpiryDate:dd/MM/yyyy}. Vui lòng liên hệ thủ thư để gia hạn.");
            }

            // 2. Validate dates
            if (dueDate < borrowDate)
            {
                ModelState.AddModelError("", "Ngày hẹn trả không thể trước ngày mượn.");
            }

            var borrowDays = (dueDate.Date - borrowDate.Date).Days;
            if (borrowDays <= 0)
            {
                ModelState.AddModelError("", "Thời gian mượn sách phải từ 1 ngày trở lên.");
            }
            else if (borrowDays > activeSettings.MaxBorrowDays)
            {
                ModelState.AddModelError("", $"Thời gian mượn tối đa là {activeSettings.MaxBorrowDays} ngày. Bạn đã chọn mượn {borrowDays} ngày.");
            }

            // 3. Check if currently borrows + selected > MaxBooks
            var activeBorrowedCount = reader.BorrowTickets
                .Where(t => t.Status != "Returned")
                .SelectMany(t => t.BorrowDetails)
                .Count(d => d.ReturnDate == null);

            if (activeBorrowedCount + selectedBookIds.Length > activeSettings.MaxBooks)
            {
                ModelState.AddModelError("", $"Bạn đang mượn {activeBorrowedCount} cuốn. Thêm {selectedBookIds.Length} cuốn sẽ vượt hạn mức mượn tối đa là {activeSettings.MaxBooks} cuốn.");
            }

            // 4. Check if reader has overdue tickets
            var hasOverdue = reader.BorrowTickets.Any(t => t.Status == "Overdue");
            if (hasOverdue)
            {
                ModelState.AddModelError("", "Bạn có phiếu mượn quá hạn chưa trả. Vui lòng trả hết sách quá hạn trước khi mượn mới.");
            }

            if (!ModelState.IsValid)
            {
                var firstBook = await _context.Books.FindAsync(selectedBookIds[0]);
                ViewBag.Book = firstBook;
                ViewBag.User = user;
                ViewBag.Reader = reader;
                ViewBag.Settings = activeSettings;
                ViewBag.AvailableBooks = await _context.Books.Where(b => b.AvailableQuantity > 0).ToListAsync();
                ViewBag.SelectedBookIds = selectedBookIds;
                return View();
            }

            // Generate TicketCode
            var ticketCount = await _context.BorrowTickets.CountAsync();
            var ticketCode = $"PM{DateTime.Now:yyyyMMdd}-{ticketCount + 1:D3}";

            var ticket = new BorrowTicket
            {
                TicketCode = ticketCode,
                ReaderId = reader.Id,
                BorrowDate = borrowDate,
                DueDate = dueDate,
                Status = "Borrowing",
                CreatedByUserId = user.Id
            };

            _context.BorrowTickets.Add(ticket);
            await _context.SaveChangesAsync();

            var bookTitles = new List<string>();
            foreach (var bId in selectedBookIds)
            {
                var targetBook = await _context.Books.FindAsync(bId);
                if (targetBook != null && targetBook.AvailableQuantity > 0)
                {
                    targetBook.AvailableQuantity--;
                    bookTitles.Add(targetBook.Title);

                    var detail = new BorrowDetail
                    {
                        BorrowTicketId = ticket.Id,
                        BookId = bId,
                        ReturnDate = null,
                        FineAmount = 0
                    };
                    _context.BorrowDetails.Add(detail);
                }
            }
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đăng ký mượn sách thành công! Đã mượn {selectedBookIds.Length} cuốn: {string.Join(", ", bookTitles)}. Hạn trả: {dueDate:dd/MM/yyyy}.";
            return RedirectToAction("Details", new { id = ticket.Id });
        }

        // GET: Borrow/History
        [HttpGet]
        public async Task<IActionResult> History()
        {
            var username = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return RedirectToAction("Login", "Auth");

            // Find reader card associated with this user
            var reader = await _context.Readers.FirstOrDefaultAsync(r => r.Name.ToLower() == user.FullName.ToLower());

            var tickets = new List<BorrowTicket>();
            if (reader != null)
            {
                tickets = await _context.BorrowTickets
                    .Include(t => t.BorrowDetails)
                        .ThenInclude(d => d.Book)
                    .Where(t => t.ReaderId == reader.Id)
                    .OrderByDescending(t => t.BorrowDate)
                    .ToListAsync();
            }

            return View(tickets);
        }
    }
}
