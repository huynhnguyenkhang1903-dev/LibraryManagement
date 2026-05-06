using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên thể loại là bắt buộc")]
        [StringLength(100)]
        [Display(Name = "Tên thể loại")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        public virtual ICollection<Book> Books { get; set; } = new List<Book>();
    }
}
