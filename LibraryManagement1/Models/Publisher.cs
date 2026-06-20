using System.ComponentModel.DataAnnotations;

namespace LibraryManagement1.Models
{
    public class Publisher
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên nhà xuất bản là bắt buộc")]
        [StringLength(150)]
        [Display(Name = "Tên nhà xuất bản")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [StringLength(20)]
        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? Phone { get; set; }

        public ICollection<Book> Books { get; set; } = new List<Book>();
    }
}
