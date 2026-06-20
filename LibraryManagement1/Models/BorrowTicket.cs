using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement1.Models
{
    public class BorrowTicket
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Mã phiếu mượn là bắt buộc")]
        [StringLength(50)]
        [Display(Name = "Mã phiếu mượn")]
        public string TicketCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn độc giả")]
        [Display(Name = "Độc giả")]
        public int ReaderId { get; set; }

        [ForeignKey("ReaderId")]
        public Reader? Reader { get; set; }

        [Required]
        [Display(Name = "Ngày mượn")]
        public DateTime BorrowDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Vui lòng chọn ngày hẹn trả")]
        [Display(Name = "Hạn trả")]
        public DateTime DueDate { get; set; } = DateTime.Now.AddDays(14);

        [Required]
        [StringLength(50)]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Borrowing"; // Borrowing, Returned, Overdue

        [Display(Name = "Người lập")]
        public int? CreatedByUserId { get; set; }

        [ForeignKey("CreatedByUserId")]
        public User? CreatedByUser { get; set; }

        public ICollection<BorrowDetail> BorrowDetails { get; set; } = new List<BorrowDetail>();
    }
}
