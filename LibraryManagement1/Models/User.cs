using System.ComponentModel.DataAnnotations;

namespace LibraryManagement1.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "Staff"; // Admin, Staff
    }
}
