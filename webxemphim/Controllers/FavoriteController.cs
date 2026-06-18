using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webxemphim.Models;
using webxemphim.Services;

namespace webxemphim.Controllers
{
    public class FavoriteController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EncryptionService _enc;
        private readonly ILogger<FavoriteController> _logger;

        public FavoriteController(ApplicationDbContext context, EncryptionService enc, ILogger<FavoriteController> logger)
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
        private FavoriteViewModel Decrypt(Favorite f) => new FavoriteViewModel
        {
            FavoriteId = f.FavoriteId,
            UserId     = f.UserId,
            UserName   = f.UserName,
            MovieId    = f.MovieId,
            MovieTitle = f.MovieTitle,
            MovieImage = _enc.Decrypt(f.MovieImage),  // ── SECURITY: giải mã AES-256-GCM
            AddedAt    = f.AddedAt
        };

        // ── GET: Favorite/Index — danh sách phim yêu thích của user ────────
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem phim yêu thích!";
                return RedirectToAction("Login", "Auth");
            }

            List<Favorite> raw;
            if (IsAdmin())
                raw = await _context.Favorites.OrderByDescending(f => f.AddedAt).ToListAsync();
            else
                raw = await _context.Favorites
                          .Where(f => f.UserId == userId)
                          .OrderByDescending(f => f.AddedAt)
                          .ToListAsync();

            ViewBag.IsAdmin = IsAdmin();
            return View(raw.Select(Decrypt).ToList());
        }

        // ── POST: Favorite/Add — thêm phim vào yêu thích ──────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int movieId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập!";
                return RedirectToAction("Login", "Auth");
            }

            // Kiểm tra đã thêm chưa
            var exists = await _context.Favorites
                .AnyAsync(f => f.UserId == userId && f.MovieId == movieId);
            if (exists)
            {
                TempData["ErrorMessage"] = "Phim đã có trong danh sách yêu thích!";
                return RedirectToAction("Detail", "Movie", new { id = movieId });
            }

            // Lấy thông tin phim để lưu kèm (denormalized)
            var movie = await _context.Movies.FindAsync(movieId);
            if (movie == null) return NotFound();

            // ── SECURITY: MovieImage lấy từ DB đang là ciphertext → giải mã để dùng đúng
            var decryptedImage = _enc.Decrypt(movie.ImageUrl);

            var fav = new Favorite
            {
                UserId     = userId.Value,
                UserName   = HttpContext.Session.GetString("UserName") ?? string.Empty,
                MovieId    = movieId,
                MovieTitle = movie.Title,
                MovieImage = _enc.Encrypt(decryptedImage),  // ── SECURITY: mã hóa lại AES-256-GCM
                AddedAt    = DateTime.UtcNow
            };

            _context.Favorites.Add(fav);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Favorite added. UserId={UserId} MovieId={MovieId}", userId, movieId);
            TempData["SuccessMessage"] = $"Đã thêm \"{movie.Title}\" vào yêu thích!";
            return RedirectToAction("Detail", "Movie", new { id = movieId });
        }

        // ── POST: Favorite/Remove — xóa khỏi yêu thích ───────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int favoriteId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập!";
                return RedirectToAction("Login", "Auth");
            }

            var fav = await _context.Favorites.FindAsync(favoriteId);
            if (fav == null) return NotFound();

            // ── SECURITY: user chỉ xóa được của mình
            if (!IsAdmin() && fav.UserId != userId)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xóa mục này!";
                return RedirectToAction(nameof(Index));
            }

            _context.Favorites.Remove(fav);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Favorite removed. FavoriteId={Id} UserId={UserId}", favoriteId, userId);
            TempData["SuccessMessage"] = "Đã xóa khỏi danh sách yêu thích!";
            return RedirectToAction(nameof(Index));
        }
    }

    // ── ViewModel: dữ liệu đã giải mã để dùng trong View ──────────────────
    public class FavoriteViewModel
    {
        public int      FavoriteId { get; set; }
        public int      UserId     { get; set; }
        public string   UserName   { get; set; } = string.Empty;
        public int      MovieId    { get; set; }
        public string   MovieTitle { get; set; } = string.Empty;
        public string   MovieImage { get; set; } = string.Empty; // plaintext sau khi giải mã
        public DateTime AddedAt    { get; set; }
    }
}
