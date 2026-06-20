using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement1.Models
{
    public class BorrowDetail
    {
        public int Id { get; set; }

        [Required]
        public int BorrowTicketId { get; set; }

        [ForeignKey("BorrowTicketId")]
        public BorrowTicket? BorrowTicket { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn sách")]
        [Display(Name = "Sách")]
        public int BookId { get; set; }

        [ForeignKey("BookId")]
        public Book? Book { get; set; }

        [Display(Name = "Ngày trả thực tế")]
        public DateTime? ReturnDate { get; set; }

        [Display(Name = "Tiền phạt")]
        public decimal FineAmount { get; set; } = 0;

        [StringLength(250)]
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }
    }
}
