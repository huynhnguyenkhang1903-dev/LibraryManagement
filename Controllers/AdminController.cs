using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public AdminController(
            UserManager<ApplicationUser> userManager, 
            RoleManager<IdentityRole> roleManager, 
            ApplicationDbContext context,
            IAuditService auditService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _auditService = auditService;
        }

        public IActionResult Index()
        {
            return View();
        }

        // --- User Management ---
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeRole(string userId, string newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, newRole);

            await _auditService.LogActionAsync("Change Role", "User", userId, $"Changed role to {newRole}");
            
            return RedirectToAction(nameof(Users));
        }

        // --- Audit Logs ---
        public async Task<IActionResult> Logs()
        {
            var logs = await _context.AuditLogs
                .Include(l => l.User)
                .OrderByDescending(l => l.CreatedAt)
                .Take(200)
                .ToListAsync();
            return View(logs);
        }

        // --- Backup & Restore ---
        public async Task<IActionResult> Backup()
        {
            var data = new
            {
                Books = await _context.Books.ToListAsync(),
                Categories = await _context.Categories.ToListAsync(),
                BorrowRecords = await _context.BorrowRecords.ToListAsync(),
                Users = await _userManager.Users.Select(u => new { u.Id, u.FullName, u.Email, u.LibraryCardNumber }).ToListAsync(),
                ExportDate = DateTime.Now
            };

            var json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            
            await _auditService.LogActionAsync("Database Backup", "System", null, "Generated JSON backup file");

            return File(bytes, "application/json", $"LibraryFullBackup_{DateTime.Now:yyyyMMdd}.json");
        }
    }
}
