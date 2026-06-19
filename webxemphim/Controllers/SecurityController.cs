using Microsoft.AspNetCore.Mvc;
using webxemphim.Models;
using webxemphim.Services;

namespace webxemphim.Controllers
{
    public class SecurityController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment  _env;
        private readonly EncryptionService    _enc;
        private readonly SecurityLogService   _secLog;

        public SecurityController(ApplicationDbContext context, IWebHostEnvironment env,
                                  EncryptionService enc, SecurityLogService secLog)
        {
            _context = context;
            _env     = env;
            _enc     = enc;
            _secLog  = secLog;
        }

        // ── GET /Security ─────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Index()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này!";
                return RedirectToAction("Index", "Home");
            }

            var model = new SecurityStatusViewModel
            {
                IsHttps           = Request.IsHttps,
                ForwardedProto    = Request.Headers["X-Forwarded-Proto"].ToString(),
                Host              = Request.Host.ToString(),
                Scheme            = Request.Scheme,
                Environment       = _env.EnvironmentName,
                IsProduction      = _env.IsProduction(),
                HasHstsHeader     = Response.Headers.ContainsKey("Strict-Transport-Security"),
                HasCspHeader      = Response.Headers.ContainsKey("Content-Security-Policy"),
                HasXFrameHeader   = Response.Headers.ContainsKey("X-Frame-Options"),
                HasNoSniffHeader  = Response.Headers.ContainsKey("X-Content-Type-Options"),
                HasReferrerHeader = Response.Headers.ContainsKey("Referrer-Policy"),
                HasPermHeader     = Response.Headers.ContainsKey("Permissions-Policy"),
                HasCoopHeader     = Response.Headers.ContainsKey("Cross-Origin-Opener-Policy"),
                HasCorpHeader     = Response.Headers.ContainsKey("Cross-Origin-Resource-Policy"),
                SessionId         = HttpContext.Session.Id,
                SessionCookieName = HttpContext.Request.Cookies.Keys
                                        .FirstOrDefault(k => k.Contains("Session")) ?? "N/A",
                DatabaseConnected = CheckDatabase(),
                ClientIp          = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "N/A",
                UserAgent         = Request.Headers["User-Agent"].ToString(),
                CheckedAt         = DateTime.UtcNow,
            };

            return View(model);
        }

        // ── GET /Security/Demo ────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Demo()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này!";
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // ── POST /Security/Encrypt — mã hóa real-time ────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Encrypt([FromBody] EncryptRequest req)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin") return Forbid();
            if (string.IsNullOrEmpty(req.Plaintext)) return BadRequest(new { error = "Plaintext trống" });

            var sw        = System.Diagnostics.Stopwatch.StartNew();
            var encrypted = _enc.Encrypt(req.Plaintext);
            sw.Stop();

            var raw    = Convert.FromBase64String(encrypted);
            var nonce  = raw[..12];
            var tag    = raw[12..28];
            var cipher = raw[28..];

            return Ok(new
            {
                plaintext    = req.Plaintext,
                encrypted,
                nonce_hex    = Convert.ToHexString(nonce),
                nonce_b64    = Convert.ToBase64String(nonce),
                tag_hex      = Convert.ToHexString(tag),
                tag_b64      = Convert.ToBase64String(tag),
                cipher_hex   = Convert.ToHexString(cipher),
                cipher_b64   = Convert.ToBase64String(cipher),
                input_bytes  = System.Text.Encoding.UTF8.GetByteCount(req.Plaintext),
                output_bytes = raw.Length,
                elapsed_ms   = Math.Round(sw.Elapsed.TotalMilliseconds, 3),
                timestamp    = DateTime.UtcNow.ToString("HH:mm:ss.fff")
            });
        }

        // ── POST /Security/Decrypt — giải mã real-time ───────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Decrypt([FromBody] DecryptRequest req)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin") return Forbid();
            if (string.IsNullOrEmpty(req.Ciphertext)) return BadRequest(new { error = "Ciphertext trống" });

            try
            {
                var sw        = System.Diagnostics.Stopwatch.StartNew();
                var decrypted = _enc.Decrypt(req.Ciphertext);
                sw.Stop();

                return Ok(new
                {
                    decrypted,
                    verified   = true,
                    elapsed_ms = Math.Round(sw.Elapsed.TotalMilliseconds, 3),
                    timestamp  = DateTime.UtcNow.ToString("HH:mm:ss.fff")
                });
            }
            catch
            {
                return Ok(new
                {
                    decrypted  = (string?)null,
                    verified   = false,
                    error      = "❌ Giải mã thất bại — dữ liệu bị sửa đổi hoặc sai key!"
                });
            }
        }

        // ── POST /Security/SimulateToken — mô phỏng toàn bộ luồng ───────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SimulateToken([FromBody] EncryptRequest req)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin") return Forbid();

            var plaintext = req.Plaintext ?? "test-data";
            var steps     = new List<object>();

            // Bước 1: Sinh Nonce ngẫu nhiên
            var nonce = new byte[12];
            System.Security.Cryptography.RandomNumberGenerator.Fill(nonce);
            steps.Add(new
            {
                step  = 1,
                label = "Sinh Nonce ngẫu nhiên (96-bit)",
                value = Convert.ToHexString(nonce),
                note  = "Mỗi lần mã hóa dùng nonce khác nhau — tuyệt đối không tái dùng với AES-GCM"
            });

            // Bước 2: Chuyển plaintext → bytes
            var inputBytes = System.Text.Encoding.UTF8.GetBytes(plaintext);
            steps.Add(new
            {
                step  = 2,
                label = "Chuyển Plaintext → UTF-8 Bytes",
                value = Convert.ToHexString(inputBytes),
                note  = $"{inputBytes.Length} bytes"
            });

            // Bước 3: Mã hóa AES-256-GCM
            var encrypted = _enc.Encrypt(plaintext);
            var raw       = Convert.FromBase64String(encrypted);
            steps.Add(new
            {
                step  = 3,
                label = "Mã hóa AES-256-GCM (Key 256-bit + Nonce)",
                value = Convert.ToHexString(raw[28..]),
                note  = $"Ciphertext {raw.Length - 28} bytes — không thể đọc nếu không có Key"
            });

            // Bước 4: Authentication Tag
            var authTag = raw[12..28];
            steps.Add(new
            {
                step  = 4,
                label = "Tạo Authentication Tag (128-bit)",
                value = Convert.ToHexString(authTag),
                note  = "Tag xác thực toàn vẹn — sửa 1 bit trong DB thì tag không khớp, decrypt thất bại"
            });

            // Bước 5: Đóng gói lưu DB
            steps.Add(new
            {
                step  = 5,
                label = "Đóng gói → Base64 lưu Database",
                value = encrypted,
                note  = $"Nonce(12B) + Tag(16B) + Ciphertext({raw.Length - 28}B) = {raw.Length}B → Base64"
            });

            // Bước 6: Giải mã khi đọc
            var decrypted = _enc.Decrypt(encrypted);
            steps.Add(new
            {
                step  = 6,
                label = "Giải mã khi đọc ra View",
                value = decrypted,
                note  = "Base64 → tách Nonce + Tag + Ciphertext → verify Tag → AES decrypt → UTF-8"
            });

            return Ok(new { steps, timestamp = DateTime.UtcNow.ToString("HH:mm:ss.fff") });
        }

        // ── GET /Security/Monitor ─────────────────────────────────────────────
        [HttpGet]
        public IActionResult Monitor()
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
            {
                TempData["ErrorMessage"] = "Ban khong co quyen truy cap trang nay!";
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // ── GET /Security/LiveFeed — polling API tra JSON ──────────────────────
        [HttpGet]
        public IActionResult LiveFeed(string? category = null, int count = 60)
        {
            if (HttpContext.Session.GetString("UserRole") != "Admin")
                return Forbid();

            var events = _secLog.GetLatest(count, category)
                .Select(e => new
                {
                    e.Id,
                    time     = e.Timestamp.ToString("HH:mm:ss"),
                    e.Category,
                    e.Level,
                    e.Message,
                    e.UserName,
                    e.IpAddress,
                    e.Detail
                });

            return Ok(new
            {
                events,
                stats = _secLog.GetStats(),
                total = _secLog.TotalCount,
                serverTime = DateTime.UtcNow.ToString("HH:mm:ss")
            });
        }

        // ── Helper ────────────────────────────────────────────────────────────
        private bool CheckDatabase()
        {
            try   { return _context.Database.CanConnect(); }
            catch { return false; }
        }
    }

    // ── ViewModels & Request Models ───────────────────────────────────────────
    public class EncryptRequest { public string? Plaintext  { get; set; } }
    public class DecryptRequest { public string? Ciphertext { get; set; } }

    public class SecurityStatusViewModel
    {
        public bool   IsHttps           { get; set; }
        public string ForwardedProto    { get; set; } = "";
        public string Host              { get; set; } = "";
        public string Scheme            { get; set; } = "";
        public string Environment       { get; set; } = "";
        public bool   IsProduction      { get; set; }
        public bool   HasHstsHeader     { get; set; }
        public bool   HasCspHeader      { get; set; }
        public bool   HasXFrameHeader   { get; set; }
        public bool   HasNoSniffHeader  { get; set; }
        public bool   HasReferrerHeader { get; set; }
        public bool   HasPermHeader     { get; set; }
        public bool   HasCoopHeader     { get; set; }
        public bool   HasCorpHeader     { get; set; }
        public string SessionId         { get; set; } = "";
        public string SessionCookieName { get; set; } = "";
        public bool   DatabaseConnected { get; set; }
        public string ClientIp          { get; set; } = "";
        public string UserAgent         { get; set; } = "";
        public DateTime CheckedAt       { get; set; }

        public bool TransportSecure =>
            IsHttps || ForwardedProto.Equals("https", StringComparison.OrdinalIgnoreCase);

        public int SecurityScore
        {
            get
            {
                int s = 0;
                if (TransportSecure)  s += 20;
                if (HasCspHeader)     s += 15;
                if (HasXFrameHeader)  s += 10;
                if (HasNoSniffHeader) s += 10;
                if (HasReferrerHeader)s += 10;
                if (HasPermHeader)    s += 10;
                if (HasCoopHeader)    s += 10;
                if (HasCorpHeader)    s += 10;
                if (DatabaseConnected)s += 5;
                return s;
            }
        }
    }
}
