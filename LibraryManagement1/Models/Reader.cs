using System.ComponentModel.DataAnnotations;

namespace LibraryManagement1.Models
{
    public class Reader
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Mã độc giả là bắt buộc")]
        [StringLength(50)]
        [Display(Name = "Mã độc giả")]
        public string ReaderCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Họ tên độc giả là bắt buộc")]
        [StringLength(100)]
        [Display(Name = "Họ tên")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày sinh là bắt buộc")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime BirthDate { get; set; }

        [StringLength(200)]
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [StringLength(20)]
        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "Ngày lập thẻ")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Ngày hết hạn thẻ là bắt buộc")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày hết hạn")]
        public DateTime ExpiryDate { get; set; } = DateTime.Now.AddYears(1);

        public ICollection<BorrowTicket> BorrowTickets { get; set; } = new List<BorrowTicket>();
    }
}
