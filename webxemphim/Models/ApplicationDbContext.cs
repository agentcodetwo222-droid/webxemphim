using Microsoft.EntityFrameworkCore;
using webxemphim.Models.Schema;

namespace webxemphim.Models
{
    /// <summary>
    /// DbContext map truc tiep ten bang/cot trong database_redesign.sql v2.
    /// Controllers dung SchemaDataService de ghep/tach du lieu.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // User group
        public DbSet<AccountRow>      Accounts      { get; set; }
        public DbSet<AccountDateRow>  AccountDates  { get; set; }
        public DbSet<ProfileRow>      Profiles      { get; set; }
        public DbSet<ProfileDateRow>  ProfileDates  { get; set; }
        public DbSet<WalletRow>       Wallets       { get; set; }
        public DbSet<WalletDateRow>   WalletDates   { get; set; }

        // Movie group
        public DbSet<MovieInfoRow>    MovieInfos    { get; set; }
        public DbSet<MovieDateRow>    MovieDates    { get; set; }
        public DbSet<MovieMediaRow>   MovieMedias   { get; set; }
        public DbSet<MediaDateRow>    MediaDates    { get; set; }

        // Transaction group
        public DbSet<TxHeaderRow>     TxHeaders     { get; set; }
        public DbSet<TxDateRow>        TxDates       { get; set; }
        public DbSet<TxDetailRow>      TxDetails     { get; set; }

        // Bill group
        public DbSet<BillHeaderRow>    BillHeaders   { get; set; }
        public DbSet<BillDateRow>      BillDates     { get; set; }
        public DbSet<BillDetailRow>    BillDetails   { get; set; }

        // Standalone
        public DbSet<Category>         Categories    { get; set; }
        public DbSet<Currency>         Currencies    { get; set; }
        public DbSet<CurrencyDateRow>   CurrencyDates { get; set; }
        public DbSet<Favorite>         Favorites     { get; set; }
        public DbSet<FavoriteDateRow>   FavoriteDates { get; set; }
        public DbSet<WatchHistory>      WatchHistories { get; set; }
        public DbSet<WatchDateRow>      WatchDates    { get; set; }
        public DbSet<AuditLog>          AuditLogs     { get; set; }
        public DbSet<AuditDateRow>      AuditDates    { get; set; }
        public DbSet<LoginAttempt>      LoginAttempts { get; set; }
        public DbSet<LoginDateRow>      LoginDates    { get; set; }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            mb.Entity<AccountRow>(e =>
            {
                e.ToTable("account");
                e.HasKey(x => x.AccId);
                e.Property(x => x.AccId).HasColumnName("acc_id");
                e.Property(x => x.AccName).HasColumnName("acc_name");
                e.Property(x => x.AccHash).HasColumnName("acc_hash");
                e.Property(x => x.AccEmail).HasColumnName("acc_email");
                e.Property(x => x.AccRole).HasColumnName("acc_role");
                e.Property(x => x.AccLocked).HasColumnName("acc_locked");
                e.Property(x => x.AccStamp).HasColumnName("acc_stamp");
            });

            mb.Entity<AccountDateRow>(e =>
            {
                e.ToTable("account_date");
                e.HasKey(x => x.AccdId);
                e.Property(x => x.AccdId).HasColumnName("accd_id");
                e.Property(x => x.AccdAcc).HasColumnName("accd_acc");
                e.Property(x => x.AccdCreated).HasColumnName("accd_created");
            });

            mb.Entity<ProfileRow>(e =>
            {
                e.ToTable("profile");
                e.HasKey(x => x.ProfId);
                e.Property(x => x.ProfId).HasColumnName("prof_id");
                e.Property(x => x.ProfAcc).HasColumnName("prof_acc");
                e.Property(x => x.ProfName).HasColumnName("prof_name");
                e.Property(x => x.ProfPhone).HasColumnName("prof_phone");
                e.Property(x => x.ProfAddress).HasColumnName("prof_address");
                e.Property(x => x.ProfAvatar).HasColumnName("prof_avatar");
            });

