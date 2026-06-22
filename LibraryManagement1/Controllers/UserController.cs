using LibraryManagement1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly LibraryDbContext _context;

        public UserController(LibraryDbContext context)
        {
            _context = context;
        }

        // GET: User
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // GET: User/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Username,PasswordHash,FullName,Role")] User user, string clearPassword)
        {
            if (string.IsNullOrEmpty(clearPassword))
            {
                ModelState.AddModelError("PasswordHash", "Mật khẩu là bắt buộc");
            }

            if (ModelState.IsValid)
            {
                // Check if username exists
                var exists = await _context.Users.AnyAsync(u => u.Username.ToLower() == user.Username.ToLower());
                if (exists)
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập này đã tồn tại.");
                    return View(user);
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(clearPassword);
                _context.Add(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã tạo tài khoản {user.Username} thành công.";
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: User/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Clear password hash so it doesn't show up in the view
            user.PasswordHash = string.Empty;
            return View(user);
        }

        // POST: User/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Username,FullName,Role")] User user, string? newPassword)
        {
            if (id != user.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _context.Users.FindAsync(id);
                    if (existingUser == null) return NotFound();

                    existingUser.FullName = user.FullName;
                    existingUser.Role = user.Role;

                    // If a new password is provided, re-hash it and update
                    if (!string.IsNullOrEmpty(newPassword))
                    {
                        existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                    }

                    _context.Update(existingUser);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Đã cập nhật tài khoản {user.Username} thành công.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: User/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FirstOrDefaultAsync(m => m.Id == id);
            if (user == null) return NotFound();

            return View(user);
        }

        // POST: User/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                var loggedInUsername = User.Identity?.Name;
                if (user.Username.Equals(loggedInUsername, StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError("", "Bạn không thể tự xóa tài khoản của chính mình.");
                    return View("Delete", user);
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã xóa tài khoản {user.Username} thành công.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}
