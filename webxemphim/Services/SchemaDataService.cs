using Microsoft.EntityFrameworkCore;
using webxemphim.Models;
using webxemphim.Models.Schema;

namespace webxemphim.Services
{
    /// <summary>
    /// Ghep/tach du lieu giua cac bang doc lap trong schema v2.
    /// Controllers van dung model User, Movie, Transaction, Bill nhu cu.
    /// </summary>
    public class SchemaDataService
    {
        private readonly ApplicationDbContext _db;

        public SchemaDataService(ApplicationDbContext db) => _db = db;

        public Task SaveChangesAsync() => _db.SaveChangesAsync();

        // ═══════════════════════════════════════════════════════════════════
        // USER
        // ═══════════════════════════════════════════════════════════════════

        public async Task<List<User>> GetAllUsersAsync()
        {
            var accounts = await _db.Accounts.AsNoTracking().ToListAsync();
            var result = new List<User>();
            foreach (var acc in accounts)
                result.Add(await BuildUserAsync(acc));
            return result;
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            var acc = await _db.Accounts.AsNoTracking()
                .FirstOrDefaultAsync(a => a.AccId == userId);
            return acc == null ? null : await BuildUserAsync(acc);
        }

        public async Task AddUserAsync(User user)
        {
            var now = user.CreatedAt == default ? DateTime.UtcNow : user.CreatedAt;

            var acc = new AccountRow
            {
                AccName = user.UserName,
                AccHash = user.MK,
                AccEmail = user.EMAIL,
                AccRole = user.ROLE,
                AccLocked = user.IsLocked,
                AccStamp = user.SecurityStamp
            };
            _db.Accounts.Add(acc);
            await _db.SaveChangesAsync();
            user.UserId = acc.AccId;

            _db.AccountDates.Add(new AccountDateRow { AccdAcc = acc.AccId, AccdCreated = now });

            var prof = new ProfileRow
            {
                ProfAcc = acc.AccId,
                ProfName = user.UserName,
                ProfPhone = user.Phone,
                ProfAddress = user.Address,
                ProfAvatar = user.Avatar
            };
            _db.Profiles.Add(prof);
            await _db.SaveChangesAsync();

            _db.ProfileDates.Add(new ProfileDateRow
            {
                ProfdProf = prof.ProfId,
                ProfdVipExp = user.VIPExpiryDate,
                ProfdUpdated = now
            });

            var wal = new WalletRow
            {
                WalAcc = acc.AccId,
                WalName = user.UserName,
                WalBalance = user.BalanceEncrypted
            };
            _db.Wallets.Add(wal);
            await _db.SaveChangesAsync();

            _db.WalletDates.Add(new WalletDateRow { WaldWal = wal.WalId, WaldUpdated = now });
            await _db.SaveChangesAsync();
        }

