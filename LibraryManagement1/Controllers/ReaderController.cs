using LibraryManagement1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement1.Controllers
{
    [Authorize]
    public class ReaderController : Controller
    {
        private readonly LibraryDbContext _context;

        public ReaderController(LibraryDbContext context)
        {
            _context = context;
        }

        // GET: Reader
        public async Task<IActionResult> Index(string searchString)
        {
            var query = _context.Readers.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                var lowerSearch = searchString.ToLower();
                query = query.Where(r => 
                    r.ReaderCode.ToLower().Contains(lowerSearch) || 
                    r.Name.ToLower().Contains(lowerSearch) || 
                    r.Phone.Contains(lowerSearch) || 
                    (r.Email != null && r.Email.ToLower().Contains(lowerSearch)));
            }

            ViewData["CurrentSearch"] = searchString;
            return View(await query.ToListAsync());
        }

        // GET: Reader/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Reader/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ReaderCode,Name,BirthDate,Address,Phone,Email,ExpiryDate")] Reader reader)
        {
            if (ModelState.IsValid)
            {
                // Verify reader code is unique
                var exists = await _context.Readers.AnyAsync(r => r.ReaderCode == reader.ReaderCode);
                if (exists)
                {
                    ModelState.AddModelError("ReaderCode", "Mã độc giả này đã tồn tại.");
                }
                else
                {
                    reader.CreatedDate = DateTime.Now;
                    _context.Add(reader);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            return View(reader);
        }

        // GET: Reader/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var reader = await _context.Readers.FindAsync(id);
            if (reader == null) return NotFound();

            return View(reader);
        }

        // POST: Reader/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ReaderCode,Name,BirthDate,Address,Phone,Email,CreatedDate,ExpiryDate")] Reader reader)
        {
            if (id != reader.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(reader);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReaderExists(reader.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(reader);
        }

        // GET: Reader/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var reader = await _context.Readers.FirstOrDefaultAsync(m => m.Id == id);
            if (reader == null) return NotFound();

            return View(reader);
        }

        // POST: Reader/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reader = await _context.Readers.FindAsync(id);
            if (reader != null)
            {
                // Verify if reader has active borrow tickets
                var hasActiveTickets = await _context.BorrowTickets
                    .AnyAsync(t => t.ReaderId == id && t.Status != "Returned");
                if (hasActiveTickets)
                {
                    ModelState.AddModelError("", "Không thể xóa độc giả này vì vẫn còn phiếu mượn chưa hoàn thành trả sách.");
                    return View("Delete", reader);
                }

                _context.Readers.Remove(reader);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ReaderExists(int id)
        {
            return _context.Readers.Any(e => e.Id == id);
        }
    }
}