            mb.Entity<ProfileDateRow>(e =>
            {
                e.ToTable("profile_date");
                e.HasKey(x => x.ProfdId);
                e.Property(x => x.ProfdId).HasColumnName("profd_id");
                e.Property(x => x.ProfdProf).HasColumnName("profd_prof");
                e.Property(x => x.ProfdVipExp).HasColumnName("profd_vip_exp");
                e.Property(x => x.ProfdUpdated).HasColumnName("profd_updated");
            });

            mb.Entity<WalletRow>(e =>
            {
                e.ToTable("wallet");
                e.HasKey(x => x.WalId);
                e.Property(x => x.WalId).HasColumnName("wal_id");
                e.Property(x => x.WalAcc).HasColumnName("wal_acc");
                e.Property(x => x.WalName).HasColumnName("wal_name");
                e.Property(x => x.WalBalance).HasColumnName("wal_balance");
            });

            mb.Entity<WalletDateRow>(e =>
            {
                e.ToTable("wallet_date");
                e.HasKey(x => x.WaldId);
                e.Property(x => x.WaldId).HasColumnName("wald_id");
                e.Property(x => x.WaldWal).HasColumnName("wald_wal");
                e.Property(x => x.WaldUpdated).HasColumnName("wald_updated");
            });

            mb.Entity<MovieInfoRow>(e =>
            {
                e.ToTable("movie_info");
                e.HasKey(x => x.MovId);
                e.Property(x => x.MovId).HasColumnName("mov_id");
                e.Property(x => x.MovTitle).HasColumnName("mov_title");
                e.Property(x => x.MovDesc).HasColumnName("mov_desc");
                e.Property(x => x.MovGenre).HasColumnName("mov_genre");
                e.Property(x => x.MovCountry).HasColumnName("mov_country");
                e.Property(x => x.MovYear).HasColumnName("mov_year");
                e.Property(x => x.MovDuration).HasColumnName("mov_duration");
                e.Property(x => x.MovActors).HasColumnName("mov_actors");
                e.Property(x => x.MovDirector).HasColumnName("mov_director");
                e.Property(x => x.MovCategory).HasColumnName("mov_category");
                e.Property(x => x.MovVip).HasColumnName("mov_vip");
                e.Property(x => x.MovActive).HasColumnName("mov_active");
                e.Property(x => x.MovViews).HasColumnName("mov_views");
            });

            mb.Entity<MovieDateRow>(e =>
            {
                e.ToTable("movie_date");
                e.HasKey(x => x.MovdId);
                e.Property(x => x.MovdId).HasColumnName("movd_id");
                e.Property(x => x.MovdMov).HasColumnName("movd_mov");
                e.Property(x => x.MovdCreated).HasColumnName("movd_created");
            });

            mb.Entity<MovieMediaRow>(e =>
            {
                e.ToTable("movie_media");
                e.HasKey(x => x.MediaId);
                e.Property(x => x.MediaId).HasColumnName("media_id");
                e.Property(x => x.MediaMov).HasColumnName("media_mov");
                e.Property(x => x.MediaTitle).HasColumnName("media_title");
                e.Property(x => x.MediaImage).HasColumnName("media_image");
                e.Property(x => x.MediaVideo).HasColumnName("media_video");
            });

            mb.Entity<MediaDateRow>(e =>
            {
                e.ToTable("media_date");
                e.HasKey(x => x.MediadId);
                e.Property(x => x.MediadId).HasColumnName("mediad_id");
                e.Property(x => x.MediadMedia).HasColumnName("mediad_media");
                e.Property(x => x.MediadUpdated).HasColumnName("mediad_updated");
            });

            mb.Entity<TxHeaderRow>(e =>
            {
                e.ToTable("tx_header");
                e.HasKey(x => x.TxId);
                e.Property(x => x.TxId).HasColumnName("tx_id");
                e.Property(x => x.TxAcc).HasColumnName("tx_acc");
                e.Property(x => x.TxName).HasColumnName("tx_name");
                e.Property(x => x.TxType).HasColumnName("tx_type");
                e.Property(x => x.TxDesc).HasColumnName("tx_desc");
                e.Property(x => x.TxStatus).HasColumnName("tx_status");
            });

