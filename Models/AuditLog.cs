using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Required]
        public string Action { get; set; } = string.Empty;

        [Required]
        public string EntityName { get; set; } = string.Empty;

        public string? EntityId { get; set; }

        public string? Details { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? IpAddress { get; set; }
    }
}
