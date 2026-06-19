using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webxemphim.Models;
using webxemphim.Services;

namespace webxemphim.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext    _context;
        private readonly ILogger<AuthController> _logger;
        private readonly EncryptionService       _enc;
        private readonly SecurityLogService      _secLog;

        private static readonly Dictionary<string, (int Count, DateTime LastAttempt)> _loginAttempts = new();
        private const int MaxLoginAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        public AuthController(ApplicationDbContext context, ILogger<AuthController> logger,
                              EncryptionService enc, SecurityLogService secLog)
        {
            _context = context;
            _logger  = logger;
            _enc     = enc;
            _secLog  = secLog;
        }

        // GET: Auth/Login
        public IActionResult Login() => View();

        // POST: Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string identifier, string password)
        {
            if (string.IsNullOrEmpty(identifier) || string.IsNullOrEmpty(password))
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin!";
                return View();
            }

            var normalizedIdentifier = identifier.Trim().ToLowerInvariant();
            var clientKey = $"{GetClientIp()}:{normalizedIdentifier}";

            if (IsLockedOut(clientKey))
            {
                _logger.LogWarning("SECURITY: Lockout. Key={Key}", clientKey);
                _secLog.LogLockout(normalizedIdentifier, GetClientIp());
                TempData["ErrorMessage"] = $"Tài khoản bị khóa tạm thời do đăng nhập sai quá {MaxLoginAttempts} lần. Thử lại sau {LockoutDuration.TotalMinutes} phút.";
                return View();
            }

            // Tìm user theo email HOẶC username (không phân biệt hoa/thường)
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.EMAIL == normalizedIdentifier ||
                u.UserName.ToLower() == normalizedIdentifier);

            bool passwordValid = user != null && BCrypt.Net.BCrypt.Verify(password, user.MK);

            if (!passwordValid)
            {
                RecordFailedAttempt(clientKey);
                _logger.LogWarning("SECURITY: Login failed. Identifier={Id} IP={IP}", normalizedIdentifier, GetClientIp());
                _secLog.LogLogin(normalizedIdentifier, "", GetClientIp(), false);
                TempData["ErrorMessage"] = "Tên đăng nhập / email hoặc mật khẩu không đúng!";
                return View();
            }

            ClearFailedAttempts(clientKey);
            HttpContext.Session.Clear();
            HttpContext.Session.SetString("UserId",   user!.UserId.ToString());
            HttpContext.Session.SetString("UserName", user.UserName);
            HttpContext.Session.SetString("UserRole", user.ROLE);

            _logger.LogInformation("SECURITY: Login OK. UserId={Id} Role={Role}", user.UserId, user.ROLE);
            _secLog.LogLogin(user.UserName, user.ROLE, GetClientIp(), true);
            TempData["SuccessMessage"] = "Đăng nhập thành công!";
            return RedirectToAction("Index", "Home");
        }

        // GET: Auth/Register
        public IActionResult Register() => View();

        // POST: Auth/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            string firstName, string lastName, string email,
            string password, string confirmPassword,
            string? phone = null, string? address = null)
        {
            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) ||
                string.IsNullOrEmpty(email)     || string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(confirmPassword))
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin!";
                return View();
            }

            if (password.Length < 8)
            {
                TempData["ErrorMessage"] = "Mật khẩu phải có ít nhất 8 ký tự!";
                return View();
            }

            if (password != confirmPassword)
            {
                TempData["ErrorMessage"] = "Mật khẩu xác nhận không khớp!";
                return View();
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();
            if (await _context.Users.AnyAsync(u => u.EMAIL == normalizedEmail))
            {
                TempData["ErrorMessage"] = "Email đã được sử dụng!";
                return View();
            }

            var newUser = new User
            {
                UserName = $"{firstName.Trim()} {lastName.Trim()}",
                EMAIL    = normalizedEmail,
                MK       = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12),
                ROLE     = "User",
                // ── SECURITY: mã hóa AES-256-GCM các trường PII
                Phone    = string.IsNullOrEmpty(phone)   ? null : _enc.Encrypt(phone.Trim()),
                Address  = string.IsNullOrEmpty(address) ? null : _enc.Encrypt(address.Trim())
            };

            try
            {
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
                _logger.LogInformation("SECURITY: New account created. Email={Email}", normalizedEmail);
                _secLog.LogRegister(newUser.UserName, GetClientIp());
                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SECURITY: Register error.");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi đăng ký!";
                return View();
            }
        }

        // GET: Auth/Logout
        public IActionResult Logout()
        {
            var userName = HttpContext.Session.GetString("UserName") ?? "?";
            _logger.LogInformation("SECURITY: Logout. UserId={Id}", HttpContext.Session.GetString("UserId") ?? "?");
            _secLog.LogLogout(userName, GetClientIp());
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Đã đăng xuất thành công!";
            return RedirectToAction("Index", "Home");
        }

        // ── Lockout helpers ──────────────────────────────────────────────────
        private bool IsLockedOut(string key)
        {
            if (!_loginAttempts.TryGetValue(key, out var r)) return false;
            if (r.Count < MaxLoginAttempts) return false;
            if (DateTime.UtcNow - r.LastAttempt > LockoutDuration) { _loginAttempts.Remove(key); return false; }
            return true;
        }
        private void RecordFailedAttempt(string key)
        {
            _loginAttempts[key] = _loginAttempts.TryGetValue(key, out var r)
                ? (r.Count + 1, DateTime.UtcNow)
                : (1, DateTime.UtcNow);
        }
        private void ClearFailedAttempts(string key) => _loginAttempts.Remove(key);
        private string GetClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
