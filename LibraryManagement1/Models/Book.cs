using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement1.Models
{
    public class Book
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Mã sách là bắt buộc")]
        [StringLength(50)]
        [Display(Name = "Mã sách")]
        public string BookCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên sách là bắt buộc")]
        [StringLength(200)]
        [Display(Name = "Tên sách")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Thể loại là bắt buộc")]
        [Display(Name = "Thể loại")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        [Required(ErrorMessage = "Tác giả là bắt buộc")]
        [Display(Name = "Tác giả")]
        public int AuthorId { get; set; }

        [ForeignKey("AuthorId")]
        public Author? Author { get; set; }

        [Required(ErrorMessage = "Nhà xuất bản là bắt buộc")]
        [Display(Name = "Nhà xuất bản")]
        public int PublisherId { get; set; }

        [ForeignKey("PublisherId")]
        public Publisher? Publisher { get; set; }

        [Range(1000, 2100, ErrorMessage = "Năm xuất bản không hợp lệ")]
        [Display(Name = "Năm xuất bản")]
        public int PublishYear { get; set; }

        [Range(0, 10000, ErrorMessage = "Số lượng phải lớn hơn hoặc bằng 0")]
        [Display(Name = "Tổng số lượng")]
        public int Quantity { get; set; }

        [Range(0, 10000, ErrorMessage = "Số lượng khả dụng phải lớn hơn hoặc bằng 0")]
        [Display(Name = "Số lượng khả dụng")]
        public int AvailableQuantity { get; set; }

        [Range(0, 100000000, ErrorMessage = "Giá tiền không hợp lệ")]
        [Display(Name = "Giá sách")]
        public decimal Price { get; set; }

        [StringLength(500)]
        [Display(Name = "Ảnh bìa sách (URL)")]
        public string? ImageUrl { get; set; }
    }
}
