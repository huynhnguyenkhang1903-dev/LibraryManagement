using LibraryManagement1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement1.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class FineController : Controller
    {
        private readonly LibraryDbContext _context;

        public FineController(LibraryDbContext context)
        {
            _context = context;
        }

        // GET: Fine
        public async Task<IActionResult> Index()
        {
            // 1. Get all borrow details with fines
            var finedDetails = await _context.BorrowDetails
                .Include(d => d.BorrowTicket)
                    .ThenInclude(t => t!.Reader)
                .Include(d => d.Book)
                .Where(d => d.FineAmount > 0)
                .OrderByDescending(d => d.ReturnDate)
                .ToListAsync();

            // 2. Get all receipts
            var receipts = await _context.FineReceipts
                .Include(r => r.Reader)
                .Include(r => r.BorrowTicket)
                .OrderByDescending(r => r.PaymentDate)
                .ToListAsync();

            // Store in ViewData or Model
            ViewData["Receipts"] = receipts;
            return View(finedDetails);
        }

        // GET: Fine/Pay/5
        public async Task<IActionResult> Pay(int? detailId)
        {
            if (detailId == null) return NotFound();

            var detail = await _context.BorrowDetails
                .Include(d => d.BorrowTicket)
                    .ThenInclude(t => t!.Reader)
                .Include(d => d.Book)
                .FirstOrDefaultAsync(d => d.Id == detailId);

            if (detail == null) return NotFound();

            // Check if there is already a receipt for this borrow ticket
            var existingReceiptsSum = await _context.FineReceipts
                .Where(r => r.BorrowTicketId == detail.BorrowTicketId)
                .SumAsync(r => r.AmountPaid);

            var pendingAmount = detail.FineAmount - existingReceiptsSum;
            if (pendingAmount <= 0)
            {
                TempData["Message"] = "Khoản phạt này đã được thanh toán đầy đủ.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["PendingAmount"] = pendingAmount;
            return View(detail);
        }

        // POST: Fine/Pay
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(int borrowTicketId, int readerId, decimal fineAmount, decimal amountPaid)
        {
            if (amountPaid <= 0)
            {
                ModelState.AddModelError("AmountPaid", "Số tiền thanh toán phải lớn hơn 0.");
                // Reload
                return RedirectToAction(nameof(Index));
            }

            var ticketCount = await _context.FineReceipts.CountAsync();
            var receiptCode = $"BL{DateTime.Now:yyyyMMdd}-{ticketCount + 1:D3}";

            var receipt = new FineReceipt
            {
                ReceiptCode = receiptCode,
                ReaderId = readerId,
                BorrowTicketId = borrowTicketId,
                FineAmount = fineAmount,
                AmountPaid = amountPaid,
                PaymentDate = DateTime.Now
            };

            _context.FineReceipts.Add(receipt);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã lập biên lai thu phạt {receiptCode} thành công. Số tiền thu: {amountPaid:N0} đ.";
            return RedirectToAction(nameof(Index));
        }
    }
}
