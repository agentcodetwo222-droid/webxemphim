using Microsoft.AspNetCore.Mvc;
using webxemphim.Models;

namespace webxemphim.Controllers
{
    public class SecurityController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public SecurityController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env     = env;
        }

        [HttpGet]
        public IActionResult Index()
        {
            // Chỉ Admin mới được vào
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này!";
                return RedirectToAction("Index", "Home");
            }

            // ── Thu thập thông tin trạng thái bảo mật thực tế ────────────────
            var model = new SecurityStatusViewModel
            {
                // Transport
                IsHttps            = Request.IsHttps,
                ForwardedProto     = Request.Headers["X-Forwarded-Proto"].ToString(),
                Host               = Request.Host.ToString(),
                Scheme             = Request.Scheme,

                // Environment
                Environment        = _env.EnvironmentName,
                IsProduction       = _env.IsProduction(),

                // Headers nhận được
                HasHstsHeader      = Response.Headers.ContainsKey("Strict-Transport-Security"),
                HasCspHeader       = Response.Headers.ContainsKey("Content-Security-Policy"),
                HasXFrameHeader    = Response.Headers.ContainsKey("X-Frame-Options"),
                HasNoSniffHeader   = Response.Headers.ContainsKey("X-Content-Type-Options"),
                HasReferrerHeader  = Response.Headers.ContainsKey("Referrer-Policy"),
                HasPermHeader      = Response.Headers.ContainsKey("Permissions-Policy"),
                HasCoopHeader      = Response.Headers.ContainsKey("Cross-Origin-Opener-Policy"),
                HasCorpHeader      = Response.Headers.ContainsKey("Cross-Origin-Resource-Policy"),

                // Session
                SessionId          = HttpContext.Session.Id,
                SessionCookieName  = HttpContext.Request.Cookies.Keys
                                        .FirstOrDefault(k => k.Contains("Session")) ?? "N/A",

                // Database
                DatabaseConnected  = CheckDatabase(),

                // Request info
                ClientIp           = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "N/A",
                UserAgent          = Request.Headers["User-Agent"].ToString(),
                CheckedAt          = DateTime.UtcNow,
            };

            return View(model);
        }

        private bool CheckDatabase()
        {
            try
            {
                return _context.Database.CanConnect();
            }
            catch
            {
                return false;
            }
        }
    }

    // ── ViewModel ─────────────────────────────────────────────────────────────
    public class SecurityStatusViewModel
    {
        // Transport Security
        public bool   IsHttps           { get; set; }
        public string ForwardedProto    { get; set; } = "";
        public string Host              { get; set; } = "";
        public string Scheme            { get; set; } = "";

        // Environment
        public string Environment       { get; set; } = "";
        public bool   IsProduction      { get; set; }

        // Security Headers
        public bool HasHstsHeader       { get; set; }
        public bool HasCspHeader        { get; set; }
        public bool HasXFrameHeader     { get; set; }
        public bool HasNoSniffHeader    { get; set; }
        public bool HasReferrerHeader   { get; set; }
        public bool HasPermHeader       { get; set; }
        public bool HasCoopHeader       { get; set; }
        public bool HasCorpHeader       { get; set; }

        // Session
        public string SessionId         { get; set; } = "";
        public string SessionCookieName { get; set; } = "";

        // Database
        public bool DatabaseConnected   { get; set; }

        // Request
        public string ClientIp          { get; set; } = "";
        public string UserAgent         { get; set; } = "";
        public DateTime CheckedAt       { get; set; }

        // Computed
        public bool TransportSecure =>
            IsHttps || ForwardedProto.Equals("https", StringComparison.OrdinalIgnoreCase);

        public int SecurityScore
        {
            get
            {
                int score = 0;
                if (TransportSecure)  score += 20;
                if (HasCspHeader)     score += 15;
                if (HasXFrameHeader)  score += 10;
                if (HasNoSniffHeader) score += 10;
                if (HasReferrerHeader)score += 10;
                if (HasPermHeader)    score += 10;
                if (HasCoopHeader)    score += 10;
                if (HasCorpHeader)    score += 10;
                if (DatabaseConnected)score += 5;
                return score;
            }
        }
    }
}
