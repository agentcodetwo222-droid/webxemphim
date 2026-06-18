using Microsoft.EntityFrameworkCore;

namespace webxemphim.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Movie> Movies { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Currency> Currencies { get; set; }
        public DbSet<Bill> Bills { get; set; }
        public DbSet<WatchHistory> WatchHistories { get; set; }
        public DbSet<Favorite> Favorites { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== Mỗi bảng có PK riêng theo tên — không có FK constraint =====

            modelBuilder.Entity<User>(e =>
            {
                e.HasKey(x => x.UserId);
                e.ToTable("Users");
            });

            modelBuilder.Entity<Movie>(e =>
            {
                e.HasKey(x => x.MovieId);
                e.ToTable("Movies");
            });

            modelBuilder.Entity<Category>(e =>
            {
                e.HasKey(x => x.CategoryId);
                e.ToTable("Categories");
            });

            modelBuilder.Entity<Transaction>(e =>
            {
                e.HasKey(x => x.TransactionId);
                e.ToTable("Transactions");
            });

            modelBuilder.Entity<Currency>(e =>
            {
                e.HasKey(x => x.CurrencyId);
                e.ToTable("Currencies");
            });

            modelBuilder.Entity<Bill>(e =>
            {
                e.HasKey(x => x.BillId);
                e.ToTable("Bills");
            });

            modelBuilder.Entity<WatchHistory>(e =>
            {
                e.HasKey(x => x.WatchHistoryId);
                e.ToTable("WatchHistories");
            });

            modelBuilder.Entity<Favorite>(e =>
            {
                e.HasKey(x => x.FavoriteId);
                e.ToTable("Favorites");
            });
        }
    }
}