        public async Task UpdateUserAsync(User user)
        {
            var acc = await _db.Accounts.FirstOrDefaultAsync(a => a.AccId == user.UserId)
                ?? throw new InvalidOperationException($"User {user.UserId} not found");

            acc.AccName = user.UserName;
            acc.AccHash = user.MK;
            acc.AccEmail = user.EMAIL;
            acc.AccRole = user.ROLE;
            acc.AccLocked = user.IsLocked;
            acc.AccStamp = user.SecurityStamp;

            var prof = await _db.Profiles.FirstOrDefaultAsync(p => p.ProfAcc == user.UserId);
            if (prof != null)
            {
                prof.ProfName = user.UserName;
                prof.ProfPhone = user.Phone;
                prof.ProfAddress = user.Address;
                prof.ProfAvatar = user.Avatar;

                var profDate = await _db.ProfileDates.FirstOrDefaultAsync(d => d.ProfdProf == prof.ProfId);
                if (profDate != null)
                {
                    profDate.ProfdVipExp = user.VIPExpiryDate;
                    profDate.ProfdUpdated = DateTime.UtcNow;
                }
            }

            var wal = await _db.Wallets.FirstOrDefaultAsync(w => w.WalAcc == user.UserId);
            if (wal != null)
            {
                wal.WalName = user.UserName;
                wal.WalBalance = user.BalanceEncrypted;

                var walDate = await _db.WalletDates.FirstOrDefaultAsync(d => d.WaldWal == wal.WalId);
                if (walDate != null)
                    walDate.WaldUpdated = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
        }

        public async Task DeleteUserAsync(int userId)
        {
            var prof = await _db.Profiles.FirstOrDefaultAsync(p => p.ProfAcc == userId);
            if (prof != null)
            {
                var profDates = _db.ProfileDates.Where(d => d.ProfdProf == prof.ProfId);
                _db.ProfileDates.RemoveRange(profDates);
                _db.Profiles.Remove(prof);
            }

            var wal = await _db.Wallets.FirstOrDefaultAsync(w => w.WalAcc == userId);
            if (wal != null)
            {
                var walDates = _db.WalletDates.Where(d => d.WaldWal == wal.WalId);
                _db.WalletDates.RemoveRange(walDates);
                _db.Wallets.Remove(wal);
            }

            var accDates = _db.AccountDates.Where(d => d.AccdAcc == userId);
            _db.AccountDates.RemoveRange(accDates);

            var acc = await _db.Accounts.FindAsync(userId);
            if (acc != null) _db.Accounts.Remove(acc);

            await _db.SaveChangesAsync();
        }

        public Task<bool> UserExistsAsync(int id)
            => _db.Accounts.AnyAsync(a => a.AccId == id);

        private async Task<User> BuildUserAsync(AccountRow acc)
        {
            var prof = await _db.Profiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProfAcc == acc.AccId);
            var profDate = prof != null
                ? await _db.ProfileDates.AsNoTracking()
                    .FirstOrDefaultAsync(d => d.ProfdProf == prof.ProfId)
                : null;
            var wal = await _db.Wallets.AsNoTracking()
                .FirstOrDefaultAsync(w => w.WalAcc == acc.AccId);
            var accDate = await _db.AccountDates.AsNoTracking()
                .FirstOrDefaultAsync(d => d.AccdAcc == acc.AccId);

            return new User
            {
                UserId = acc.AccId,
                UserName = acc.AccName,
                MK = acc.AccHash,
                EMAIL = acc.AccEmail,
                ROLE = acc.AccRole,
                IsLocked = acc.AccLocked,
                SecurityStamp = acc.AccStamp,
                CreatedAt = accDate?.AccdCreated ?? DateTime.UtcNow,
                Phone = prof?.ProfPhone,
                Address = prof?.ProfAddress,
                Avatar = prof?.ProfAvatar,
                VIPExpiryDate = profDate?.ProfdVipExp,
                BalanceEncrypted = wal?.WalBalance ?? ""
            };
        }

        // ═══════════════════════════════════════════════════════════════════
        // MOVIE
        // ═══════════════════════════════════════════════════════════════════

        public async Task<List<Movie>> GetAllMoviesAsync()
        {
            var infos = await _db.MovieInfos.AsNoTracking().ToListAsync();
            var result = new List<Movie>();
            foreach (var info in infos)
                result.Add(await BuildMovieAsync(info));
            return result;
        }

        public async Task<Movie?> GetMovieByIdAsync(int movieId)
        {
            var info = await _db.MovieInfos.AsNoTracking()
                .FirstOrDefaultAsync(m => m.MovId == movieId);
            return info == null ? null : await BuildMovieAsync(info);
        }

        public Task<bool> AnyMovieAsync(System.Linq.Expressions.Expression<Func<MovieInfoRow, bool>> pred)
            => _db.MovieInfos.AnyAsync(pred);

        public async Task AddMovieAsync(Movie movie)
        {
            var now = movie.CreatedAt == default ? DateTime.UtcNow : movie.CreatedAt;

            var info = new MovieInfoRow
            {
                MovTitle = movie.Title,
                MovDesc = movie.Description,
                MovGenre = movie.Genre,
                MovCountry = movie.Country,
                MovYear = movie.Year,
                MovDuration = movie.Duration,
                MovActors = movie.Actors,
                MovDirector = movie.Director,
                MovCategory = movie.CategoryName,
                MovVip = movie.IsVipOnly,
                MovActive = movie.IsAvailable,
                MovViews = movie.TotalViews
            };
            _db.MovieInfos.Add(info);
            await _db.SaveChangesAsync();
            movie.MovieId = info.MovId;

            _db.MovieDates.Add(new MovieDateRow { MovdMov = info.MovId, MovdCreated = now });

            var media = new MovieMediaRow
            {
                MediaMov = info.MovId,
                MediaTitle = movie.Title,
                MediaImage = movie.ImageUrl,
                MediaVideo = movie.VideoUrl
            };
            _db.MovieMedias.Add(media);
            await _db.SaveChangesAsync();

            _db.MediaDates.Add(new MediaDateRow { MediadMedia = media.MediaId, MediadUpdated = now });
            await _db.SaveChangesAsync();
        }

