using LibraryManagement1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement1.Controllers
{
    [Authorize]
    public class BookController : Controller
    {
        private readonly LibraryDbContext _context;

        public BookController(LibraryDbContext context)
        {
            _context = context;
        }

        // GET: Book
        // Supports searchString, categoryId (Chức năng 11: Tra cứu / Tìm kiếm sách)
        public async Task<IActionResult> Index(string searchString, int? categoryId, int? authorId)
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

            if (authorId.HasValue)
            {
                query = query.Where(b => b.AuthorId == authorId.Value);
            }

            ViewData["Categories"] = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", categoryId);
            ViewData["Authors"] = new SelectList(await _context.Authors.ToListAsync(), "Id", "Name", authorId);
            ViewData["CurrentSearch"] = searchString;
            ViewData["CurrentCategory"] = categoryId;
            ViewData["CurrentAuthor"] = authorId;

            return View(await query.ToListAsync());
        }

        // GET: Book/Details/5
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var book = await _context.Books
                .Include(b => b.Category)
                .Include(b => b.Author)
                .Include(b => b.Publisher)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (book == null) return NotFound();

            // Fetch borrowing history / current active borrow details for this book
            var activeBorrows = await _context.BorrowDetails
                .Include(d => d.BorrowTicket)
                .ThenInclude(t => t!.Reader)
                .Where(d => d.BookId == id && d.ReturnDate == null)
                .ToListAsync();

            var pastBorrows = await _context.BorrowDetails
                .Include(d => d.BorrowTicket)
                .ThenInclude(t => t!.Reader)
                .Where(d => d.BookId == id && d.ReturnDate != null)
                .OrderByDescending(d => d.ReturnDate)
                .Take(10)
                .ToListAsync();

            ViewBag.ActiveBorrows = activeBorrows;
            ViewBag.PastBorrows = pastBorrows;

            return View(book);
        }

        // GET: Book/Create
        public async Task<IActionResult> Create()
        {
            ViewData["CategoryId"] = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
            ViewData["AuthorId"] = new SelectList(await _context.Authors.ToListAsync(), "Id", "Name");
            ViewData["PublisherId"] = new SelectList(await _context.Publishers.ToListAsync(), "Id", "Name");
            return View();
        }

        // POST: Book/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,BookCode,Title,CategoryId,AuthorId,PublisherId,PublishYear,Quantity,Price")] Book book)
        {
            if (ModelState.IsValid)
            {
                // Verify book code is unique
                var exists = await _context.Books.AnyAsync(b => b.BookCode == book.BookCode);
                if (exists)
                {
                    ModelState.AddModelError("BookCode", "Mã sách này đã tồn tại trong hệ thống.");
                }
                else
                {
                    book.AvailableQuantity = book.Quantity; // Initially, all books are available
                    _context.Add(book);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }

            ViewData["CategoryId"] = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", book.CategoryId);
            ViewData["AuthorId"] = new SelectList(await _context.Authors.ToListAsync(), "Id", "Name", book.AuthorId);
            ViewData["PublisherId"] = new SelectList(await _context.Publishers.ToListAsync(), "Id", "Name", book.PublisherId);
            return View(book);
        }

        // GET: Book/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();

            ViewData["CategoryId"] = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", book.CategoryId);
            ViewData["AuthorId"] = new SelectList(await _context.Authors.ToListAsync(), "Id", "Name", book.AuthorId);
            ViewData["PublisherId"] = new SelectList(await _context.Publishers.ToListAsync(), "Id", "Name", book.PublisherId);
            return View(book);
        }

        // POST: Book/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,BookCode,Title,CategoryId,AuthorId,PublisherId,PublishYear,Quantity,AvailableQuantity,Price")] Book book)
        {
            if (id != book.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Basic sanity check: AvailableQuantity cannot exceed Quantity
                    if (book.AvailableQuantity > book.Quantity)
                    {
                        book.AvailableQuantity = book.Quantity;
                    }
                    
                    _context.Update(book);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookExists(book.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["CategoryId"] = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", book.CategoryId);
            ViewData["AuthorId"] = new SelectList(await _context.Authors.ToListAsync(), "Id", "Name", book.AuthorId);
            ViewData["PublisherId"] = new SelectList(await _context.Publishers.ToListAsync(), "Id", "Name", book.PublisherId);
            return View(book);
        }

        // GET: Book/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var book = await _context.Books
                .Include(b => b.Category)
                .Include(b => b.Author)
                .Include(b => b.Publisher)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (book == null) return NotFound();

            return View(book);
        }

        // POST: Book/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book != null)
            {
                // Check if book has active borrow records
                var hasActiveBorrows = await _context.BorrowDetails
                    .AnyAsync(d => d.BookId == id && d.ReturnDate == null);
                if (hasActiveBorrows)
                {
                    ModelState.AddModelError("", "Không thể xóa cuốn sách này vì hiện có độc giả đang mượn.");
                    
                    var reloadedBook = await _context.Books
                        .Include(b => b.Category)
                        .Include(b => b.Author)
                        .Include(b => b.Publisher)
                        .FirstOrDefaultAsync(m => m.Id == id);
                    return View("Delete", reloadedBook);
                }

                _context.Books.Remove(book);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool BookExists(int id)
        {
            return _context.Books.Any(e => e.Id == id);
        }
    }
}
