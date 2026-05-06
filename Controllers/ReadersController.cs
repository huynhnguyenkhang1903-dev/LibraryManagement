using LibraryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class ReadersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ReadersController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // GET: Readers
        public async Task<IActionResult> Index(string? searchTerm)
        {
            var readers = await _userManager.GetUsersInRoleAsync("User");
            var query = readers.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u => u.FullName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || 
                                         u.Email!.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                         (u.LibraryCardNumber != null && u.LibraryCardNumber.Contains(searchTerm)));
            }

            return View(query.OrderByDescending(u => u.CreatedAt).ToList());
        }

        // GET: Readers/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();

            var reader = await _userManager.FindByIdAsync(id);
            if (reader == null) return NotFound();

            return View(reader);
        }

        // GET: Readers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Readers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ApplicationUser model, string password)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    Address = model.Address,
                    LibraryCardNumber = model.LibraryCardNumber,
                    CardIssueDate = model.CardIssueDate,
                    CardExpiryDate = model.CardExpiryDate,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "User");
                    return RedirectToAction(nameof(Index));
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        // GET: Readers/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            var reader = await _userManager.FindByIdAsync(id);
            if (reader == null) return NotFound();

            return View(reader);
        }

        // POST: Readers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, ApplicationUser model)
        {
            if (id != model.Id) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.FullName = model.FullName;
            user.Address = model.Address;
            user.LibraryCardNumber = model.LibraryCardNumber;
            user.CardIssueDate = model.CardIssueDate;
            user.CardExpiryDate = model.CardExpiryDate;
            user.PhoneNumber = model.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(Index));
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        // POST: Readers/ToggleLock/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (await _userManager.IsLockedOutAsync(user))
            {
                // Unlock
                await _userManager.SetLockoutEndDateAsync(user, null);
            }
            else
            {
                // Lock indefinitely (100 years)
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Readers/IssueCard/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IssueCard(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Generate a simple card number: LIB + Year + random 4 digits
            user.LibraryCardNumber = "LIB" + DateTime.Now.Year + new Random().Next(1000, 9999);
            user.CardIssueDate = DateTime.Now;
            user.CardExpiryDate = DateTime.Now.AddYears(1);

            await _userManager.UpdateAsync(user);
            return RedirectToAction(nameof(Index));
        }

        // POST: Readers/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