        public async Task UpdateMovieAsync(Movie movie)
        {
            var info = await _db.MovieInfos.FirstOrDefaultAsync(m => m.MovId == movie.MovieId)
                ?? throw new InvalidOperationException($"Movie {movie.MovieId} not found");

            info.MovTitle = movie.Title;
            info.MovDesc = movie.Description;
            info.MovGenre = movie.Genre;
            info.MovCountry = movie.Country;
            info.MovYear = movie.Year;
            info.MovDuration = movie.Duration;
            info.MovActors = movie.Actors;
            info.MovDirector = movie.Director;
            info.MovCategory = movie.CategoryName;
            info.MovVip = movie.IsVipOnly;
            info.MovActive = movie.IsAvailable;
            info.MovViews = movie.TotalViews;

            var media = await _db.MovieMedias.FirstOrDefaultAsync(m => m.MediaMov == movie.MovieId);
            if (media != null)
            {
                media.MediaTitle = movie.Title;
                media.MediaImage = movie.ImageUrl;
                media.MediaVideo = movie.VideoUrl;

                var mediaDate = await _db.MediaDates.FirstOrDefaultAsync(d => d.MediadMedia == media.MediaId);
                if (mediaDate != null)
                    mediaDate.MediadUpdated = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
        }

        public async Task DeleteMovieAsync(int movieId)
        {
            var media = await _db.MovieMedias.FirstOrDefaultAsync(m => m.MediaMov == movieId);
            if (media != null)
            {
                var mediaDates = _db.MediaDates.Where(d => d.MediadMedia == media.MediaId);
                _db.MediaDates.RemoveRange(mediaDates);
                _db.MovieMedias.Remove(media);
            }

            var movDates = _db.MovieDates.Where(d => d.MovdMov == movieId);
            _db.MovieDates.RemoveRange(movDates);

            var info = await _db.MovieInfos.FindAsync(movieId);
            if (info != null) _db.MovieInfos.Remove(info);

            await _db.SaveChangesAsync();
        }

        public Task<bool> MovieExistsAsync(int id)
            => _db.MovieInfos.AnyAsync(m => m.MovId == id);

        public Task<bool> AnyMovieTitleContainsAsync(string fragment)
            => _db.MovieInfos.AnyAsync(m => m.MovTitle.Contains(fragment));

        public async Task<List<Movie>> GetMoviesFilteredAsync(bool admin, bool availableOnly = false, string? genreContains = null)
        {
            var query = _db.MovieInfos.AsNoTracking().AsQueryable();
            if (!admin && availableOnly)
                query = query.Where(m => m.MovActive);
            if (!string.IsNullOrEmpty(genreContains))
                query = query.Where(m => m.MovGenre.Contains(genreContains));

            var infos = await query.ToListAsync();
            var result = new List<Movie>();
            foreach (var info in infos)
                result.Add(await BuildMovieAsync(info));
            return result;
        }

        public async Task<Movie?> GetFirstMovieAsync()
        {
            var info = await _db.MovieInfos.AsNoTracking().FirstOrDefaultAsync();
            return info == null ? null : await BuildMovieAsync(info);
        }

        public async Task<WatchHistory?> GetWatchHistoryByIdAsync(int id)
        {
            var w = await _db.WatchHistories.FindAsync(id);
            if (w != null)
                w.WatchedAt = await GetWatchedAtAsync(w.WatchHistoryId);
            return w;
        }

        private async Task<Movie> BuildMovieAsync(MovieInfoRow info)
        {
            var media = await _db.MovieMedias.AsNoTracking()
                .FirstOrDefaultAsync(m => m.MediaMov == info.MovId);
            var movDate = await _db.MovieDates.AsNoTracking()
                .FirstOrDefaultAsync(d => d.MovdMov == info.MovId);

            return new Movie
            {
                MovieId = info.MovId,
                Title = info.MovTitle,
                Description = info.MovDesc,
                Genre = info.MovGenre,
                Country = info.MovCountry,
                Year = info.MovYear,
                Duration = info.MovDuration,
                Actors = info.MovActors,
                Director = info.MovDirector,
                CategoryName = info.MovCategory,
                IsVipOnly = info.MovVip,
                IsAvailable = info.MovActive,
                TotalViews = info.MovViews,
                CreatedAt = movDate?.MovdCreated ?? DateTime.UtcNow,
                ImageUrl = media?.MediaImage ?? "",
                VideoUrl = media?.MediaVideo ?? ""
            };
        }

        // ═══════════════════════════════════════════════════════════════════
        // TRANSACTION
        // ═══════════════════════════════════════════════════════════════════

        public async Task<List<Transaction>> GetTransactionsByUserAsync(int userId)
        {
            var headers = await _db.TxHeaders.AsNoTracking()
                .Where(t => t.TxAcc == userId).ToListAsync();
            var result = new List<Transaction>();
            foreach (var h in headers)
                result.Add(await BuildTransactionAsync(h));
            return result.OrderByDescending(t => t.CreatedAt).ToList();
        }

        public async Task AddTransactionAsync(Transaction tx)
        {
            var now = tx.CreatedAt == default ? DateTime.UtcNow : tx.CreatedAt;

            var header = new TxHeaderRow
            {
                TxAcc = tx.UserId,
                TxName = tx.UserName,
                TxType = tx.Type,
                TxDesc = tx.Description,
                TxStatus = tx.Status
            };
            _db.TxHeaders.Add(header);
            await _db.SaveChangesAsync();
            tx.TransactionId = header.TxId;

            _db.TxDates.Add(new TxDateRow { TxdateTx = header.TxId, TxdateCreated = now });

            _db.TxDetails.Add(new TxDetailRow
            {
                TxdTx = header.TxId,
                TxdAmount = tx.Amount,
                TxdCurrency = tx.CurrencyCode,
                TxdVnd = tx.AmountInVND
            });
            await _db.SaveChangesAsync();
        }

        private async Task<Transaction> BuildTransactionAsync(TxHeaderRow h)
        {
            var detail = await _db.TxDetails.AsNoTracking()
                .FirstOrDefaultAsync(d => d.TxdTx == h.TxId);
            var date = await _db.TxDates.AsNoTracking()
                .FirstOrDefaultAsync(d => d.TxdateTx == h.TxId);

            return new Transaction
            {
                TransactionId = h.TxId,
                UserId = h.TxAcc,
                UserName = h.TxName,
                Type = h.TxType,
                Description = h.TxDesc,
                Status = h.TxStatus,
                CreatedAt = date?.TxdateCreated ?? DateTime.UtcNow,
                Amount = detail?.TxdAmount ?? "",
                CurrencyCode = detail?.TxdCurrency ?? "",
                AmountInVND = detail?.TxdVnd ?? ""
            };
        }

        // ═══════════════════════════════════════════════════════════════════
        // BILL
        // ═══════════════════════════════════════════════════════════════════

        public async Task<List<Bill>> GetAllBillsAsync()
        {
            var headers = await _db.BillHeaders.AsNoTracking().ToListAsync();
            var result = new List<Bill>();
            foreach (var h in headers)
                result.Add(await BuildBillAsync(h));
            return result.OrderByDescending(b => b.CreatedAt).ToList();
        }

        public async Task<List<Bill>> GetBillsByUserAsync(int userId)
        {
            var headers = await _db.BillHeaders.AsNoTracking()
                .Where(b => b.BillAcc == userId).ToListAsync();
            var result = new List<Bill>();
            foreach (var h in headers)
                result.Add(await BuildBillAsync(h));
            return result.OrderByDescending(b => b.CreatedAt).ToList();
        }

        public async Task<Bill?> GetBillByIdAsync(int billId)
        {
            var h = await _db.BillHeaders.AsNoTracking()
                .FirstOrDefaultAsync(b => b.BillId == billId);
            return h == null ? null : await BuildBillAsync(h);
        }

        public async Task AddBillAsync(Bill bill)
        {
            var now = bill.CreatedAt == default ? DateTime.UtcNow : bill.CreatedAt;

            var header = new BillHeaderRow
            {
                BillCode = bill.BillCode,
                BillAcc = bill.UserId,
                BillName = bill.UserName,
                BillEmail = bill.UserEmail,
                BillTx = bill.TransactionId,
                BillType = bill.Type,
                BillService = bill.ServiceName,
                BillStatus = bill.Status,
                BillNote = bill.Note
            };
            _db.BillHeaders.Add(header);
            await _db.SaveChangesAsync();
            bill.BillId = header.BillId;

            _db.BillDates.Add(new BillDateRow { BilldBill = header.BillId, BilldCreated = now });

            _db.BillDetails.Add(new BillDetailRow
            {
                BildBill = header.BillId,
                BildAmount = bill.Amount,
                BildCurrency = bill.CurrencyCode,
                BildVnd = bill.AmountInVND,
                BildBefore = bill.BalanceBefore,
                BildAfter = bill.BalanceAfter
            });
            await _db.SaveChangesAsync();
        }

        public async Task DeleteBillAsync(int billId)
        {
            var details = _db.BillDetails.Where(d => d.BildBill == billId);
            _db.BillDetails.RemoveRange(details);

            var dates = _db.BillDates.Where(d => d.BilldBill == billId);
            _db.BillDates.RemoveRange(dates);

            var header = await _db.BillHeaders.FindAsync(billId);
            if (header != null) _db.BillHeaders.Remove(header);

            await _db.SaveChangesAsync();
        }

        private async Task<Bill> BuildBillAsync(BillHeaderRow h)
        {
            var detail = await _db.BillDetails.AsNoTracking()
                .FirstOrDefaultAsync(d => d.BildBill == h.BillId);
            var date = await _db.BillDates.AsNoTracking()
                .FirstOrDefaultAsync(d => d.BilldBill == h.BillId);

            return new Bill
            {
                BillId = h.BillId,
                BillCode = h.BillCode,
                UserId = h.BillAcc,
                UserName = h.BillName,
                UserEmail = h.BillEmail,
                TransactionId = h.BillTx,
                Type = h.BillType,
                ServiceName = h.BillService,
                Status = h.BillStatus,
                Note = h.BillNote,
                CreatedAt = date?.BilldCreated ?? DateTime.UtcNow,
                Amount = detail?.BildAmount ?? "",
                CurrencyCode = detail?.BildCurrency ?? "",
                AmountInVND = detail?.BildVnd ?? "",
                BalanceBefore = detail?.BildBefore ?? "",
                BalanceAfter = detail?.BildAfter ?? ""
            };
        }

        // ═══════════════════════════════════════════════════════════════════
        // CURRENCY
        // ═══════════════════════════════════════════════════════════════════

        public async Task<List<Currency>> GetActiveCurrenciesAsync()
        {
            var rows = await _db.Currencies.AsNoTracking()
                .Where(c => c.IsActive).OrderBy(c => c.Code).ToListAsync();
            foreach (var c in rows)
                c.LastUpdated = await GetCurrencyUpdatedAsync(c.CurrencyId);
            return rows;
        }

        public async Task ReplaceAllCurrenciesAsync(IEnumerable<Currency> currencies)
        {
            var existing = await _db.Currencies.ToListAsync();
            foreach (var c in existing)
            {
                var dates = _db.CurrencyDates.Where(d => d.CurdCur == c.CurrencyId);
                _db.CurrencyDates.RemoveRange(dates);
            }
            _db.Currencies.RemoveRange(existing);
            await _db.SaveChangesAsync();

            foreach (var c in currencies)
            {
                _db.Currencies.Add(c);
                await _db.SaveChangesAsync();
                _db.CurrencyDates.Add(new CurrencyDateRow
                {
                    CurdCur = c.CurrencyId,
                    CurdUpdated = DateTime.UtcNow
                });
            }
            await _db.SaveChangesAsync();
        }

        public async Task<Currency?> GetCurrencyByCodeAsync(string code)
        {
            var c = await _db.Currencies.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == code && x.IsActive);
            if (c != null)
                c.LastUpdated = await GetCurrencyUpdatedAsync(c.CurrencyId);
            return c;
        }

