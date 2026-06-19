using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webxemphim.Models;
using webxemphim.Services;
using BCrypt.Net;

namespace webxemphim.Controllers
{
    public class UserController : Controller
    {
        private readonly ApplicationDbContext    _context;
        private readonly ILogger<UserController> _logger;
        private readonly EncryptionService       _enc;
        private readonly SecurityLogService      _secLog;

        public UserController(ApplicationDbContext context, ILogger<UserController> logger,
                              EncryptionService enc, SecurityLogService secLog)
        {
            _context = context;
            _logger  = logger;
            _enc     = enc;
            _secLog  = secLog;
        }

        // ── SECURITY: helper kiểm tra Admin tập trung
        private bool IsAdmin() => HttpContext.Session.GetString("UserRole") == "Admin";

        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này!";
                return RedirectToAction("Index", "Home");
            }

            // ── SECURITY: không trả về cột MK (password hash) ra view
            var users = await _context.Users
                .Select(u => new User
                {
                    UserId    = u.UserId,
                    UserName  = u.UserName,
                    EMAIL     = u.EMAIL,
                    ROLE      = u.ROLE,
                    Balance   = u.Balance,
                    VIPExpiryDate = u.VIPExpiryDate,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return View(users);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này!";
                return RedirectToAction("Index", "Home");
            }

            if (id == null) return NotFound();

            var user = await _context.Users.FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null) return NotFound();

            return View(user);
        }

        public IActionResult Create()
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này!";
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserName,MK,EMAIL,ROLE")] User user)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này!";
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                // ── SECURITY: hash mật khẩu trước khi lưu (BCrypt work factor 12)
                user.MK    = BCrypt.Net.BCrypt.HashPassword(user.MK, workFactor: 12);
                user.EMAIL = user.EMAIL.Trim().ToLowerInvariant();
                // ── SECURITY: mã hóa Phone/Address nếu có
                if (!string.IsNullOrEmpty(user.Phone))   user.Phone   = _enc.Encrypt(user.Phone.Trim());
                if (!string.IsNullOrEmpty(user.Address)) user.Address = _enc.Encrypt(user.Address.Trim());

                _context.Add(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation("SECURITY: Admin tạo user mới. Email={Email}", user.EMAIL);
                _secLog.LogRegister(user.UserName, HttpContext.Connection.RemoteIpAddress?.ToString() ?? "admin");
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này!";
                return RedirectToAction("Index", "Home");
            }

            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // ── SECURITY: xóa hash trước khi đưa vào form (không hiện hash ra UI)
            user.MK = string.Empty;
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UserId,UserName,MK,EMAIL,ROLE")] User user)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này!";
                return RedirectToAction("Index", "Home");
            }

            if (id != user.UserId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.Users.FindAsync(id);
                    if (existing == null) return NotFound();

                    existing.UserName = user.UserName;
                    existing.EMAIL    = user.EMAIL.Trim().ToLowerInvariant();
                    existing.ROLE     = user.ROLE;
                    // ── SECURITY: mã hóa lại Phone/Address nếu admin nhập mới
                    if (user.Phone   != null) existing.Phone   = string.IsNullOrWhiteSpace(user.Phone)   ? null : _enc.Encrypt(user.Phone.Trim());
                    if (user.Address != null) existing.Address = string.IsNullOrWhiteSpace(user.Address) ? null : _enc.Encrypt(user.Address.Trim());

                    // ── SECURITY: chỉ hash lại nếu admin nhập mật khẩu mới
                    if (!string.IsNullOrWhiteSpace(user.MK))
                    {
                        if (user.MK.Length < 8)
                        {
                            ModelState.AddModelError("MK", "Mật khẩu phải có ít nhất 8 ký tự!");
                            return View(user);
                        }
                        existing.MK = BCrypt.Net.BCrypt.HashPassword(user.MK, workFactor: 12);
                    }
                    // nếu bỏ trống → giữ nguyên mật khẩu cũ

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("SECURITY: Admin cập nhật user. UserId={Id}", id);
                    _secLog.LogEncrypt("User.Edit (Admin)", $"UserId={id}", "Phone/Address re-encrypted", 0);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.UserId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này!";
                return RedirectToAction("Index", "Home");
            }

            if (id == null) return NotFound();

            var user = await _context.Users.FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này!";
                return RedirectToAction("Index", "Home");
            }

            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                _logger.LogInformation("SECURITY: Admin xóa user. UserId={Id}", id);
                _secLog.LogLogin($"DELETE UserId={id}", "Admin-Action",
                    HttpContext.Connection.RemoteIpAddress?.ToString() ?? "admin", false);
            }

            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}
