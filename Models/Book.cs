using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement.Models
{
    public class Book
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tiêu đề sách là bắt buộc")]
        [StringLength(255)]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tác giả là bắt buộc")]
        [StringLength(200)]
        [Display(Name = "Tác giả")]
        public string Author { get; set; } = string.Empty;

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [StringLength(50)]
        public string? ISBN { get; set; }

        [Display(Name = "Ảnh bìa")]
        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Giá sách là bắt buộc")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Giá")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Số lượng tồn kho là bắt buộc")]
        [Display(Name = "Số lượng tồn")]
        public int StockQuantity { get; set; }

        [Display(Name = "Thể loại")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
