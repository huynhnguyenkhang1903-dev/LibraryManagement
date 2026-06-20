using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement1.Models
{
    public class FineReceipt
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Mã biên lai")]
        public string ReceiptCode { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Độc giả")]
        public int ReaderId { get; set; }

        [ForeignKey("ReaderId")]
        public Reader? Reader { get; set; }

        [Required]
        [Display(Name = "Phiếu mượn")]
        public int BorrowTicketId { get; set; }

        [ForeignKey("BorrowTicketId")]
        public BorrowTicket? BorrowTicket { get; set; }

        [Display(Name = "Số tiền phạt")]
        public decimal FineAmount { get; set; }

        [Display(Name = "Số tiền thu")]
        [Range(0, 100000000, ErrorMessage = "Số tiền thu không hợp lệ")]
        public decimal AmountPaid { get; set; }

        [Display(Name = "Ngày thu")]
        public DateTime PaymentDate { get; set; } = DateTime.Now;
    }
}