        private async Task<DateTime> GetCurrencyUpdatedAsync(int curId)
        {
            var d = await _db.CurrencyDates.AsNoTracking()
                .FirstOrDefaultAsync(x => x.CurdCur == curId);
            return d?.CurdUpdated ?? DateTime.UtcNow;
        }

        // ═══════════════════════════════════════════════════════════════════
        // FAVORITE
        // ═══════════════════════════════════════════════════════════════════

        public async Task<List<Favorite>> GetAllFavoritesAsync()
        {
            var favs = await _db.Favorites.AsNoTracking().ToListAsync();
            foreach (var f in favs)
                f.AddedAt = await GetFavoriteAddedAsync(f.FavoriteId);
            return favs.OrderByDescending(f => f.AddedAt).ToList();
        }

        public async Task<List<Favorite>> GetFavoritesByUserAsync(int userId)
        {
            var favs = await _db.Favorites.AsNoTracking()
                .Where(f => f.UserId == userId).ToListAsync();
            foreach (var f in favs)
                f.AddedAt = await GetFavoriteAddedAsync(f.FavoriteId);
            return favs.OrderByDescending(f => f.AddedAt).ToList();
        }

        public Task<bool> FavoriteExistsAsync(int userId, int movieId)
            => _db.Favorites.AnyAsync(f => f.UserId == userId && f.MovieId == movieId);

