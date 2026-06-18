using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webxemphim.Models;
using webxemphim.Services;

namespace webxemphim.Controllers
{
    public class MovieController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EncryptionService _enc;

        public MovieController(ApplicationDbContext context, EncryptionService enc)
        {
            _context = context;
            _enc     = enc;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userRole = HttpContext.Session.GetString("UserRole");

            await EnsureSampleMoviesExist();

            List<Movie> movies;
            if (userRole == "Admin")
                movies = await _context.Movies.ToListAsync();
            else
                movies = await _context.Movies.Where(m => m.IsAvailable).ToListAsync();

            ViewBag.UserRole = userRole;
            return View(DecryptMovies(movies));
        }

        [HttpGet]
        [Route("Movie/Detail/{id?}")]
        public async Task<IActionResult> Detail(int? id)
        {
            Movie? movie;

            if (id.HasValue)
                movie = await _context.Movies.FindAsync(id.Value);
            else
                movie = await _context.Movies.FirstOrDefaultAsync();

            if (movie == null)
            {
                TempData["ErrorMessage"] = "Không có phim nào để xem!";
                return RedirectToAction("Index");
            }

            var userRole = HttpContext.Session.GetString("UserRole");
            var userId   = HttpContext.Session.GetString("UserId");

            // ── SECURITY: kiểm tra VIP đúng — chặn cả user chưa đăng nhập
            if (movie.IsVipOnly)
            {
                bool hasAccess = !string.IsNullOrEmpty(userId)
                                 && (userRole == "Admin" || userRole == "User VIP");
                if (!hasAccess)
                {
                    TempData["ErrorMessage"] = "Bạn cần đăng nhập và nâng cấp lên User VIP để xem phim này!";
                    return RedirectToAction("Index");
                }
            }

            ViewBag.UserRole = userRole;
            return View("Detail", DecryptMovie(movie));
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này!";
                return RedirectToAction("Index");
            }

            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,Genre,Country,Year,ImageUrl,VideoUrl,IsVipOnly,IsAvailable,CategoryName")] Movie movie,
            IFormFile? imageFile, IFormFile? videoFile, string? imageType, string? videoType)
        {
            try
            {
                var userRole = HttpContext.Session.GetString("UserRole");
                if (string.IsNullOrEmpty(userRole) || userRole != "Admin")
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này!";
                    return RedirectToAction("Index");
                }

                if (imageType == "file" && imageFile != null && imageFile.Length > 0)
                {
                    if (imageFile.Length > 5 * 1024 * 1024)
                    {
                        TempData["ErrorMessage"] = "Hình ảnh không được vượt quá 5MB!";
                        ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync();
                        return View(movie);
                    }

                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        TempData["ErrorMessage"] = "Chỉ chấp nhận file hình ảnh: JPG, PNG, GIF!";
                        ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync();
                        return View(movie);
                    }

                    var imageFileName = Guid.NewGuid().ToString() + fileExtension;
                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "images");
                    if (!Directory.Exists(imagePath)) Directory.CreateDirectory(imagePath);

                    var imageFilePath = Path.Combine(imagePath, imageFileName);
                    using (var stream = new FileStream(imageFilePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    movie.ImageUrl = _enc.Encrypt("/uploads/images/" + imageFileName);
                }

                if (videoType == "file" && videoFile != null && videoFile.Length > 0)
                {
                    if (videoFile.Length > 500 * 1024 * 1024)
                    {
                        TempData["ErrorMessage"] = "Video không được vượt quá 500MB!";
                        ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync();
                        return View(movie);
                    }

                    var allowedExtensions = new[] { ".mp4", ".avi", ".mov", ".mkv", ".wmv", ".flv" };
                    var fileExtension = Path.GetExtension(videoFile.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        TempData["ErrorMessage"] = "Chỉ chấp nhận file video: MP4, AVI, MOV, MKV, WMV, FLV!";
                        ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync();
                        return View(movie);
                    }

                    var videoFileName = Guid.NewGuid().ToString() + fileExtension;
                    var videoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "videos");
                    if (!Directory.Exists(videoPath)) Directory.CreateDirectory(videoPath);

                    var videoFilePath = Path.Combine(videoPath, videoFileName);
                    using (var stream = new FileStream(videoFilePath, FileMode.Create))
                    {
                        await videoFile.CopyToAsync(stream);
                    }

                    movie.VideoUrl = _enc.Encrypt("/uploads/videos/" + videoFileName);
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    TempData["ErrorMessage"] = $"Lỗi validation: {string.Join(", ", errors)}";
                    ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync();
                    return View(movie);
                }

                // ── SECURITY: mã hóa ImageUrl/VideoUrl nếu là URL thủ công (không phải upload)
                if (!movie.ImageUrl.StartsWith("AQID") && !string.IsNullOrEmpty(movie.ImageUrl))
                    movie.ImageUrl = _enc.Encrypt(movie.ImageUrl);
                if (!movie.VideoUrl.StartsWith("AQID") && !string.IsNullOrEmpty(movie.VideoUrl))
                    movie.VideoUrl = _enc.Encrypt(movie.VideoUrl);

                _context.Add(movie);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm phim thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi tạo phim: {ex.Message}";
                ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync();
            }

            return View(movie);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này!";
                return RedirectToAction("Index");
            }

            if (id == null) return NotFound();

            var movie = await _context.Movies.FindAsync(id);
            if (movie == null) return NotFound();

            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync();
            return View(movie);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MovieId,Title,Description,Genre,Country,Year,ImageUrl,VideoUrl,IsVipOnly,IsAvailable,CategoryName")] Movie movie)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này!";
                return RedirectToAction("Index");
            }

            if (id != movie.MovieId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // ── SECURITY: mã hóa ImageUrl/VideoUrl trước khi lưu
                    if (!string.IsNullOrEmpty(movie.ImageUrl))
                        movie.ImageUrl = _enc.Encrypt(movie.ImageUrl);
                    if (!string.IsNullOrEmpty(movie.VideoUrl))
                        movie.VideoUrl = _enc.Encrypt(movie.VideoUrl);

                    _context.Update(movie);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật phim thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MovieExists(movie.MovieId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(movie);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này!";
                return RedirectToAction("Index");
            }

            if (id == null) return NotFound();

            var movie = await _context.Movies.FirstOrDefaultAsync(m => m.MovieId == id);
            if (movie == null) return NotFound();

            return View(movie);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này!";
                return RedirectToAction("Index");
            }

            var movie = await _context.Movies.FindAsync(id);
            if (movie != null)
            {
                _context.Movies.Remove(movie);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa phim thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool MovieExists(int id)
        {
            return _context.Movies.Any(e => e.MovieId == id);
        }

        [HttpGet]
        [Route("Movie/Player/{id?}")]
        public async Task<IActionResult> Player(int? id)
        {
            Movie? movie;

            if (id.HasValue)
                movie = await _context.Movies.FindAsync(id.Value);
            else
                movie = await _context.Movies.FirstOrDefaultAsync();

            if (movie == null)
            {
                TempData["ErrorMessage"] = "Không có phim nào để xem!";
                return RedirectToAction("Index");
            }

            var userRole = HttpContext.Session.GetString("UserRole");
            var userId   = HttpContext.Session.GetString("UserId");

            // ── SECURITY: kiểm tra VIP đúng — chặn cả user chưa đăng nhập
            if (movie.IsVipOnly)
            {
                bool hasAccess = !string.IsNullOrEmpty(userId)
                                 && (userRole == "Admin" || userRole == "User VIP");
                if (!hasAccess)
                {
                    TempData["ErrorMessage"] = "Bạn cần đăng nhập và nâng cấp lên User VIP để xem phim này!";
                    return RedirectToAction("Index");
                }
            }

            ViewBag.UserRole = userRole;
            ViewBag.Movie = movie;
            return View(DecryptMovie(movie));
        }

        [HttpGet]
        public async Task<IActionResult> Series()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            await EnsureSampleMoviesExist();

            List<Movie> seriesMovies;
            if (userRole == "Admin")
                seriesMovies = await _context.Movies.Where(m => m.Genre.Contains("Phim Bộ")).ToListAsync();
            else
                seriesMovies = await _context.Movies.Where(m => m.Genre.Contains("Phim Bộ") && m.IsAvailable).ToListAsync();

            ViewBag.UserRole = userRole;
            return View(DecryptMovies(seriesMovies));
        }

        private async Task EnsureSampleMoviesExist()
        {
            if (!await _context.Movies.AnyAsync(m => m.Title.Contains("Tazan")))
            {
                _context.Movies.Add(new Movie
                {
                    Title = "Tazan Nhí: Cuộc Phiêu Lưu Kỳ Thú",
                    Description = "Tazan Nhí là một cậu bé dũng cảm sống trong rừng rậm.",
                    Genre = "Phim Bộ", Country = "Việt Nam", Year = "2024",
                    ImageUrl = _enc.Encrypt("/images/tazannhi/hqdefault.jpg"),
                    VideoUrl = _enc.Encrypt("/videos/tazannhi/tazannhitap1.mp4"),
                    IsVipOnly = false, IsAvailable = true
                });
                await _context.SaveChangesAsync();
            }

            if (!await _context.Movies.AnyAsync(m => m.Title.Contains("Conan")))
            {
                _context.Movies.Add(new Movie
                {
                    Title = "Thám Tử Conan: Tập 1",
                    Description = "Kudo Shinichi là một thám tử trung học nổi tiếng.",
                    Genre = "Phim Bộ", Country = "Nhật Bản", Year = "1996",
                    ImageUrl = _enc.Encrypt("/images/conan/anh-conan-ngau.jpg"),
                    VideoUrl = _enc.Encrypt("/videos/conan/conan-ep1.mp4"),
                    IsVipOnly = false, IsAvailable = true
                });
                await _context.SaveChangesAsync();
            }

            if (!await _context.Movies.AnyAsync(m => m.Title.Contains("One Piece")))
            {
                _context.Movies.Add(new Movie
                {
                    Title = "One Piece: Tập 1 - Tôi là Luffy",
                    Description = "Monkey D. Luffy là một cậu bé mơ ước trở thành Vua Hải Tặc.",
                    Genre = "Phim Bộ", Country = "Nhật Bản", Year = "1999",
                    ImageUrl = _enc.Encrypt("/images/conan/images.webp"),
                    VideoUrl = _enc.Encrypt("/videos/conan/conan-ep1.mp4"),
                    IsVipOnly = false, IsAvailable = true
                });
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>Giải mã ImageUrl và VideoUrl của movie trước khi đưa ra View.</summary>
        private Movie DecryptMovie(Movie m)
        {
            m.ImageUrl = _enc.Decrypt(m.ImageUrl);
            m.VideoUrl = _enc.Decrypt(m.VideoUrl);
            return m;
        }

        private List<Movie> DecryptMovies(List<Movie> movies)
        {
            foreach (var m in movies) DecryptMovie(m);
            return movies;
        }
    }
}
