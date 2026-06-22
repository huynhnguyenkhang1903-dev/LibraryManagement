using Microsoft.EntityFrameworkCore;

namespace LibraryManagement1.Models
{
    public class LibraryDbContext : DbContext
    {
        public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<Publisher> Publishers { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<Reader> Readers { get; set; }
        public DbSet<BorrowTicket> BorrowTickets { get; set; }
        public DbSet<BorrowDetail> BorrowDetails { get; set; }
        public DbSet<FineReceipt> FineReceipts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships if needed
            modelBuilder.Entity<BorrowTicket>()
                .HasOne(t => t.Reader)
                .WithMany(r => r.BorrowTickets)
                .HasForeignKey(t => t.ReaderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BorrowDetail>()
                .HasOne(d => d.BorrowTicket)
                .WithMany(t => t.BorrowDetails)
                .HasForeignKey(d => d.BorrowTicketId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BorrowDetail>()
                .HasOne(d => d.Book)
                .WithMany()
                .HasForeignKey(d => d.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure decimal precision for SQL Server compatibility
            modelBuilder.Entity<Book>()
                .Property(b => b.Price)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<BorrowDetail>()
                .Property(d => d.FineAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<FineReceipt>()
                .Property(f => f.FineAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<FineReceipt>()
                .Property(f => f.AmountPaid)
                .HasColumnType("decimal(18,2)");

            // Configure relationships for FineReceipt to restrict delete cascade loops
            modelBuilder.Entity<FineReceipt>()
                .HasOne(f => f.Reader)
                .WithMany()
                .HasForeignKey(f => f.ReaderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FineReceipt>()
                .HasOne(f => f.BorrowTicket)
                .WithMany()
                .HasForeignKey(f => f.BorrowTicketId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public static void SeedData(LibraryDbContext context)
        {
            context.Database.EnsureCreated();

            try
            {
                context.Database.ExecuteSqlRaw("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Books') AND name = 'ImageUrl') ALTER TABLE Books ADD ImageUrl NVARCHAR(500) NULL;");
            }
            catch { }

            if (context.Users.Any())
            {
                return; // DB has been seeded
            }

            // Seed Users
            var users = new List<User>
            {
                new User { Username = "admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), FullName = "Quản trị viên", Role = "Admin" },
                new User { Username = "thuthu", PasswordHash = BCrypt.Net.BCrypt.HashPassword("thuthu123"), FullName = "Nguyễn Văn Thư", Role = "Staff" }
            };
            context.Users.AddRange(users);

            // Seed Categories
            var categories = new List<Category>
            {
                new Category { Name = "Công nghệ thông tin", Description = "Sách về lập trình, phần cứng, AI và khoa học máy tính" },
                new Category { Name = "Văn học nghệ thuật", Description = "Tiểu thuyết, truyện ngắn, thơ ca" },
                new Category { Name = "Kinh tế - Kinh doanh", Description = "Quản trị, tài chính, marketing, khởi nghiệp" },
                new Category { Name = "Khoa học tự nhiên", Description = "Toán học, vật lý, hóa học, sinh học" },
                new Category { Name = "Kỹ năng sống", Description = "Phát triển bản thân, kỹ năng giao tiếp" }
            };
            context.Categories.AddRange(categories);
            context.SaveChanges();

            // Seed Authors
            var authors = new List<Author>
            {
                new Author { Name = "Robert C. Martin", Bio = "Tác giả của cuốn sách nổi tiếng Clean Code, Uncle Bob là chuyên gia phát triển phần mềm.", BirthDate = new DateTime(1952, 12, 5) },
                new Author { Name = "Nguyễn Nhật Ánh", Bio = "Nhà văn Việt Nam nổi tiếng chuyên viết cho tuổi mới lớn.", BirthDate = new DateTime(1955, 5, 7) },
                new Author { Name = "Dale Carnegie", Bio = "Nhà văn và nhà thuyết trình Mỹ, tác giả cuốn Đắc Nhân Tâm.", BirthDate = new DateTime(1888, 11, 24) },
                new Author { Name = "Adam Smith", Bio = "Nhà triết học và kinh tế học lỗi lạc người Scotland.", BirthDate = new DateTime(1723, 6, 16) }
            };
            context.Authors.AddRange(authors);
            context.SaveChanges();

            // Seed Publishers
            var publishers = new List<Publisher>
            {
                new Publisher { Name = "NXB Trẻ", Address = "161B Lý Chính Thắng, Quận 3, TP.HCM", Phone = "02839316289" },
                new Publisher { Name = "NXB Lao Động", Address = "175 Giảng Võ, Đống Đa, Hà Nội", Phone = "02438515380" },
                new Publisher { Name = "O'Reilly Media", Address = "1005 Gravenstein Highway North, Sebastopol, CA", Phone = "18009989938" },
                new Publisher { Name = "NXB Tổng hợp TP.HCM", Address = "62 Nguyễn Thị Minh Khai, Quận 1, TP.HCM", Phone = "02838225018" }
            };
            context.Publishers.AddRange(publishers);
            context.SaveChanges();

            // Seed Books
            var books = new List<Book>
            {
                new Book { BookCode = "MS001", Title = "Clean Code: A Handbook of Agile Software Craftsmanship", CategoryId = categories[0].Id, AuthorId = authors[0].Id, PublisherId = publishers[2].Id, PublishYear = 2008, Quantity = 10, AvailableQuantity = 9, Price = 150000, ImageUrl = "https://images-na.ssl-images-amazon.com/images/I/41xShCOh3mL._SX218_BO1,204,203,200_QL40_FMwebp_.jpg" },
                new Book { BookCode = "MS002", Title = "Mắt Biếc", CategoryId = categories[1].Id, AuthorId = authors[1].Id, PublisherId = publishers[0].Id, PublishYear = 1990, Quantity = 15, AvailableQuantity = 13, Price = 95000, ImageUrl = "https://bizweb.dktcdn.net/100/197/269/products/mat-biec-bia-2019-1.jpg?v=1574068595460" },
                new Book { BookCode = "MS003", Title = "Đắc Nhân Tâm (How to Win Friends and Influence People)", CategoryId = categories[4].Id, AuthorId = authors[2].Id, PublisherId = publishers[1].Id, PublishYear = 1936, Quantity = 20, AvailableQuantity = 19, Price = 86000, ImageUrl = "https://bizweb.dktcdn.net/100/197/269/products/dac-nhan-tam-kho-lon.jpg?v=1522033060197" },
                new Book { BookCode = "MS004", Title = "Cơ sở dữ liệu lớn và AI", CategoryId = categories[0].Id, AuthorId = authors[0].Id, PublisherId = publishers[2].Id, PublishYear = 2021, Quantity = 5, AvailableQuantity = 5, Price = 230000, ImageUrl = "https://m.media-amazon.com/images/I/81P8H8fD2BL._AC_UF1000,1000_QL80_.jpg" },
                new Book { BookCode = "MS005", Title = "Cho tôi xin một vé đi tuổi thơ", CategoryId = categories[1].Id, AuthorId = authors[1].Id, PublisherId = publishers[0].Id, PublishYear = 2008, Quantity = 12, AvailableQuantity = 12, Price = 88000, ImageUrl = "https://bizweb.dktcdn.net/100/197/269/products/cho-toi-xin-mot-ve-di-tuoi-tho-bia-mem-tai-ban-2018.jpg?v=1527756181747" }
            };
            context.Books.AddRange(books);
            context.SaveChanges();

            // Seed Readers
            var readers = new List<Reader>
            {
                new Reader { ReaderCode = "DG001", Name = "Trần Văn Hùng", BirthDate = new DateTime(1998, 3, 15), Address = "Quận 1, TP.HCM", Phone = "0901234567", Email = "hung.tran@gmail.com", CreatedDate = DateTime.Now.AddMonths(-3), ExpiryDate = DateTime.Now.AddMonths(9) },
                new Reader { ReaderCode = "DG002", Name = "Lê Thị Mai", BirthDate = new DateTime(2001, 8, 22), Address = "Cầu Giấy, Hà Nội", Phone = "0987654321", Email = "mai.le@gmail.com", CreatedDate = DateTime.Now.AddMonths(-6), ExpiryDate = DateTime.Now.AddMonths(6) },
                new Reader { ReaderCode = "DG003", Name = "Nguyễn Hoàng Nam", BirthDate = new DateTime(1995, 11, 2), Address = "Hải Châu, Đà Nẵng", Phone = "0971122334", Email = "nam.nguyen@gmail.com", CreatedDate = DateTime.Now.AddMonths(-1), ExpiryDate = DateTime.Now.AddMonths(11) },
                new Reader { ReaderCode = "DG004", Name = "Phạm Minh Anh", BirthDate = new DateTime(2000, 5, 30), Address = "Ninh Kiều, Cần Thơ", Phone = "0939888999", Email = "minhanh.pham@gmail.com", CreatedDate = DateTime.Now.AddYears(-1), ExpiryDate = DateTime.Now.AddDays(-5) } // Expired card
            };
            context.Readers.AddRange(readers);
            context.SaveChanges();

            // Seed Borrow Tickets
            // Ticket 1: Active and normal
            var ticket1 = new BorrowTicket
            {
                TicketCode = "PM001",
                ReaderId = readers[0].Id,
                BorrowDate = DateTime.Now.AddDays(-7),
                DueDate = DateTime.Now.AddDays(7),
                Status = "Borrowing"
            };
            // Ticket 2: Active and Overdue
            var ticket2 = new BorrowTicket
            {
                TicketCode = "PM002",
                ReaderId = readers[1].Id,
                BorrowDate = DateTime.Now.AddDays(-20),
                DueDate = DateTime.Now.AddDays(-6),
                Status = "Overdue"
            };
            // Ticket 3: Returned
            var ticket3 = new BorrowTicket
            {
                TicketCode = "PM003",
                ReaderId = readers[2].Id,
                BorrowDate = DateTime.Now.AddDays(-10),
                DueDate = DateTime.Now.AddDays(4),
                Status = "Returned"
            };

            context.BorrowTickets.AddRange(ticket1, ticket2, ticket3);
            context.SaveChanges();

            // Details
            var details = new List<BorrowDetail>
            {
                new BorrowDetail { BorrowTicketId = ticket1.Id, BookId = books[0].Id },
                new BorrowDetail { BorrowTicketId = ticket2.Id, BookId = books[1].Id },
                new BorrowDetail { BorrowTicketId = ticket2.Id, BookId = books[2].Id },
                new BorrowDetail { BorrowTicketId = ticket3.Id, BookId = books[1].Id, ReturnDate = DateTime.Now.AddDays(-1), FineAmount = 0 }
            };
            context.BorrowDetails.AddRange(details);
            context.SaveChanges();
        }
    }
}
