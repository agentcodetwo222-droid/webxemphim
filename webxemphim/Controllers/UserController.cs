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
        private readonly AuditLogService         _audit;
        private readonly UserService             _userSvc;

        public UserController(ApplicationDbContext context, ILogger<UserController> logger,
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

            var users = await _context.Users
                .Select(u => new User
                {
                    UserId          = u.UserId,
                    UserName        = u.UserName,
                    EMAIL           = u.EMAIL,           // ciphertext — giai ma ben duoi
                    ROLE            = u.ROLE,
                    BalanceEncrypted= u.BalanceEncrypted, // can de giai ma
                    VIPExpiryDate   = u.VIPExpiryDate,
                    IsLocked        = u.IsLocked,
                    CreatedAt       = u.CreatedAt
                })
                .ToListAsync();

            // Giai ma EMAIL + Balance cho tung user
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
                TempData["ErrorMessage"] = "Ban khong co quyen truy cap trang nay!";
                return RedirectToAction("Index", "Home");
            }

            // Xoa ModelState errors cua cac truong se duoc xu ly trong code
            ModelState.Remove("BalanceEncrypted");
            ModelState.Remove("EMAIL");
            ModelState.Remove("MK");

            // Validate thu cong
            if (string.IsNullOrWhiteSpace(user.UserName))
                ModelState.AddModelError("UserName", "Ten hien thi khong duoc de trong.");
            if (string.IsNullOrWhiteSpace(user.EMAIL))
                ModelState.AddModelError("EMAIL", "Email khong duoc de trong.");
            if (string.IsNullOrWhiteSpace(user.ROLE))
                ModelState.AddModelError("ROLE", "Vui long chon vai tro.");

            // Password complexity check
            var pwErr = UserService.ValidatePassword(user.MK);
            if (pwErr != null)
                ModelState.AddModelError("MK", pwErr);

            if (!ModelState.IsValid)
                return View(user);

            // Kiem tra email trung
            var allUsers = await _context.Users.ToListAsync();
            if (_userSvc.EmailExists(allUsers, user.EMAIL))
            {
                ModelState.AddModelError("EMAIL", "Email nay da duoc su dung!");
                return View(user);
            }

            // Ma hoa truoc khi luu
            user.MK       = BCrypt.Net.BCrypt.HashPassword(user.MK, workFactor: 12);
            user.Balance  = 0;
            user.CreatedAt = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(user.Phone))   user.Phone   = _enc.Encrypt(user.Phone.Trim());
            if (!string.IsNullOrEmpty(user.Address)) user.Address = _enc.Encrypt(user.Address.Trim());
            _userSvc.EncryptForSave(user); // ma hoa EMAIL + BalanceEncrypted

            _context.Add(user);
            await _context.SaveChangesAsync();
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
                    existing.EMAIL    = _enc.Encrypt(user.EMAIL.Trim().ToLowerInvariant());
                    existing.ROLE     = user.ROLE;
                    if (user.Phone   != null) existing.Phone   = string.IsNullOrWhiteSpace(user.Phone)   ? null : _enc.Encrypt(user.Phone.Trim());
                    if (user.Address != null) existing.Address = string.IsNullOrWhiteSpace(user.Address) ? null : _enc.Encrypt(user.Address.Trim());

                    // ── Task 8: Tang SecurityStamp khi doi mat khau hoac doi role ──
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

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("SECURITY: Admin cap nhat user. UserId={Id}", id);
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
                _logger.LogInformation("SECURITY: Admin xoa user. UserId={Id}", id);
                _audit.LogAdminLockUser(AdminId(), AdminName(), id, GetIp());
            }

            return RedirectToAction(nameof(Index));
        }

        // ── Task 8: Admin khoa / mo khoa tai khoan ───────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(int id)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Ban khong co quyen!";
                return RedirectToAction("Index", "Home");
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Khong cho khoa chinh minh
            if (id == AdminId())
            {
                TempData["ErrorMessage"] = "Khong the khoa chinh tai khoan cua minh!";
                return RedirectToAction(nameof(Index));
            }

            user.IsLocked = !user.IsLocked;
            // Tang SecurityStamp → session cu se bi invalidate ngay
            user.SecurityStamp++;
            await _context.SaveChangesAsync();

            _audit.LogAdminLockUser(AdminId(), AdminName(), id, GetIp());
            _secLog.LogLogin($"TOGGLE_LOCK UserId={id} IsLocked={user.IsLocked}",
                "Admin-Action", GetIp(), false);

            TempData["SuccessMessage"] = user.IsLocked
                ? $"Da khoa tai khoan #{id}"
                : $"Da mo khoa tai khoan #{id}";
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}
