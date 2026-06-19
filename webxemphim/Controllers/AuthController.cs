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
        private readonly AuditLogService         _audit;
        private readonly UserService             _userSvc;

        private const int MaxLoginAttempts  = 5;
        private const int LockoutMinutes    = 15;

        public AuthController(ApplicationDbContext context, ILogger<AuthController> logger,
                              EncryptionService enc, SecurityLogService secLog,
                              AuditLogService audit, UserService userSvc)
        {
            _context = context;
            _logger  = logger;
            _enc     = enc;
            _secLog  = secLog;
            _audit   = audit;
            _userSvc = userSvc;
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
                TempData["ErrorMessage"] = "Vui long dien day du thong tin!";
                return View();
            }

            var normalized = identifier.Trim().ToLowerInvariant();
            var clientKey  = $"{GetClientIp()}:{normalized}";

            // ── Task 6: Lockout DB ──────────────────────────────────────────
            if (await IsLockedOutAsync(clientKey))
            {
                _secLog.LogLockout(normalized, GetClientIp());
                _audit.LogLockout(normalized, GetClientIp());
                TempData["ErrorMessage"] = $"Tai khoan bi khoa do dang nhap sai qua {MaxLoginAttempts} lan. Thu lai sau {LockoutMinutes} phut.";
                return View();
            }

            // Tim user theo email HOAC username
            // Task 2: Email da ma hoa → phai load tat ca roi giai ma so sanh
            var allUsers = await _context.Users.ToListAsync();
            var user = allUsers.FirstOrDefault(u =>
            {
                try
                {
                    var decEmail = _enc.Decrypt(u.EMAIL);
                    return decEmail.Trim().ToLowerInvariant() == normalized
                        || u.UserName.ToLowerInvariant() == normalized;
                }
                catch { return u.UserName.ToLowerInvariant() == normalized; }
            });

            // ── Task 8: Kiem tra tai khoan bi khoa boi Admin ─────────────
            if (user != null && user.IsLocked)
            {
                TempData["ErrorMessage"] = "Tai khoan cua ban da bi khoa. Vui long lien he quan tri vien.";
                return View();
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            bool passwordValid = user != null && BCrypt.Net.BCrypt.Verify(password, user.MK);
            sw.Stop();

            if (!passwordValid)
            {
                await RecordFailedAttemptAsync(clientKey);
                _secLog.LogLogin(normalized, "", GetClientIp(), false);
                _secLog.LogLoginEncryption(normalized, GetClientIp(), false, "", sw.Elapsed.TotalMilliseconds);
                _secLog.LogVpnLogin(normalized, GetClientIp(), false, sw.Elapsed.TotalMilliseconds);
                _audit.LogLogin(null, normalized, GetClientIp(), false);
                TempData["ErrorMessage"] = "Ten dang nhap / email hoac mat khau khong dung!";
                return View();
            }

            await ClearFailedAttemptsAsync(clientKey);
            _userSvc.Decrypt(user!);

            HttpContext.Session.Clear();
            HttpContext.Session.SetString("UserId",        user.UserId.ToString());
            HttpContext.Session.SetString("UserName",      user.UserName);
            HttpContext.Session.SetString("UserRole",      user.ROLE);
            HttpContext.Session.SetString("SecurityStamp", user.SecurityStamp.ToString());

            _secLog.LogLogin(user.UserName, user.ROLE, GetClientIp(), true);
            _secLog.LogLoginEncryption(user.UserName, GetClientIp(), true, user.MK, sw.Elapsed.TotalMilliseconds);
            _secLog.LogSessionToken(user.UserName, GetClientIp(), HttpContext.Session.Id, user.ROLE);
            _secLog.LogVpnLogin(user.UserName, GetClientIp(), true, sw.Elapsed.TotalMilliseconds);
            _audit.LogLogin(user.UserId, user.UserName, GetClientIp(), true);
            TempData["SuccessMessage"] = "Dang nhap thanh cong!";
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
                string.IsNullOrEmpty(email)     || string.IsNullOrEmpty(password))
            {
                TempData["ErrorMessage"] = "Vui long dien day du thong tin!";
                return View();
            }

            // ── Task 4: Password complexity ────────────────────────────────
            var pwErr = UserService.ValidatePassword(password);
            if (pwErr != null)
            {
                TempData["ErrorMessage"] = pwErr;
                return View();
            }

            if (password != confirmPassword)
            {
                TempData["ErrorMessage"] = "Mat khau xac nhan khong khop!";
                return View();
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();

            // ── Task 2: Kiem tra email trung (phai giai ma roi so sanh) ───
            var allUsers = await _context.Users.ToListAsync();
            if (_userSvc.EmailExists(allUsers, normalizedEmail))
            {
                TempData["ErrorMessage"] = "Email da duoc su dung!";
                return View();
            }

            var newUser = new User
            {
                UserName = $"{firstName.Trim()} {lastName.Trim()}",
                ROLE     = "User",
                MK       = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12),
                Phone    = string.IsNullOrEmpty(phone)   ? null : _enc.Encrypt(phone.Trim()),
                Address  = string.IsNullOrEmpty(address) ? null : _enc.Encrypt(address.Trim()),
                CreatedAt = DateTime.UtcNow,
                Balance  = 0
            };

            // ── Task 2: Ma hoa Email + Balance truoc khi luu ──────────────
            newUser.EMAIL = normalizedEmail; // set truoc de EncryptForSave ma hoa
            _userSvc.EncryptForSave(newUser);

            try
            {
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
                _secLog.LogRegister(newUser.UserName, GetClientIp());
                _secLog.LogRegisterEncryption(
                    newUser.UserName, GetClientIp(),
                    newUser.EMAIL, newUser.BalanceEncrypted);
                _secLog.LogVpnRegister(newUser.UserName, GetClientIp(), 0);
                _audit.LogRegister(newUser.UserId, newUser.UserName, GetClientIp());
                TempData["SuccessMessage"] = "Dang ky thanh cong! Vui long dang nhap.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Register error");
                TempData["ErrorMessage"] = "Co loi xay ra khi dang ky!";
                return View();
            }
        }

        // GET: Auth/Logout
        public IActionResult Logout()
        {
            var userId   = HttpContext.Session.GetString("UserId");
            var userName = HttpContext.Session.GetString("UserName") ?? "?";
            _secLog.LogLogout(userName, GetClientIp());
            if (int.TryParse(userId, out var uid))
                _audit.LogLogout(uid, userName, GetClientIp());
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Da dang xuat thanh cong!";
            return RedirectToAction("Index", "Home");
        }

        // ── Task 6: Lockout DB helpers ───────────────────────────────────────

        private async Task<bool> IsLockedOutAsync(string clientKey)
        {
            var record = await _context.LoginAttempts
                .FirstOrDefaultAsync(x => x.ClientKey == clientKey);
            if (record == null) return false;
            if (!record.IsLocked) return false;
            if (record.LockedUntil.HasValue && DateTime.UtcNow > record.LockedUntil.Value)
            {
                // Het thoi gian khoa → reset
                record.IsLocked   = false;
                record.FailCount  = 0;
                record.LockedUntil = null;
                await _context.SaveChangesAsync();
                return false;
            }
            return true;
        }

        private async Task RecordFailedAttemptAsync(string clientKey)
        {
            var record = await _context.LoginAttempts
                .FirstOrDefaultAsync(x => x.ClientKey == clientKey);
            if (record == null)
            {
                record = new LoginAttempt { ClientKey = clientKey, FailCount = 1, LastAttempt = DateTime.UtcNow };
                _context.LoginAttempts.Add(record);
            }
            else
            {
                record.FailCount++;
                record.LastAttempt = DateTime.UtcNow;
                if (record.FailCount >= MaxLoginAttempts)
                {
                    record.IsLocked    = true;
                    record.LockedUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                }
            }
            await _context.SaveChangesAsync();
        }

        private async Task ClearFailedAttemptsAsync(string clientKey)
        {
            var record = await _context.LoginAttempts
                .FirstOrDefaultAsync(x => x.ClientKey == clientKey);
            if (record != null)
            {
                record.FailCount   = 0;
                record.IsLocked    = false;
                record.LockedUntil = null;
                await _context.SaveChangesAsync();
            }
        }

        private string GetClientIp()
            => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