            mb.Entity<TxDateRow>(e =>
            {
                e.ToTable("tx_date");
                e.HasKey(x => x.TxdateId);
                e.Property(x => x.TxdateId).HasColumnName("txdate_id");
                e.Property(x => x.TxdateTx).HasColumnName("txdate_tx");
                e.Property(x => x.TxdateCreated).HasColumnName("txdate_created");
            });

            mb.Entity<TxDetailRow>(e =>
            {
                e.ToTable("tx_detail");
                e.HasKey(x => x.TxdId);
                e.Property(x => x.TxdId).HasColumnName("txd_id");
                e.Property(x => x.TxdTx).HasColumnName("txd_tx");
                e.Property(x => x.TxdAmount).HasColumnName("txd_amount");
                e.Property(x => x.TxdCurrency).HasColumnName("txd_currency");
                e.Property(x => x.TxdVnd).HasColumnName("txd_vnd");
            });

            mb.Entity<BillHeaderRow>(e =>
            {
                e.ToTable("bill_header");
                e.HasKey(x => x.BillId);
                e.Property(x => x.BillId).HasColumnName("bill_id");
                e.Property(x => x.BillCode).HasColumnName("bill_code");
                e.Property(x => x.BillAcc).HasColumnName("bill_acc");
                e.Property(x => x.BillName).HasColumnName("bill_name");
                e.Property(x => x.BillEmail).HasColumnName("bill_email");
                e.Property(x => x.BillTx).HasColumnName("bill_tx");
                e.Property(x => x.BillType).HasColumnName("bill_type");
                e.Property(x => x.BillService).HasColumnName("bill_service");
                e.Property(x => x.BillStatus).HasColumnName("bill_status");
                e.Property(x => x.BillNote).HasColumnName("bill_note");
            });

            mb.Entity<BillDateRow>(e =>
            {
                e.ToTable("bill_date");
                e.HasKey(x => x.BilldId);
                e.Property(x => x.BilldId).HasColumnName("billd_id");
                e.Property(x => x.BilldBill).HasColumnName("billd_bill");
                e.Property(x => x.BilldCreated).HasColumnName("billd_created");
            });

            mb.Entity<BillDetailRow>(e =>
            {
                e.ToTable("bill_detail");
                e.HasKey(x => x.BildId);
                e.Property(x => x.BildId).HasColumnName("bild_id");
                e.Property(x => x.BildBill).HasColumnName("bild_bill");
                e.Property(x => x.BildAmount).HasColumnName("bild_amount");
                e.Property(x => x.BildCurrency).HasColumnName("bild_currency");
                e.Property(x => x.BildVnd).HasColumnName("bild_vnd");
                e.Property(x => x.BildBefore).HasColumnName("bild_before");
                e.Property(x => x.BildAfter).HasColumnName("bild_after");
            });

            mb.Entity<Category>(e =>
            {
                e.ToTable("category");
                e.HasKey(x => x.CategoryId);
                e.Property(x => x.CategoryId).HasColumnName("cat_id");
                e.Property(x => x.Name).HasColumnName("cat_name");
                e.Property(x => x.Description).HasColumnName("cat_desc");
                e.Property(x => x.Type).HasColumnName("cat_type");
                e.Property(x => x.IsActive).HasColumnName("cat_active");
                e.Property(x => x.SortOrder).HasColumnName("cat_order");
            });

            mb.Entity<Currency>(e =>
            {
                e.ToTable("currency");
                e.HasKey(x => x.CurrencyId);
                e.Property(x => x.CurrencyId).HasColumnName("cur_id");
                e.Property(x => x.Code).HasColumnName("cur_code");
                e.Property(x => x.Name).HasColumnName("cur_name");
                e.Property(x => x.Symbol).HasColumnName("cur_symbol");
                e.Property(x => x.ExchangeRate).HasColumnName("cur_rate");
                e.Property(x => x.IsActive).HasColumnName("cur_active");
                e.Ignore(x => x.LastUpdated);
            });

            mb.Entity<CurrencyDateRow>(e =>
            {
                e.ToTable("currency_date");
                e.HasKey(x => x.CurdId);
                e.Property(x => x.CurdId).HasColumnName("curd_id");
                e.Property(x => x.CurdCur).HasColumnName("curd_cur");
                e.Property(x => x.CurdUpdated).HasColumnName("curd_updated");
            });

