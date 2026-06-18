using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webxemphim.Models;
using webxemphim.Services;

namespace webxemphim.Controllers
{
    public class WatchHistoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EncryptionService _enc;
        private readonly ILogger<WatchHistoryController> _logger;

        public WatchHistoryController(ApplicationDbContext context, EncryptionService enc, ILogger<WatchHistoryController> logger)
        {
            _context = context;
            _enc     = enc;
            _logger  = logger;
        }

        private int? GetCurrentUserId()
        {
            var s = HttpContext.Session.GetString("UserId");
            return int.TryParse(s, out var id) ? id : null;
        }

        private bool IsAdmin() => HttpContext.Session.GetString("UserRole") == "Admin";

        // ── Helper: giải mã MovieImage trước khi đưa ra View ───────────────
        private WatchHistoryViewModel Decrypt(WatchHistory w) => new WatchHistoryViewModel
        {
            WatchHistoryId = w.WatchHistoryId,
            UserId         = w.UserId,
            UserName       = w.UserName,
            MovieId        = w.MovieId,
            MovieTitle     = w.MovieTitle,
            MovieImage     = _enc.Decrypt(w.MovieImage),  // ── SECURITY: giải mã AES-256-GCM
            WatchedAt      = w.WatchedAt,
            WatchDuration  = w.WatchDuration,
            IsCompleted    = w.IsCompleted
        };

        // ── GET: WatchHistory/Index — lịch sử xem phim ────────────────────
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem lịch sử!";
                return RedirectToAction("Login", "Auth");
            }

            List<WatchHistory> raw;
            if (IsAdmin())
                raw = await _context.WatchHistories.OrderByDescending(w => w.WatchedAt).ToListAsync();
            else
                raw = await _context.WatchHistories
                          .Where(w => w.UserId == userId)
                          .OrderByDescending(w => w.WatchedAt)
                          .ToListAsync();

            ViewBag.IsAdmin = IsAdmin();
            return View(raw.Select(Decrypt).ToList());
        }

        // ── POST: WatchHistory/Record — ghi lại lịch sử xem ───────────────
        // Gọi khi user bắt đầu hoặc tiếp tục xem phim
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Record(int movieId, int watchDuration = 0, bool isCompleted = false)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Json(new { success = false });

            var movie = await _context.Movies.FindAsync(movieId);
            if (movie == null) return Json(new { success = false });

            // ── SECURITY: lấy ảnh từ DB (đang là ciphertext) → giải mã → mã hóa lại lưu vào WatchHistory
            var decryptedImage = _enc.Decrypt(movie.ImageUrl);

            // Kiểm tra đã có record chưa — nếu có thì update
            var existing = await _context.WatchHistories
                .FirstOrDefaultAsync(w => w.UserId == userId && w.MovieId == movieId);

            if (existing != null)
            {
                existing.WatchedAt     = DateTime.UtcNow;
                existing.WatchDuration = watchDuration;
                existing.IsCompleted   = isCompleted;
                // Cập nhật lại MovieImage (mã hóa mới) phòng khi ảnh phim thay đổi
                existing.MovieImage    = _enc.Encrypt(decryptedImage);  // ── SECURITY: AES-256-GCM
            }
            else
            {
                var history = new WatchHistory
                {
                    UserId        = userId.Value,
                    UserName      = HttpContext.Session.GetString("UserName") ?? string.Empty,
                    MovieId       = movieId,
                    MovieTitle    = movie.Title,
                    MovieImage    = _enc.Encrypt(decryptedImage),  // ── SECURITY: mã hóa AES-256-GCM
                    WatchedAt     = DateTime.UtcNow,
                    WatchDuration = watchDuration,
                    IsCompleted   = isCompleted
                };
                _context.WatchHistories.Add(history);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ── POST: WatchHistory/Delete — xóa 1 mục lịch sử ────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int watchHistoryId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập!";
                return RedirectToAction("Login", "Auth");
            }

            var record = await _context.WatchHistories.FindAsync(watchHistoryId);
            if (record == null) return NotFound();

            // ── SECURITY: user chỉ xóa được của mình
            if (!IsAdmin() && record.UserId != userId)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xóa mục này!";
                return RedirectToAction(nameof(Index));
            }

            _context.WatchHistories.Remove(record);
            await _context.SaveChangesAsync();

            _logger.LogInformation("WatchHistory deleted. Id={Id} UserId={UserId}", watchHistoryId, userId);
            TempData["SuccessMessage"] = "Đã xóa khỏi lịch sử xem phim!";
            return RedirectToAction(nameof(Index));
        }

        // ── POST: WatchHistory/ClearAll — xóa toàn bộ lịch sử của user ───
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearAll()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập!";
                return RedirectToAction("Login", "Auth");
            }

            var records = await _context.WatchHistories
                .Where(w => w.UserId == userId)
                .ToListAsync();

            _context.WatchHistories.RemoveRange(records);
            await _context.SaveChangesAsync();

            _logger.LogInformation("WatchHistory cleared. UserId={UserId} Count={Count}", userId, records.Count);
            TempData["SuccessMessage"] = $"Đã xóa {records.Count} mục lịch sử xem phim!";
            return RedirectToAction(nameof(Index));
        }
    }

    // ── ViewModel: dữ liệu đã giải mã để dùng trong View ──────────────────
    public class WatchHistoryViewModel
    {
        public int      WatchHistoryId { get; set; }
        public int      UserId         { get; set; }
        public string   UserName       { get; set; } = string.Empty;
        public int      MovieId        { get; set; }
        public string   MovieTitle     { get; set; } = string.Empty;
        public string   MovieImage     { get; set; } = string.Empty; // plaintext sau khi giải mã
        public DateTime WatchedAt      { get; set; }
        public int      WatchDuration  { get; set; }
        public bool     IsCompleted    { get; set; }
    }
}
