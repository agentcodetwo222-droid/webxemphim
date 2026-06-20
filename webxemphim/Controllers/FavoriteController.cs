using Microsoft.AspNetCore.Mvc;
using webxemphim.Models;
using webxemphim.Services;

namespace webxemphim.Controllers
{
    public class FavoriteController : Controller
    {
        private readonly SchemaDataService _schema;
        private readonly EncryptionService _enc;
        private readonly ILogger<FavoriteController> _logger;

        public FavoriteController(SchemaDataService schema, EncryptionService enc, ILogger<FavoriteController> logger)
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

        private FavoriteViewModel Decrypt(Favorite f) => new FavoriteViewModel
        {
            FavoriteId = f.FavoriteId,
            UserId     = f.UserId,
            UserName   = f.UserName,
            MovieId    = f.MovieId,
            MovieTitle = f.MovieTitle,
            MovieImage = _enc.Decrypt(f.MovieImage),
            AddedAt    = f.AddedAt
        };

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem phim yêu thích!";
                return RedirectToAction("Login", "Auth");
            }

            var raw = IsAdmin()
                ? await _schema.GetAllFavoritesAsync()
                : await _schema.GetFavoritesByUserAsync(userId.Value);

            ViewBag.IsAdmin = IsAdmin();
            return View(raw.Select(Decrypt).ToList());
        }

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

            if (await _schema.FavoriteExistsAsync(userId.Value, movieId))
            {
                TempData["ErrorMessage"] = "Phim đã có trong danh sách yêu thích!";
                return RedirectToAction("Detail", "Movie", new { id = movieId });
            }

            var movie = await _schema.GetMovieByIdAsync(movieId);
            if (movie == null) return NotFound();

            var decryptedImage = _enc.Decrypt(movie.ImageUrl);

            var fav = new Favorite
            {
                UserId     = userId.Value,
                UserName   = HttpContext.Session.GetString("UserName") ?? string.Empty,
                MovieId    = movieId,
                MovieTitle = movie.Title,
                MovieImage = _enc.Encrypt(decryptedImage),
                AddedAt    = DateTime.UtcNow
            };

            await _schema.AddFavoriteAsync(fav);

            _logger.LogInformation("Favorite added. UserId={UserId} MovieId={MovieId}", userId, movieId);
            TempData["SuccessMessage"] = $"Đã thêm \"{movie.Title}\" vào yêu thích!";
            return RedirectToAction("Detail", "Movie", new { id = movieId });
        }

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

            var fav = await _schema.GetFavoriteByIdAsync(favoriteId);
            if (fav == null) return NotFound();

            if (!IsAdmin() && fav.UserId != userId)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xóa mục này!";
                return RedirectToAction(nameof(Index));
            }

            await _schema.DeleteFavoriteAsync(fav);

            _logger.LogInformation("Favorite removed. FavoriteId={Id} UserId={UserId}", favoriteId, userId);
            TempData["SuccessMessage"] = "Đã xóa khỏi danh sách yêu thích!";
            return RedirectToAction(nameof(Index));
        }
    }

    public class FavoriteViewModel
    {
        public int      FavoriteId { get; set; }
        public int      UserId     { get; set; }
        public string   UserName   { get; set; } = string.Empty;
        public int      MovieId    { get; set; }
        public string   MovieTitle { get; set; } = string.Empty;
        public string   MovieImage { get; set; } = string.Empty;
        public DateTime AddedAt    { get; set; }
    }
}
