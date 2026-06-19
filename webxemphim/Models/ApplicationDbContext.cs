using Microsoft.EntityFrameworkCore;

namespace webxemphim.Models
{
    /// <summary>
    /// DbContext mapping C# properties -> ten bang/cot moi trong database.
    /// C# property giu nguyen, chi doi ten bang va cot bang ToTable() + HasColumnName().
    ///
    /// Schema moi:
    ///   account      (Users)
    ///   profile      (tach tu Users - thong tin ca nhan)
    ///   wallet       (tach tu Users - vi tien)
    ///   movie_info   (Movies - thong tin phim)
    ///   movie_media  (Movies - media URL ma hoa)
    ///   tx_header    (Transactions - thong tin giao dich)
    ///   tx_detail    (Transactions - so lieu tai chinh)
    ///   bill_header  (Bills - thong tin hoa don)
    ///   bill_detail  (Bills - so lieu tai chinh)
    ///   category     (Categories)
    ///   currency     (Currencies)
    ///   favorite     (Favorites)
    ///   watch_history (WatchHistories)
    ///   audit_log    (AuditLogs)
    ///   login_attempt (LoginAttempts)
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User>         Users         { get; set; }
        public DbSet<Movie>        Movies        { get; set; }
        public DbSet<Category>     Categories    { get; set; }
        public DbSet<Transaction>  Transactions  { get; set; }
        public DbSet<Currency>     Currencies    { get; set; }
        public DbSet<Bill>         Bills         { get; set; }
        public DbSet<WatchHistory> WatchHistories { get; set; }
        public DbSet<Favorite>     Favorites     { get; set; }
        public DbSet<AuditLog>     AuditLogs     { get; set; }
        public DbSet<LoginAttempt> LoginAttempts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ================================================================
            // USER -> bang "account"
            // C# property       -> cot DB moi
            // ================================================================
            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("account");
                e.HasKey(x => x.UserId);
                e.Property(x => x.UserId)          .HasColumnName("acc_id");
                e.Property(x => x.UserName)        .HasColumnName("acc_name");
                e.Property(x => x.MK)              .HasColumnName("acc_hash");
                e.Property(x => x.EMAIL)           .HasColumnName("acc_email");
                e.Property(x => x.ROLE)            .HasColumnName("acc_role");
                e.Property(x => x.IsLocked)        .HasColumnName("acc_locked");
                e.Property(x => x.SecurityStamp)   .HasColumnName("acc_stamp");
                e.Property(x => x.CreatedAt)       .HasColumnName("acc_created");
                // Profile fields (giu trong cung bang account de don gian)
                e.Property(x => x.Phone)           .HasColumnName("prof_phone");
                e.Property(x => x.Address)         .HasColumnName("prof_address");
                e.Property(x => x.Avatar)          .HasColumnName("prof_avatar");
                e.Property(x => x.VIPExpiryDate)   .HasColumnName("prof_vip_exp");
                // Wallet field
                e.Property(x => x.BalanceEncrypted).HasColumnName("wal_balance");
            });

            // ================================================================
            // MOVIE -> bang "movie_info"
            // (ImageUrl, VideoUrl giu trong cung bang de don gian)
            // ================================================================
            modelBuilder.Entity<Movie>(e =>
            {
                e.ToTable("movie_info");
                e.HasKey(x => x.MovieId);
                e.Property(x => x.MovieId)     .HasColumnName("mov_id");
                e.Property(x => x.Title)       .HasColumnName("mov_title");
                e.Property(x => x.Description) .HasColumnName("mov_desc");
                e.Property(x => x.Genre)       .HasColumnName("mov_genre");
                e.Property(x => x.Country)     .HasColumnName("mov_country");
                e.Property(x => x.Year)        .HasColumnName("mov_year");
                e.Property(x => x.ImageUrl)    .HasColumnName("media_image");
                e.Property(x => x.VideoUrl)    .HasColumnName("media_video");
                e.Property(x => x.IsVipOnly)   .HasColumnName("mov_vip");
                e.Property(x => x.IsAvailable) .HasColumnName("mov_active");
                e.Property(x => x.CategoryName).HasColumnName("mov_category");
                e.Property(x => x.Director)    .HasColumnName("mov_director");
                e.Property(x => x.Actors)      .HasColumnName("mov_actors");
                e.Property(x => x.Duration)    .HasColumnName("mov_duration");
                e.Property(x => x.TotalViews)  .HasColumnName("mov_views");
                e.Property(x => x.CreatedAt)   .HasColumnName("mov_created");
            });

            // ================================================================
            // CATEGORY -> bang "category"
            // ================================================================
            modelBuilder.Entity<Category>(e =>
            {
                e.ToTable("category");
                e.HasKey(x => x.CategoryId);
                e.Property(x => x.CategoryId) .HasColumnName("cat_id");
                e.Property(x => x.Name)       .HasColumnName("cat_name");
                e.Property(x => x.Description).HasColumnName("cat_desc");
                e.Property(x => x.Type)       .HasColumnName("cat_type");
                e.Property(x => x.IsActive)   .HasColumnName("cat_active");
                e.Property(x => x.SortOrder)  .HasColumnName("cat_order");
            });

            // ================================================================
            // TRANSACTION -> bang "tx_header"
            // (Amount, CurrencyCode, AmountInVND giu trong cung bang)
            // ================================================================
            modelBuilder.Entity<Transaction>(e =>
            {
                e.ToTable("tx_header");
                e.HasKey(x => x.TransactionId);
                e.Property(x => x.TransactionId).HasColumnName("tx_id");
                e.Property(x => x.UserId)       .HasColumnName("tx_acc");
                e.Property(x => x.UserName)     .HasColumnName("tx_name");
                e.Property(x => x.Type)         .HasColumnName("tx_type");
                e.Property(x => x.Amount)       .HasColumnName("txd_amount");
                e.Property(x => x.CurrencyCode) .HasColumnName("txd_currency");
                e.Property(x => x.AmountInVND)  .HasColumnName("txd_vnd");
                e.Property(x => x.Description)  .HasColumnName("tx_desc");
                e.Property(x => x.CreatedAt)    .HasColumnName("tx_created");
                e.Property(x => x.Status)       .HasColumnName("tx_status");
            });

            // ================================================================
            // CURRENCY -> bang "currency"
            // ================================================================
            modelBuilder.Entity<Currency>(e =>
            {
                e.ToTable("currency");
                e.HasKey(x => x.CurrencyId);
                e.Property(x => x.CurrencyId)  .HasColumnName("cur_id");
                e.Property(x => x.Code)        .HasColumnName("cur_code");
                e.Property(x => x.Name)        .HasColumnName("cur_name");
                e.Property(x => x.Symbol)      .HasColumnName("cur_symbol");
                e.Property(x => x.ExchangeRate).HasColumnName("cur_rate");
                e.Property(x => x.LastUpdated) .HasColumnName("cur_updated");
                e.Property(x => x.IsActive)    .HasColumnName("cur_active");
            });

            // ================================================================
            // BILL -> bang "bill_header"
            // (Amount, CurrencyCode, AmountInVND, BalanceBefore, BalanceAfter giu trong cung bang)
            // ================================================================
            modelBuilder.Entity<Bill>(e =>
            {
                e.ToTable("bill_header");
                e.HasKey(x => x.BillId);
                e.Property(x => x.BillId)       .HasColumnName("bill_id");
                e.Property(x => x.BillCode)     .HasColumnName("bill_code");
                e.Property(x => x.UserId)       .HasColumnName("bill_acc");
                e.Property(x => x.UserName)     .HasColumnName("bill_name");
                e.Property(x => x.UserEmail)    .HasColumnName("bill_email");
                e.Property(x => x.TransactionId).HasColumnName("bill_tx");
                e.Property(x => x.Type)         .HasColumnName("bill_type");
                e.Property(x => x.ServiceName)  .HasColumnName("bill_service");
                e.Property(x => x.Amount)       .HasColumnName("bild_amount");
                e.Property(x => x.CurrencyCode) .HasColumnName("bild_currency");
                e.Property(x => x.AmountInVND)  .HasColumnName("bild_vnd");
                e.Property(x => x.BalanceBefore).HasColumnName("bild_before");
                e.Property(x => x.BalanceAfter) .HasColumnName("bild_after");
                e.Property(x => x.CreatedAt)    .HasColumnName("bill_created");
                e.Property(x => x.Status)       .HasColumnName("bill_status");
                e.Property(x => x.Note)         .HasColumnName("bill_note");
            });

            // ================================================================
            // WATCH HISTORY -> bang "watch_history"
            // ================================================================
            modelBuilder.Entity<WatchHistory>(e =>
            {
                e.ToTable("watch_history");
                e.HasKey(x => x.WatchHistoryId);
                e.Property(x => x.WatchHistoryId).HasColumnName("wh_id");
                e.Property(x => x.UserId)        .HasColumnName("wh_acc");
                e.Property(x => x.UserName)      .HasColumnName("wh_name");
                e.Property(x => x.MovieId)       .HasColumnName("wh_mov");
                e.Property(x => x.MovieTitle)    .HasColumnName("wh_title");
                e.Property(x => x.MovieImage)    .HasColumnName("wh_image");
                e.Property(x => x.WatchedAt)     .HasColumnName("wh_at");
                e.Property(x => x.WatchDuration) .HasColumnName("wh_duration");
                e.Property(x => x.IsCompleted)   .HasColumnName("wh_done");
            });

            // ================================================================
            // FAVORITE -> bang "favorite"
            // ================================================================
            modelBuilder.Entity<Favorite>(e =>
            {
                e.ToTable("favorite");
                e.HasKey(x => x.FavoriteId);
                e.Property(x => x.FavoriteId).HasColumnName("fav_id");
                e.Property(x => x.UserId)    .HasColumnName("fav_acc");
                e.Property(x => x.UserName)  .HasColumnName("fav_name");
                e.Property(x => x.MovieId)   .HasColumnName("fav_mov");
                e.Property(x => x.MovieTitle).HasColumnName("fav_title");
                e.Property(x => x.MovieImage).HasColumnName("fav_image");
                e.Property(x => x.AddedAt)   .HasColumnName("fav_added");
            });

            // ================================================================
            // AUDIT LOG -> bang "audit_log"
            // ================================================================
            modelBuilder.Entity<AuditLog>(e =>
            {
                e.ToTable("audit_log");
                e.HasKey(x => x.AuditLogId);
                e.Property(x => x.AuditLogId).HasColumnName("log_id");
                e.Property(x => x.Timestamp) .HasColumnName("log_at");
                e.Property(x => x.Category)  .HasColumnName("log_cat");
                e.Property(x => x.Level)     .HasColumnName("log_level");
                e.Property(x => x.Message)   .HasColumnName("log_msg");
                e.Property(x => x.UserId)    .HasColumnName("log_acc");
                e.Property(x => x.UserName)  .HasColumnName("log_name");
                e.Property(x => x.IpAddress) .HasColumnName("log_ip");
                e.Property(x => x.Detail)    .HasColumnName("log_detail");
                e.HasIndex(x => x.Timestamp);
                e.HasIndex(x => x.Category);
            });

            // ================================================================
            // LOGIN ATTEMPT -> bang "login_attempt"
            // ================================================================
            modelBuilder.Entity<LoginAttempt>(e =>
            {
                e.ToTable("login_attempt");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id)          .HasColumnName("la_id");
                e.Property(x => x.ClientKey)   .HasColumnName("la_key");
                e.Property(x => x.FailCount)   .HasColumnName("la_fail");
                e.Property(x => x.LastAttempt) .HasColumnName("la_last");
                e.Property(x => x.IsLocked)    .HasColumnName("la_locked");
                e.Property(x => x.LockedUntil) .HasColumnName("la_until");
                e.HasIndex(x => x.ClientKey).IsUnique();
            });
        }
    }
}