        public async Task<Favorite?> GetFavoriteByIdAsync(int favoriteId)
        {
            var f = await _db.Favorites.FindAsync(favoriteId);
            if (f != null)
                f.AddedAt = await GetFavoriteAddedAsync(f.FavoriteId);
            return f;
        }

        public async Task AddFavoriteAsync(Favorite fav)
        {
            var now = fav.AddedAt == default ? DateTime.UtcNow : fav.AddedAt;
            _db.Favorites.Add(fav);
            await _db.SaveChangesAsync();
            _db.FavoriteDates.Add(new FavoriteDateRow { FavdFav = fav.FavoriteId, FavdAdded = now });
            await _db.SaveChangesAsync();
        }

        public async Task DeleteFavoriteAsync(Favorite fav)
        {
            var dates = _db.FavoriteDates.Where(d => d.FavdFav == fav.FavoriteId);
            _db.FavoriteDates.RemoveRange(dates);
            _db.Favorites.Remove(fav);
            await _db.SaveChangesAsync();
        }

        private async Task<DateTime> GetFavoriteAddedAsync(int favId)
        {
            var d = await _db.FavoriteDates.AsNoTracking()
                .FirstOrDefaultAsync(x => x.FavdFav == favId);
            return d?.FavdAdded ?? DateTime.UtcNow;
        }

