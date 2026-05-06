using LibraryManagement.Data;
using LibraryManagement.Models;

namespace LibraryManagement.Services
{
    public interface INotificationService
    {
        Task SendNotificationAsync(string userId, string title, string message, string? link = null);
        Task SendEmailAsync(string email, string subject, string body);
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task SendNotificationAsync(string userId, string title, string message, string? link = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Link = link,
                CreatedAt = DateTime.Now
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            
            // Also trigger "email" sending
            // In a real app, you would fetch user email and call SendEmailAsync
        }

        public async Task SendEmailAsync(string email, string subject, string body)
        {
            // Simulate email sending logic
            // In a real app, use SmtpClient or SendGrid
            Console.WriteLine($"[EMAIL SENT to {email}]: {subject}");
            await Task.CompletedTask;
        }
    }
}
