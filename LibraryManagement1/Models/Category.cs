using System.ComponentModel.DataAnnotations;

namespace LibraryManagement1.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên thể loại là bắt buộc")]
        [StringLength(100)]
        [Display(Name = "Tên thể loại")]
        public string Name { get; set; } = string.Empty;

        [StringLength(250)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        public ICollection<Book> Books { get; set; } = new List<Book>();
    }
}
