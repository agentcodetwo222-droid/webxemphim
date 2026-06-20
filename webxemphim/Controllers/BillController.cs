using Microsoft.AspNetCore.Mvc;
using webxemphim.Models;
using webxemphim.Services;

namespace webxemphim.Controllers
{
    public class BillController : Controller
    {
        private readonly SchemaDataService _schema;
        private readonly EncryptionService _enc;
        private readonly ILogger<BillController> _logger;

        public BillController(SchemaDataService schema, EncryptionService enc, ILogger<BillController> logger)
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

        private BillViewModel Decrypt(Bill b) => new BillViewModel
        {
            BillId        = b.BillId,
            BillCode      = _enc.Decrypt(b.BillCode),
            UserId        = b.UserId,
            UserName      = b.UserName,
            UserEmail     = _enc.Decrypt(b.UserEmail),
            TransactionId = _enc.Decrypt(b.TransactionId),
            Type          = b.Type,
            ServiceName   = b.ServiceName,
            Amount        = _enc.DecryptDecimal(b.Amount),
            CurrencyCode  = _enc.Decrypt(b.CurrencyCode),
            AmountInVND   = _enc.DecryptDecimal(b.AmountInVND),
            BalanceBefore = _enc.DecryptDecimal(b.BalanceBefore),
            BalanceAfter  = _enc.DecryptDecimal(b.BalanceAfter),
            CreatedAt     = b.CreatedAt,
            Status        = b.Status,
            Note          = b.Note
        };

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập!";
                return RedirectToAction("Login", "Auth");
            }

            var raw = IsAdmin()
                ? await _schema.GetAllBillsAsync()
                : await _schema.GetBillsByUserAsync(userId.Value);

            var vms = raw.Select(Decrypt).ToList();
            ViewBag.IsAdmin = IsAdmin();
            return View(vms);
        }

        public async Task<IActionResult> Details(int? id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập!";
                return RedirectToAction("Login", "Auth");
            }

            if (id == null) return NotFound();

            var bill = await _schema.GetBillByIdAsync(id.Value);
            if (bill == null) return NotFound();

            if (!IsAdmin() && bill.UserId != userId)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xem hóa đơn này!";
                return RedirectToAction(nameof(Index));
            }

            return View(Decrypt(bill));
        }

        public IActionResult Create()
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập!";
                return RedirectToAction("Index", "Home");
            }
            return View(new BillViewModel { CreatedAt = DateTime.UtcNow });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BillViewModel vm)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập!";
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid) return View(vm);

            var bill = new Bill
            {
                BillCode      = _enc.Encrypt(vm.BillCode),
                UserId        = vm.UserId,
                UserName      = vm.UserName,
                UserEmail     = _enc.Encrypt(vm.UserEmail),
                TransactionId = _enc.Encrypt(vm.TransactionId),
                Type          = vm.Type,
                ServiceName   = vm.ServiceName,
                Amount        = _enc.EncryptDecimal(vm.Amount),
                CurrencyCode  = _enc.Encrypt(vm.CurrencyCode),
                AmountInVND   = _enc.EncryptDecimal(vm.AmountInVND),
                BalanceBefore = _enc.EncryptDecimal(vm.BalanceBefore),
                BalanceAfter  = _enc.EncryptDecimal(vm.BalanceAfter),
                CreatedAt     = DateTime.UtcNow,
                Status        = vm.Status,
                Note          = vm.Note
            };

            await _schema.AddBillAsync(bill);

            _logger.LogInformation("SECURITY: Bill created. BillId={Id} UserId={UserId}", bill.BillId, bill.UserId);
            TempData["SuccessMessage"] = "Tạo hóa đơn thành công!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập!";
                return RedirectToAction("Index", "Home");
            }

            if (id == null) return NotFound();

            var bill = await _schema.GetBillByIdAsync(id.Value);
            if (bill == null) return NotFound();

            return View(Decrypt(bill));
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập!";
                return RedirectToAction("Index", "Home");
            }

            if (await _schema.GetBillByIdAsync(id) != null)
            {
                await _schema.DeleteBillAsync(id);
                _logger.LogInformation("SECURITY: Bill deleted. BillId={Id}", id);
                TempData["SuccessMessage"] = "Xóa hóa đơn thành công!";
            }

            return RedirectToAction(nameof(Index));
        }
    }

    public class BillViewModel
    {
        public int     BillId        { get; set; }
        public string  BillCode      { get; set; } = string.Empty;
        public int     UserId        { get; set; }
        public string  UserName      { get; set; } = string.Empty;
        public string  UserEmail     { get; set; } = string.Empty;
        public string  TransactionId { get; set; } = string.Empty;
        public string  Type          { get; set; } = string.Empty;
        public string  ServiceName   { get; set; } = string.Empty;
        public decimal Amount        { get; set; }
        public string  CurrencyCode  { get; set; } = "VND";
        public decimal AmountInVND   { get; set; }
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter  { get; set; }
        public DateTime CreatedAt    { get; set; }
        public string  Status        { get; set; } = "Completed";
        public string  Note          { get; set; } = string.Empty;
    }
}
