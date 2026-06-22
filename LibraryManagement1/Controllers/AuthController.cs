using System.Security.Claims;
using LibraryManagement1.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement1.Controllers
{
    public class AuthController : Controller
    {
        private readonly LibraryDbContext _context;

        public AuthController(LibraryDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                if (role == "Admin" || role == "Staff")
                {
                    return RedirectToAction("Dashboard", "Home");
                }
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.");
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không chính xác.");
                return View();
            }

            // Create user identity claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("FullName", user.FullName),
                new Claim("UserId", user.Id.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            if (user.Role == "Admin" || user.Role == "Staff")
            {
                return RedirectToAction("Dashboard", "Home");
            }
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                if (role == "Admin" || role == "Staff")
                {
                    return RedirectToAction("Dashboard", "Home");
                }
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string username, string fullName, string password, string confirmPassword)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ tất cả các trường.");
                return View();
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu xác nhận không khớp.");
                return View();
            }

            // Check if username exists
            var exists = await _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower());
            if (exists)
            {
                ModelState.AddModelError("", "Tên đăng nhập này đã tồn tại trong hệ thống.");
                return View();
            }

            var newUser = new User
            {
                Username = username,
                FullName = fullName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = "User" // Mặc định là User
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