            mb.Entity<Favorite>(e =>
            {
                e.ToTable("favorite");
                e.HasKey(x => x.FavoriteId);
                e.Property(x => x.FavoriteId).HasColumnName("fav_id");
                e.Property(x => x.UserId).HasColumnName("fav_acc");
                e.Property(x => x.UserName).HasColumnName("fav_name");
                e.Property(x => x.MovieId).HasColumnName("fav_mov");
                e.Property(x => x.MovieTitle).HasColumnName("fav_title");
                e.Property(x => x.MovieImage).HasColumnName("fav_image");
                e.Ignore(x => x.AddedAt);
            });

            mb.Entity<FavoriteDateRow>(e =>
            {
                e.ToTable("favorite_date");
                e.HasKey(x => x.FavdId);
                e.Property(x => x.FavdId).HasColumnName("favd_id");
                e.Property(x => x.FavdFav).HasColumnName("favd_fav");
                e.Property(x => x.FavdAdded).HasColumnName("favd_added");
            });

            mb.Entity<WatchHistory>(e =>
            {
                e.ToTable("watch_history");
                e.HasKey(x => x.WatchHistoryId);
                e.Property(x => x.WatchHistoryId).HasColumnName("wh_id");
                e.Property(x => x.UserId).HasColumnName("wh_acc");
                e.Property(x => x.UserName).HasColumnName("wh_name");
                e.Property(x => x.MovieId).HasColumnName("wh_mov");
                e.Property(x => x.MovieTitle).HasColumnName("wh_title");
                e.Property(x => x.MovieImage).HasColumnName("wh_image");
                e.Property(x => x.WatchDuration).HasColumnName("wh_duration");
                e.Property(x => x.IsCompleted).HasColumnName("wh_done");
                e.Ignore(x => x.WatchedAt);
            });

            mb.Entity<WatchDateRow>(e =>
            {
                e.ToTable("watch_date");
                e.HasKey(x => x.WhdId);
                e.Property(x => x.WhdId).HasColumnName("whd_id");
                e.Property(x => x.WhdWh).HasColumnName("whd_wh");
                e.Property(x => x.WhdAt).HasColumnName("whd_at");
            });

            mb.Entity<AuditLog>(e =>
            {
                e.ToTable("audit_log");
                e.HasKey(x => x.AuditLogId);
                e.Property(x => x.AuditLogId).HasColumnName("log_id");
                e.Property(x => x.Category).HasColumnName("log_cat");
                e.Property(x => x.Level).HasColumnName("log_level");
                e.Property(x => x.Message).HasColumnName("log_msg");
                e.Property(x => x.UserId).HasColumnName("log_acc");
                e.Property(x => x.UserName).HasColumnName("log_name");
                e.Property(x => x.IpAddress).HasColumnName("log_ip");
                e.Property(x => x.Detail).HasColumnName("log_detail");
                e.Ignore(x => x.Timestamp);
                e.HasIndex(x => x.Category);
            });

            mb.Entity<AuditDateRow>(e =>
            {
                e.ToTable("audit_date");
                e.HasKey(x => x.LogdId);
                e.Property(x => x.LogdId).HasColumnName("logd_id");
                e.Property(x => x.LogdLog).HasColumnName("logd_log");
                e.Property(x => x.LogdAt).HasColumnName("logd_at");
            });

            mb.Entity<LoginAttempt>(e =>
            {
                e.ToTable("login_attempt");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("la_id");
                e.Property(x => x.ClientKey).HasColumnName("la_key");
                e.Property(x => x.FailCount).HasColumnName("la_fail");
                e.Property(x => x.IsLocked).HasColumnName("la_locked");
                e.Ignore(x => x.LastAttempt);
                e.Ignore(x => x.LockedUntil);
                e.HasIndex(x => x.ClientKey).IsUnique();
            });

            mb.Entity<LoginDateRow>(e =>
            {
                e.ToTable("login_date");
                e.HasKey(x => x.LadId);
                e.Property(x => x.LadId).HasColumnName("lad_id");
                e.Property(x => x.LadLa).HasColumnName("lad_la");
                e.Property(x => x.LadLast).HasColumnName("lad_last");
                e.Property(x => x.LadUntil).HasColumnName("lad_until");
            });
        }
    }
}
