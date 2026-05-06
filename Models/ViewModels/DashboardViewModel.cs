using LibraryManagement.Models;

namespace LibraryManagement.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalBooks { get; set; }
        public int TotalReaders { get; set; }
        public int TotalBorrows { get; set; }
        public int ActiveBorrows { get; set; }
        public int OverdueBorrows { get; set; }

        public List<BookStats> TopBorrowedBooks { get; set; } = new();
        public List<ReaderStats> MostActiveReaders { get; set; } = new();
        public List<MonthlyTrend> BorrowingTrends { get; set; } = new();
    }

    public class BookStats
    {
        public string Title { get; set; } = string.Empty;
        public int BorrowCount { get; set; }
    }

    public class ReaderStats
    {
        public string FullName { get; set; } = string.Empty;
        public int BorrowCount { get; set; }
    }

    public class MonthlyTrend
    {
        public string Month { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
