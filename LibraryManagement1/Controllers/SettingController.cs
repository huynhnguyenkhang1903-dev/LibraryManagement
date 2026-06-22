using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace LibraryManagement1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SettingController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public SettingController(IWebHostEnvironment env)
        {
            _env = env;
        }

        private string GetSettingsPath()
        {
            return Path.Combine(_env.WebRootPath, "settings.json");
        }

        // GET: Setting
        public IActionResult Index()
        {
            var settingsPath = GetSettingsPath();
            var settings = new LibrarySettings();

            if (System.IO.File.Exists(settingsPath))
            {
                try
                {
                    var json = System.IO.File.ReadAllText(settingsPath);
                    var deserialized = JsonSerializer.Deserialize<LibrarySettings>(json);
                    if (deserialized != null)
                    {
                        settings = deserialized;
                    }
                }
                catch { }
            }
            else
            {
                // Create default if it doesn't exist
                SaveSettingsToFile(settings);
            }

            return View(settings);
        }

        // POST: Setting/Save
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save(LibrarySettings settings)
        {
            if (ModelState.IsValid)
            {
                if (settings.CardExpiryMonths <= 0 || settings.MaxBorrowDays <= 0 || settings.MaxBooks <= 0 || settings.FinePerDay < 0)
                {
                    ModelState.AddModelError("", "Các quy định phải là số lớn hơn 0 và mức phạt không được âm.");
                    return View("Index", settings);
                }

                SaveSettingsToFile(settings);
                TempData["SuccessMessage"] = "Đã cập nhật quy định thư viện thành công.";
                return RedirectToAction(nameof(Index));
            }

            return View("Index", settings);
        }

        private void SaveSettingsToFile(LibrarySettings settings)
        {
            var settingsPath = GetSettingsPath();
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(settings, options);
            System.IO.File.WriteAllText(settingsPath, json);
        }
    }

    public class LibrarySettings
    {
        public int CardExpiryMonths { get; set; } = 6;
        public int MaxBorrowDays { get; set; } = 14;
        public int MaxBooks { get; set; } = 5;
        public double FinePerDay { get; set; } = 1000.0;
    }
}
