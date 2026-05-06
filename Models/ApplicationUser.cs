using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(200)]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [Display(Name = "Số thẻ thư viện")]
        public string? LibraryCardNumber { get; set; }

        [Display(Name = "Ngày cấp thẻ")]
        public DateTime? CardIssueDate { get; set; }

        [Display(Name = "Ngày hết hạn thẻ")]
        public DateTime? CardExpiryDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