        // ═══════════════════════════════════════════════════════════════════
        // WATCH HISTORY
        // ═══════════════════════════════════════════════════════════════════

        public async Task<List<WatchHistory>> GetAllWatchHistoriesAsync()
        {
            var rows = await _db.WatchHistories.AsNoTracking().ToListAsync();
            foreach (var w in rows)
                w.WatchedAt = await GetWatchedAtAsync(w.WatchHistoryId);
            return rows.OrderByDescending(w => w.WatchedAt).ToList();
        }

        public async Task<List<WatchHistory>> GetWatchHistoriesByUserAsync(int userId)
        {
            var rows = await _db.WatchHistories.AsNoTracking()
                .Where(w => w.UserId == userId).ToListAsync();
            foreach (var w in rows)
                w.WatchedAt = await GetWatchedAtAsync(w.WatchHistoryId);
            return rows.OrderByDescending(w => w.WatchedAt).ToList();
        }

        public async Task<WatchHistory?> GetWatchHistoryAsync(int userId, int movieId)
        {
            var w = await _db.WatchHistories
                .FirstOrDefaultAsync(x => x.UserId == userId && x.MovieId == movieId);
            if (w != null)
                w.WatchedAt = await GetWatchedAtAsync(w.WatchHistoryId);
            return w;
        }

        public async Task AddWatchHistoryAsync(WatchHistory wh)
        {
            var now = wh.WatchedAt == default ? DateTime.UtcNow : wh.WatchedAt;
            _db.WatchHistories.Add(wh);
            await _db.SaveChangesAsync();
            _db.WatchDates.Add(new WatchDateRow { WhdWh = wh.WatchHistoryId, WhdAt = now });
            await _db.SaveChangesAsync();
        }

