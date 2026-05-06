using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement.Models
{
    public class BorrowRecord
    {
        public int Id { get; set; }

        [Required]
        public int BookId { get; set; }

        [ForeignKey("BookId")]
        public virtual Book? Book { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Display(Name = "Ngày mượn")]
        public DateTime BorrowDate { get; set; } = DateTime.Now;

        [Display(Name = "Hạn trả")]
        public DateTime DueDate { get; set; }

        [Display(Name = "Ngày trả thực tế")]
        public DateTime? ReturnDate { get; set; }

        [Display(Name = "Đã trả?")]
        public bool IsReturned { get; set; } = false;

        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }

        [Display(Name = "Tiền phạt")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal FineAmount { get; set; } = 0;

        [NotMapped]
        public int OverdueDays
        {
            get
            {
                if (IsReturned)
                {
                    if (ReturnDate > DueDate)
                        return (ReturnDate.Value - DueDate).Days;
                    return 0;
                }
                if (DateTime.Now > DueDate)
                    return (DateTime.Now - DueDate).Days;
                return 0;
            }
        }

        [NotMapped]
        public decimal CurrentFine => OverdueDays * 5000; // 5000 VND per day

        public string Status
        {
            get
            {
                if (IsReturned) return "Đã trả";
                if (DateTime.Now > DueDate) return "Quá hạn";
                return "Đang mượn";
            }
        }
    }
}
