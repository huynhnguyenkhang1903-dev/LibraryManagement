using System.ComponentModel.DataAnnotations;

namespace LibraryManagement1.Models
{
    public class Author
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên tác giả là bắt buộc")]
        [StringLength(100)]
        [Display(Name = "Tên tác giả")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Tiểu sử")]
        public string? Bio { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime? BirthDate { get; set; }

        public ICollection<Book> Books { get; set; } = new List<Book>();
    }
}