        public async Task UpdateWatchHistoryAsync(WatchHistory wh)
        {
            _db.WatchHistories.Update(wh);
            var date = await _db.WatchDates
                .FirstOrDefaultAsync(d => d.WhdWh == wh.WatchHistoryId);
            if (date != null)
                date.WhdAt = wh.WatchedAt == default ? DateTime.UtcNow : wh.WatchedAt;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteWatchHistoryAsync(WatchHistory wh)
        {
            var dates = _db.WatchDates.Where(d => d.WhdWh == wh.WatchHistoryId);
            _db.WatchDates.RemoveRange(dates);
            _db.WatchHistories.Remove(wh);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteWatchHistoriesAsync(IEnumerable<WatchHistory> items)
        {
            foreach (var wh in items)
            {
                var dates = _db.WatchDates.Where(d => d.WhdWh == wh.WatchHistoryId);
                _db.WatchDates.RemoveRange(dates);
                _db.WatchHistories.Remove(wh);
            }
            await _db.SaveChangesAsync();
        }

        private async Task<DateTime> GetWatchedAtAsync(int whId)
        {
            var d = await _db.WatchDates.AsNoTracking()
                .FirstOrDefaultAsync(x => x.WhdWh == whId);
            return d?.WhdAt ?? DateTime.UtcNow;
        }

        // ═══════════════════════════════════════════════════════════════════
        // AUDIT LOG
        // ═══════════════════════════════════════════════════════════════════

        public async Task AddAuditLogAsync(AuditLog log)
        {
            var now = log.Timestamp == default ? DateTime.UtcNow : log.Timestamp;
            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync();
            _db.AuditDates.Add(new AuditDateRow { LogdLog = log.AuditLogId, LogdAt = now });
            await _db.SaveChangesAsync();
        }

        public async Task<List<AuditLog>> GetLatestAuditLogsAsync(int count, string? category = null)
        {
            var logs = string.IsNullOrEmpty(category)
                ? await _db.AuditLogs.AsNoTracking().ToListAsync()
                : await _db.AuditLogs.AsNoTracking()
                    .Where(x => x.Category == category).ToListAsync();

            foreach (var l in logs)
                l.Timestamp = await GetAuditTimestampAsync(l.AuditLogId);

            return logs.OrderByDescending(x => x.Timestamp).Take(count).ToList();
        }

        public async Task<int> CountAuditLogsAsync()
            => await _db.AuditLogs.CountAsync();

        public async Task DeleteOldAuditLogsAsync(int keepCount)
        {
            var all = await GetLatestAuditLogsAsync(int.MaxValue);
            if (all.Count <= keepCount) return;

            var toDelete = all.OrderBy(x => x.Timestamp).Take(all.Count - keepCount).ToList();
            foreach (var log in toDelete)
            {
                var dates = _db.AuditDates.Where(d => d.LogdLog == log.AuditLogId);
                _db.AuditDates.RemoveRange(dates);
                var row = await _db.AuditLogs.FindAsync(log.AuditLogId);
                if (row != null) _db.AuditLogs.Remove(row);
            }
            await _db.SaveChangesAsync();
        }

        private async Task<DateTime> GetAuditTimestampAsync(long logId)
        {
            var d = await _db.AuditDates.AsNoTracking()
                .FirstOrDefaultAsync(x => x.LogdLog == logId);
            return d?.LogdAt ?? DateTime.UtcNow;
        }

        // ═══════════════════════════════════════════════════════════════════
        // LOGIN ATTEMPT
        // ═══════════════════════════════════════════════════════════════════

        public async Task<LoginAttempt?> GetLoginAttemptAsync(string clientKey)
        {
            var la = await _db.LoginAttempts
                .FirstOrDefaultAsync(x => x.ClientKey == clientKey);
            if (la != null)
                await FillLoginDatesAsync(la);
            return la;
        }

        public async Task AddLoginAttemptAsync(LoginAttempt la)
        {
            _db.LoginAttempts.Add(la);
            await _db.SaveChangesAsync();
            _db.LoginDates.Add(new LoginDateRow
            {
                LadLa = la.Id,
                LadLast = la.LastAttempt,
                LadUntil = la.LockedUntil
            });
            await _db.SaveChangesAsync();
        }

        public async Task UpdateLoginAttemptAsync(LoginAttempt la)
        {
            var date = await _db.LoginDates.FirstOrDefaultAsync(d => d.LadLa == la.Id);
            if (date != null)
            {
                date.LadLast = la.LastAttempt;
                date.LadUntil = la.LockedUntil;
            }
            await _db.SaveChangesAsync();
        }

        private async Task FillLoginDatesAsync(LoginAttempt la)
        {
            var date = await _db.LoginDates.AsNoTracking()
                .FirstOrDefaultAsync(d => d.LadLa == la.Id);
            if (date != null)
            {
                la.LastAttempt = date.LadLast;
                la.LockedUntil = date.LadUntil;
            }
        }
    }
}
