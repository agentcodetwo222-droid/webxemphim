using System.Collections.Concurrent;

namespace webxemphim.Services
{
    /// <summary>
    /// In-memory real-time security event log.
    /// Singleton — thu thap su kien tu toan app, Admin xem qua /Security/LiveFeed.
    /// Giu toi da 500 ban ghi moi nhat (vong tron).
    /// </summary>
    public class SecurityLogService
    {
        private const int MaxEntries = 500;
        private readonly ConcurrentQueue<SecurityEvent> _queue = new();

        // ── Log Methods ───────────────────────────────────────────────────────

        public void LogLogin(string userName, string role, string ip, bool success)
        {
            Add(new SecurityEvent
            {
                Category  = success ? "LOGIN_OK" : "LOGIN_FAIL",
                Level     = success ? "SUCCESS"  : "WARNING",
                Message   = success
                    ? $"Dang nhap thanh cong: {userName} [{role}]"
                    : $"Dang nhap that bai: {userName}",
                UserName  = userName,
                IpAddress = ip,
                Detail    = success ? $"Role: {role}" : "Sai mat khau hoac tai khoan"
            });
        }

        public void LogLogout(string userName, string ip)
        {
            Add(new SecurityEvent
            {
                Category  = "LOGOUT",
                Level     = "INFO",
                Message   = $"Dang xuat: {userName}",
                UserName  = userName,
                IpAddress = ip,
                Detail    = "Phien lam viec ket thuc"
            });
        }

        public void LogRegister(string userName, string ip)
        {
            Add(new SecurityEvent
            {
                Category  = "REGISTER",
                Level     = "INFO",
                Message   = $"Tai khoan moi: {userName}",
                UserName  = userName,
                IpAddress = ip,
                Detail    = "Dang ky tai khoan thanh cong"
            });
        }

        public void LogLockout(string identifier, string ip)
        {
            Add(new SecurityEvent
            {
                Category  = "LOCKOUT",
                Level     = "DANGER",
                Message   = $"Bi khoa: {identifier}",
                UserName  = identifier,
                IpAddress = ip,
                Detail    = "Dang nhap sai qua 5 lan - khoa 15 phut"
            });
        }

        public void LogEncrypt(string field, string inputPreview, string outputPreview, double ms)
        {
            Add(new SecurityEvent
            {
                Category  = "ENCRYPT",
                Level     = "CRYPTO",
                Message   = $"Ma hoa: {field}",
                UserName  = "System",
                IpAddress = "internal",
                Detail    = $"Input: {inputPreview} | Nonce+Tag+Cipher -> {outputPreview} | {ms:F2}ms"
            });
        }

        public void LogDecrypt(string field, double ms)
        {
            Add(new SecurityEvent
            {
                Category  = "DECRYPT",
                Level     = "CRYPTO",
                Message   = $"Giai ma: {field}",
                UserName  = "System",
                IpAddress = "internal",
                Detail    = $"AES-256-GCM verify Tag OK | {ms:F2}ms"
            });
        }

        public void LogDeposit(string userName, string amount, string currency, string ip)
        {
            Add(new SecurityEvent
            {
                Category  = "DEPOSIT",
                Level     = "FINANCE",
                Message   = $"Nap tien: {userName}",
                UserName  = userName,
                IpAddress = ip,
                Detail    = $"So tien: {amount} {currency} | Ma hoa AES-256-GCM truoc khi luu DB"
            });
        }

        public void LogBuyVIP(string userName, string package, string ip)
        {
            Add(new SecurityEvent
            {
                Category  = "BUY_VIP",
                Level     = "FINANCE",
                Message   = $"Mua VIP: {userName}",
                UserName  = userName,
                IpAddress = ip,
                Detail    = $"Goi: {package} | Giao dich ma hoa, luu DB"
            });
        }

        public void LogCsrfToken(string path, string ip)
        {
            Add(new SecurityEvent
            {
                Category  = "CSRF_TOKEN",
                Level     = "SECURITY",
                Message   = $"CSRF token cap: {path}",
                UserName  = "System",
                IpAddress = ip,
                Detail    = "Antiforgery token sinh va dinh kem vao form"
            });
        }

        public void LogRateLimit(string ip, string endpoint)
        {
            Add(new SecurityEvent
            {
                Category  = "RATE_LIMIT",
                Level     = "DANGER",
                Message   = $"Rate limit: {ip}",
                UserName  = "Unknown",
                IpAddress = ip,
                Detail    = $"Endpoint: {endpoint} | 429 Too Many Requests"
            });
        }

        // ── Query ─────────────────────────────────────────────────────────────

        /// <summary>Lay n ban ghi moi nhat, loc theo category neu co.</summary>
        public IEnumerable<SecurityEvent> GetLatest(int count = 50, string? category = null)
        {
            var all = _queue.ToArray().Reverse();
            if (!string.IsNullOrEmpty(category))
                all = all.Where(e => e.Category == category);
            return all.Take(count);
        }

        public int TotalCount => _queue.Count;

        public Dictionary<string, int> GetStats()
        {
            var all = _queue.ToArray();
            return new Dictionary<string, int>
            {
                ["LOGIN_OK"]   = all.Count(e => e.Category == "LOGIN_OK"),
                ["LOGIN_FAIL"] = all.Count(e => e.Category == "LOGIN_FAIL"),
                ["LOCKOUT"]    = all.Count(e => e.Category == "LOCKOUT"),
                ["ENCRYPT"]    = all.Count(e => e.Category == "ENCRYPT"),
                ["DECRYPT"]    = all.Count(e => e.Category == "DECRYPT"),
                ["DEPOSIT"]    = all.Count(e => e.Category == "DEPOSIT"),
                ["BUY_VIP"]    = all.Count(e => e.Category == "BUY_VIP"),
                ["REGISTER"]   = all.Count(e => e.Category == "REGISTER"),
            };
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void Add(SecurityEvent evt)
        {
            evt.Timestamp = DateTime.UtcNow;
            evt.Id        = Interlocked.Increment(ref _idCounter);
            _queue.Enqueue(evt);

            // Giu toi da MaxEntries ban ghi
            while (_queue.Count > MaxEntries)
                _queue.TryDequeue(out _);
        }

        private static long _idCounter = 0;
    }

    // ── Event Model ───────────────────────────────────────────────────────────
    public class SecurityEvent
    {
        public long     Id        { get; set; }
        public DateTime Timestamp { get; set; }
        public string   Category  { get; set; } = "";
        public string   Level     { get; set; } = "INFO";
        public string   Message   { get; set; } = "";
        public string   UserName  { get; set; } = "";
        public string   IpAddress { get; set; } = "";
        public string   Detail    { get; set; } = "";
    }
}
