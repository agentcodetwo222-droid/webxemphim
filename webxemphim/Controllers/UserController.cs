using Microsoft.AspNetCore.Mvc;
using webxemphim.Models;
using webxemphim.Services;

namespace webxemphim.Controllers
{
    public class UserController : Controller
    {
        private readonly SchemaDataService       _schema;
        private readonly ILogger<UserController> _logger;
        private readonly EncryptionService       _enc;
        private readonly SecurityLogService      _secLog;
        private readonly AuditLogService         _audit;
        private readonly UserService             _userSvc;

        public UserController(SchemaDataService schema, ILogger<UserController> logger,
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

        private bool IsAdmin() => HttpContext.Session.GetString("UserRole") == "Admin";
        private int  AdminId() => int.TryParse(HttpContext.Session.GetString("UserId"), out var id) ? id : 0;
        private string AdminName() => HttpContext.Session.GetString("UserName") ?? "Admin";
        private string GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này!";
                return RedirectToAction("Index", "Home");
            }

            var users = await _schema.GetAllUsersAsync();
            foreach (var u in users)
                _userSvc.Decrypt(u);

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

            var user = await _schema.GetUserByIdAsync(id.Value);
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
                TempData["ErrorMessage"] = "Ban khong co quyen truy cap trang nay!";
                return RedirectToAction("Index", "Home");
            }

            ModelState.Remove("BalanceEncrypted");
            ModelState.Remove("EMAIL");
            ModelState.Remove("MK");

            if (string.IsNullOrWhiteSpace(user.UserName))
                ModelState.AddModelError("UserName", "Ten hien thi khong duoc de trong.");
            if (string.IsNullOrWhiteSpace(user.EMAIL))
                ModelState.AddModelError("EMAIL", "Email khong duoc de trong.");
            if (string.IsNullOrWhiteSpace(user.ROLE))
                ModelState.AddModelError("ROLE", "Vui long chon vai tro.");

            var pwErr = UserService.ValidatePassword(user.MK);
            if (pwErr != null)
                ModelState.AddModelError("MK", pwErr);

            if (!ModelState.IsValid)
                return View(user);

            var allUsers = await _schema.GetAllUsersAsync();
            if (_userSvc.EmailExists(allUsers, user.EMAIL))
            {
                ModelState.AddModelError("EMAIL", "Email nay da duoc su dung!");
                return View(user);
            }

            user.MK        = BCrypt.Net.BCrypt.HashPassword(user.MK, workFactor: 12);
            user.Balance   = 0;
            user.CreatedAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(user.Phone))   user.Phone   = _enc.Encrypt(user.Phone.Trim());
            if (!string.IsNullOrEmpty(user.Address)) user.Address = _enc.Encrypt(user.Address.Trim());
            _userSvc.EncryptForSave(user);

            await _schema.AddUserAsync(user);
            _logger.LogInformation("SECURITY: Admin tao user moi. UserId={Id}", user.UserId);
            _secLog.LogRegister(user.UserName, GetIp());
            _audit.LogRegister(user.UserId, user.UserName, GetIp());
            TempData["SuccessMessage"] = $"Da tao tai khoan {user.UserName} voi role {user.ROLE}!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này!";
                return RedirectToAction("Index", "Home");
            }

            if (id == null) return NotFound();

            var user = await _schema.GetUserByIdAsync(id.Value);
            if (user == null) return NotFound();

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
                var existing = await _schema.GetUserByIdAsync(id);
                if (existing == null) return NotFound();

                existing.UserName = user.UserName;
                existing.EMAIL    = _enc.Encrypt(user.EMAIL.Trim().ToLowerInvariant());
                existing.ROLE     = user.ROLE;
                if (user.Phone   != null) existing.Phone   = string.IsNullOrWhiteSpace(user.Phone)   ? null : _enc.Encrypt(user.Phone.Trim());
                if (user.Address != null) existing.Address = string.IsNullOrWhiteSpace(user.Address) ? null : _enc.Encrypt(user.Address.Trim());

                bool stampChanged = false;
                if (!string.IsNullOrWhiteSpace(user.MK))
                {
                    var pwErr = UserService.ValidatePassword(user.MK);
                    if (pwErr != null)
                    {
                        ModelState.AddModelError("MK", pwErr);
                        return View(user);
                    }
                    existing.MK = BCrypt.Net.BCrypt.HashPassword(user.MK, workFactor: 12);
                    stampChanged = true;
                }
                if (existing.ROLE != user.ROLE) stampChanged = true;
                if (stampChanged) existing.SecurityStamp++;

                await _schema.UpdateUserAsync(existing);
                _logger.LogInformation("SECURITY: Admin cap nhat user. UserId={Id}", id);
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

            var user = await _schema.GetUserByIdAsync(id.Value);
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

            if (await _schema.UserExistsAsync(id))
            {
                await _schema.DeleteUserAsync(id);
                _logger.LogInformation("SECURITY: Admin xoa user. UserId={Id}", id);
                _audit.LogAdminLockUser(AdminId(), AdminName(), id, GetIp());
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(int id)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Ban khong co quyen!";
                return RedirectToAction("Index", "Home");
            }

            var user = await _schema.GetUserByIdAsync(id);
            if (user == null) return NotFound();

            if (id == AdminId())
            {
                TempData["ErrorMessage"] = "Khong the khoa chinh tai khoan cua minh!";
                return RedirectToAction(nameof(Index));
            }

            user.IsLocked = !user.IsLocked;
            user.SecurityStamp++;
            await _schema.UpdateUserAsync(user);

            _audit.LogAdminLockUser(AdminId(), AdminName(), id, GetIp());
            _secLog.LogLogin($"TOGGLE_LOCK UserId={id} IsLocked={user.IsLocked}",
                "Admin-Action", GetIp(), false);

            TempData["SuccessMessage"] = user.IsLocked
                ? $"Da khoa tai khoan #{id}"
                : $"Da mo khoa tai khoan #{id}";
            return RedirectToAction(nameof(Index));
        }
    }
}
