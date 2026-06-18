using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webxemphim.Models;
using webxemphim.Services;

namespace webxemphim.Controllers
{
    public class WalletController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EncryptionService _enc;

        public WalletController(ApplicationDbContext context, EncryptionService enc)
        {
            _context = context;
            _enc     = enc;
        }

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để truy cập!";
                return RedirectToAction("Login", "Auth");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == int.Parse(userId));
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin user!";
                return RedirectToAction("Login", "Auth");
            }

            // Lấy transactions rồi giải mã để hiển thị
            var rawTxs = await _context.Transactions
                .Where(t => t.UserId == user.UserId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            // Giải mã các trường nhạy cảm trước khi đưa vào View
            var decryptedTxs = rawTxs.Select(t => new
            {
                t.TransactionId,
                t.Type,
                t.Description,
                t.Status,
                t.CreatedAt,
                Amount       = _enc.DecryptDecimal(t.Amount),
                CurrencyCode = _enc.Decrypt(t.CurrencyCode),
                AmountInVND  = _enc.DecryptDecimal(t.AmountInVND)
            }).ToList();

            ViewBag.Transactions = decryptedTxs;
            return View(user);
        }

        public async Task<IActionResult> Deposit()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để nạp tiền!";
                return RedirectToAction("Login", "Auth");
            }

            var currencies = await _context.Currencies.Where(c => c.IsActive).OrderBy(c => c.Code).ToListAsync();
            if (!currencies.Any())
            {
                await InitializeCurrencies();
                currencies = await _context.Currencies.Where(c => c.IsActive).OrderBy(c => c.Code).ToListAsync();
            }

            ViewBag.Currencies = currencies;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deposit(decimal amount, string currencyCode = "VND")
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để nạp tiền!";
                return RedirectToAction("Login", "Auth");
            }

            if (amount <= 0)
            {
                TempData["ErrorMessage"] = "Số tiền nạp phải lớn hơn 0!";
                return RedirectToAction("Deposit");
            }

            try
            {
                var user = await _context.Users.FindAsync(int.Parse(userId));
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin user!";
                    return RedirectToAction("Login", "Auth");
                }

                var currency = await _context.Currencies.FirstOrDefaultAsync(c => c.Code == currencyCode && c.IsActive);
                if (currency == null)
                {
                    TempData["ErrorMessage"] = "Loại tiền tệ không hợp lệ!";
                    return RedirectToAction("Deposit");
                }

                decimal amountInVND = amount * currency.ExchangeRate;

                // ── SECURITY: mã hóa các trường nhạy cảm trước khi lưu DB
                var transaction = new Transaction
                {
                    UserId       = user.UserId,
                    UserName     = user.UserName,
                    Type         = "Deposit",
                    Amount       = _enc.EncryptDecimal(amount),
                    CurrencyCode = _enc.Encrypt(currencyCode),
                    AmountInVND  = _enc.EncryptDecimal(amountInVND),
                    Description  = $"Nạp tiền: {amount:N2} {currency.Symbol} ({amountInVND:N0} VNĐ)",
                    Status       = "Completed",
                    CreatedAt    = DateTime.UtcNow
                };

                user.Balance += amountInVND;
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Nạp tiền thành công! {amount:N2} {currency.Symbol} = {amountInVND:N0} VNĐ";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
                return RedirectToAction("Deposit");
            }
        }

        public IActionResult BuyVIP()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để mua VIP!";
                return RedirectToAction("Login", "Auth");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuyVIP(string package)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để mua VIP!";
                return RedirectToAction("Login", "Auth");
            }

            try
            {
                var user = await _context.Users.FindAsync(int.Parse(userId));
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin user!";
                    return RedirectToAction("Login", "Auth");
                }

                decimal cost = 0;
                int days = 0;
                string description = "";

                switch (package)
                {
                    case "1month":  cost = 50000;  days = 30;  description = "Gói VIP 1 tháng"; break;
                    case "3months": cost = 120000; days = 90;  description = "Gói VIP 3 tháng"; break;
                    case "1year":   cost = 400000; days = 365; description = "Gói VIP 1 năm";   break;
                    default:
                        TempData["ErrorMessage"] = "Gói VIP không hợp lệ!";
                        return View();
                }

                if (user.Balance < cost)
                {
                    TempData["ErrorMessage"] = "Số dư không đủ để mua gói VIP này!";
                    return View();
                }

                // ── SECURITY: mã hóa các trường nhạy cảm trước khi lưu DB
                var transaction = new Transaction
                {
                    UserId       = user.UserId,
                    UserName     = user.UserName,
                    Type         = "Purchase",
                    Amount       = _enc.EncryptDecimal(-cost),
                    CurrencyCode = _enc.Encrypt("VND"),
                    AmountInVND  = _enc.EncryptDecimal(-cost),
                    Description  = description,
                    Status       = "Completed",
                    CreatedAt    = DateTime.UtcNow
                };

                user.Balance -= cost;
                user.ROLE = "User VIP";

                if (user.VIPExpiryDate.HasValue && user.VIPExpiryDate.Value > DateTime.UtcNow)
                    user.VIPExpiryDate = user.VIPExpiryDate.Value.AddDays(days);
                else
                    user.VIPExpiryDate = DateTime.UtcNow.AddDays(days);

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Mua VIP thành công! {description} - Hết hạn: {user.VIPExpiryDate.Value:dd/MM/yyyy}";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
                return View();
            }
        }

        private async Task InitializeCurrencies()
        {
            try
            {
                var existingCurrencies = await _context.Currencies.ToListAsync();
                if (existingCurrencies.Any())
                {
                    _context.Currencies.RemoveRange(existingCurrencies);
                    await _context.SaveChangesAsync();
                }

                var currencies = new List<Currency>
                {
                    new Currency { Code = "VND", Name = "Vietnamese Dong",   Symbol = "₫",   ExchangeRate = 1.0m,     IsActive = true },
                    new Currency { Code = "USD", Name = "US Dollar",         Symbol = "$",   ExchangeRate = 24500.0m, IsActive = true },
                    new Currency { Code = "EUR", Name = "Euro",              Symbol = "€",   ExchangeRate = 26800.0m, IsActive = true },
                    new Currency { Code = "JPY", Name = "Japanese Yen",      Symbol = "¥",   ExchangeRate = 165.0m,   IsActive = true },
                    new Currency { Code = "KRW", Name = "South Korean Won",  Symbol = "₩",   ExchangeRate = 18.2m,    IsActive = true },
                    new Currency { Code = "CNY", Name = "Chinese Yuan",      Symbol = "¥",   ExchangeRate = 3400.0m,  IsActive = true },
                    new Currency { Code = "SGD", Name = "Singapore Dollar",  Symbol = "S$",  ExchangeRate = 18200.0m, IsActive = true },
                    new Currency { Code = "THB", Name = "Thai Baht",         Symbol = "฿",   ExchangeRate = 680.0m,   IsActive = true },
                    new Currency { Code = "MYR", Name = "Malaysian Ringgit", Symbol = "RM",  ExchangeRate = 5200.0m,  IsActive = true },
                    new Currency { Code = "IDR", Name = "Indonesian Rupiah", Symbol = "Rp",  ExchangeRate = 1.55m,    IsActive = true },
                    new Currency { Code = "GBP", Name = "British Pound",     Symbol = "£",   ExchangeRate = 31000.0m, IsActive = true },
                    new Currency { Code = "AUD", Name = "Australian Dollar", Symbol = "A$",  ExchangeRate = 16200.0m, IsActive = true },
                    new Currency { Code = "CAD", Name = "Canadian Dollar",   Symbol = "C$",  ExchangeRate = 18000.0m, IsActive = true },
                    new Currency { Code = "CHF", Name = "Swiss Franc",       Symbol = "CHF", ExchangeRate = 28500.0m, IsActive = true },
                    new Currency { Code = "HKD", Name = "Hong Kong Dollar",  Symbol = "HK$", ExchangeRate = 3150.0m,  IsActive = true },
                    new Currency { Code = "TWD", Name = "Taiwan Dollar",     Symbol = "NT$", ExchangeRate = 780.0m,   IsActive = true },
                    new Currency { Code = "PHP", Name = "Philippine Peso",   Symbol = "₱",   ExchangeRate = 440.0m,   IsActive = true },
                    new Currency { Code = "INR", Name = "Indian Rupee",      Symbol = "₹",   ExchangeRate = 295.0m,   IsActive = true },
                    new Currency { Code = "BRL", Name = "Brazilian Real",    Symbol = "R$",  ExchangeRate = 5000.0m,  IsActive = true }
                };

                _context.Currencies.AddRange(currencies);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing currencies: {ex.Message}");
            }
        }
    }
}
