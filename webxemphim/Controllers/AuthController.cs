using Microsoft.AspNetCore.Mvc;
using webxemphim.Models;
using webxemphim.Services;

namespace webxemphim.Controllers
{
    public class AuthController : Controller
    {
        private readonly SchemaDataService       _schema;
        private readonly ILogger<AuthController>   _logger;
        private readonly EncryptionService         _enc;
        private readonly SecurityLogService        _secLog;
        private readonly AuditLogService           _audit;
        private readonly UserService               _userSvc;

        private const int MaxLoginAttempts  = 5;
        private const int LockoutMinutes    = 15;

        public AuthController(SchemaDataService schema, ILogger<AuthController> logger,
                              EncryptionService enc, SecurityLogService secLog,
                              AuditLogService audit, UserService userSvc)
        {
            _schema  = schema;
            _logger  = logger;
            _enc     = enc;
            _secLog  = secLog;
            _audit   = audit;
            _userSvc = userSvc;
        }

        public IActionResult Login() => View();

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

            if (await IsLockedOutAsync(clientKey))
            {
                _secLog.LogLockout(normalized, GetClientIp());
                _audit.LogLockout(normalized, GetClientIp());
                TempData["ErrorMessage"] = $"Tai khoan bi khoa do dang nhap sai qua {MaxLoginAttempts} lan. Thu lai sau {LockoutMinutes} phut.";
                return View();
            }

            var allUsers = await _schema.GetAllUsersAsync();
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
            HttpContext.Session.SetString("UserId",        user!.UserId.ToString());
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

        public IActionResult Register() => View();

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
            var allUsers = await _schema.GetAllUsersAsync();
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

            newUser.EMAIL = normalizedEmail;
            _userSvc.EncryptForSave(newUser);

            try
            {
                await _schema.AddUserAsync(newUser);
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

        private async Task<bool> IsLockedOutAsync(string clientKey)
        {
            var record = await _schema.GetLoginAttemptAsync(clientKey);
            if (record == null) return false;
            if (!record.IsLocked) return false;
            if (record.LockedUntil.HasValue && DateTime.UtcNow > record.LockedUntil.Value)
            {
                record.IsLocked    = false;
                record.FailCount   = 0;
                record.LockedUntil = null;
                await _schema.UpdateLoginAttemptAsync(record);
                return false;
            }
            return true;
        }

        private async Task RecordFailedAttemptAsync(string clientKey)
        {
            var record = await _schema.GetLoginAttemptAsync(clientKey);
            if (record == null)
            {
                record = new LoginAttempt { ClientKey = clientKey, FailCount = 1, LastAttempt = DateTime.UtcNow };
                await _schema.AddLoginAttemptAsync(record);
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
                await _schema.UpdateLoginAttemptAsync(record);
            }
        }

        private async Task ClearFailedAttemptsAsync(string clientKey)
        {
            var record = await _schema.GetLoginAttemptAsync(clientKey);
            if (record != null)
            {
                record.FailCount   = 0;
                record.IsLocked    = false;
                record.LockedUntil = null;
                await _schema.UpdateLoginAttemptAsync(record);
            }
        }

        private string GetClientIp()
            => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
