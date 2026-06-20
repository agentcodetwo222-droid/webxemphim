using Microsoft.AspNetCore.Mvc;
using webxemphim.Models;
using webxemphim.Services;

namespace webxemphim.Controllers
{
    public class WatchHistoryController : Controller
    {
        private readonly SchemaDataService _schema;
        private readonly EncryptionService _enc;
        private readonly ILogger<WatchHistoryController> _logger;

        public WatchHistoryController(SchemaDataService schema, EncryptionService enc, ILogger<WatchHistoryController> logger)
        {
            _schema = schema;
            _enc    = enc;
            _logger = logger;
        }

        private int? GetCurrentUserId()
        {
            var s = HttpContext.Session.GetString("UserId");
            return int.TryParse(s, out var id) ? id : null;
        }
        private bool IsAdmin() => HttpContext.Session.GetString("UserRole") == "Admin";

        private WatchHistoryViewModel Decrypt(WatchHistory w) => new WatchHistoryViewModel
        {
            WatchHistoryId = w.WatchHistoryId,
            UserId         = w.UserId,
            UserName       = w.UserName,
            MovieId        = w.MovieId,
            MovieTitle     = w.MovieTitle,
            MovieImage     = _enc.Decrypt(w.MovieImage),
            WatchedAt      = w.WatchedAt,
            WatchDuration  = w.WatchDuration,
            IsCompleted    = w.IsCompleted
        };

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem lịch sử!";
                return RedirectToAction("Login", "Auth");
            }

            var raw = IsAdmin()
                ? await _schema.GetAllWatchHistoriesAsync()
                : await _schema.GetWatchHistoriesByUserAsync(userId.Value);

            ViewBag.IsAdmin = IsAdmin();
            return View(raw.Select(Decrypt).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Record(int movieId, int watchDuration = 0, bool isCompleted = false)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Json(new { success = false });

            var movie = await _schema.GetMovieByIdAsync(movieId);
            if (movie == null) return Json(new { success = false });

            var decryptedImage = _enc.Decrypt(movie.ImageUrl);
            var existing = await _schema.GetWatchHistoryAsync(userId.Value, movieId);

            if (existing != null)
            {
                existing.WatchedAt     = DateTime.UtcNow;
                existing.WatchDuration = watchDuration;
                existing.IsCompleted   = isCompleted;
                existing.MovieImage    = _enc.Encrypt(decryptedImage);
                await _schema.UpdateWatchHistoryAsync(existing);
            }
            else
            {
                var history = new WatchHistory
                {
                    UserId        = userId.Value,
                    UserName      = HttpContext.Session.GetString("UserName") ?? string.Empty,
                    MovieId       = movieId,
                    MovieTitle    = movie.Title,
                    MovieImage    = _enc.Encrypt(decryptedImage),
                    WatchedAt     = DateTime.UtcNow,
                    WatchDuration = watchDuration,
                    IsCompleted   = isCompleted
                };
                await _schema.AddWatchHistoryAsync(history);
            }

            return Json(new { success = true });
        }

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

            var record = await _schema.GetWatchHistoryByIdAsync(watchHistoryId);
            if (record == null) return NotFound();

            if (!IsAdmin() && record.UserId != userId)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xóa mục này!";
                return RedirectToAction(nameof(Index));
            }

            await _schema.DeleteWatchHistoryAsync(record);

            _logger.LogInformation("WatchHistory deleted. Id={Id} UserId={UserId}", watchHistoryId, userId);
            TempData["SuccessMessage"] = "Đã xóa khỏi lịch sử xem phim!";
            return RedirectToAction(nameof(Index));
        }

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

            var records = await _schema.GetWatchHistoriesByUserAsync(userId.Value);
            await _schema.DeleteWatchHistoriesAsync(records);

            _logger.LogInformation("WatchHistory cleared. UserId={UserId} Count={Count}", userId, records.Count);
            TempData["SuccessMessage"] = $"Đã xóa {records.Count} mục lịch sử xem phim!";
            return RedirectToAction(nameof(Index));
        }
    }

    public class WatchHistoryViewModel
    {
        public int      WatchHistoryId { get; set; }
        public int      UserId         { get; set; }
        public string   UserName       { get; set; } = string.Empty;
        public int      MovieId        { get; set; }
        public string   MovieTitle     { get; set; } = string.Empty;
        public string   MovieImage     { get; set; } = string.Empty;
        public DateTime WatchedAt      { get; set; }
        public int      WatchDuration  { get; set; }
        public bool     IsCompleted    { get; set; }
    }
}
