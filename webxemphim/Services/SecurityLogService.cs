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

        // ── Chi tiet ma hoa theo tung hanh dong ──────────────────────────────

        /// <summary>Log khi user dang nhap — hien thi qua trinh BCrypt</summary>
        public void LogLoginEncryption(string userName, string ip, bool success,
                                       string bcryptHash, double ms)
        {
            Add(new SecurityEvent
            {
                Category  = "AUTH_CRYPTO",
                Level     = "CRYPTO",
                Message   = $"BCrypt verify: {userName}",
                UserName  = userName,
                IpAddress = ip,
                Detail    = success
                    ? $"BCrypt(workFactor=12) MATCH | Hash: {bcryptHash[..20]}... | {ms:F1}ms | " +
                      $"Tailscale: {IsTailscale(ip)}"
                    : $"BCrypt(workFactor=12) NO MATCH | {ms:F1}ms"
            });
        }

        /// <summary>Log khi dang ky — hien thi toan bo ma hoa</summary>
        public void LogRegisterEncryption(string userName, string ip,
                                          string emailCipher, string balanceCipher)
        {
            Add(new SecurityEvent
            {
                Category  = "REGISTER_CRYPTO",
                Level     = "CRYPTO",
                Message   = $"Ma hoa dang ky: {userName}",
                UserName  = userName,
                IpAddress = ip,
                Detail    = $"Password: BCrypt(12 rounds) | " +
                            $"Email: AES-GCM→{emailCipher[..Math.Min(16, emailCipher.Length)]}... | " +
                            $"Balance: AES-GCM→{balanceCipher[..Math.Min(16, balanceCipher.Length)]}... | " +
                            $"Tailscale: {IsTailscale(ip)}"
            });
        }

        /// <summary>Log khi xem phim — hien thi giai ma VideoUrl</summary>
        public void LogVideoDecrypt(string movieTitle, string ip,
                                    string cipherPreview, string urlPreview, double ms)
        {
            Add(new SecurityEvent
            {
                Category  = "VIDEO_CRYPTO",
                Level     = "CRYPTO",
                Message   = $"Giai ma VideoUrl: {movieTitle}",
                UserName  = "System",
                IpAddress = ip,
                Detail    = $"AES-256-GCM decrypt | " +
                            $"Cipher: {cipherPreview[..Math.Min(16, cipherPreview.Length)]}... | " +
                            $"URL: {urlPreview} | {ms:F1}ms | " +
                            $"Tailscale: {IsTailscale(ip)}"
            });
        }

        /// <summary>Log khi nap tien — hien thi ma hoa so lieu tai chinh</summary>
        public void LogDepositEncryption(string userName, string ip,
                                         decimal amount, string amountCipher,
                                         string currencyCipher)
        {
            Add(new SecurityEvent
            {
                Category  = "DEPOSIT_CRYPTO",
                Level     = "CRYPTO",
                Message   = $"Ma hoa giao dich: {userName}",
                UserName  = userName,
                IpAddress = ip,
                Detail    = $"Amount {amount:N0} VND → AES-GCM: {amountCipher[..Math.Min(16,amountCipher.Length)]}... | " +
                            $"Currency → AES-GCM: {currencyCipher[..Math.Min(16,currencyCipher.Length)]}... | " +
                            $"Tailscale: {IsTailscale(ip)}"
            });
        }

        /// <summary>Log session token sau dang nhap</summary>
        public void LogSessionToken(string userName, string ip,
                                    string sessionId, string role)
        {
            Add(new SecurityEvent
            {
                Category  = "SESSION_TOKEN",
                Level     = "SECURITY",
                Message   = $"Session tao: {userName}",
                UserName  = userName,
                IpAddress = ip,
                Detail    = $"SessionId: {sessionId[..Math.Min(12, sessionId.Length)]}... | " +
                            $"Role: {role} | Cookie: HttpOnly+SameSite=Strict | " +
                            $"Tailscale: {IsTailscale(ip)}"
            });
        }

        // Helper kiem tra IP Tailscale
        private static bool IsTailscale(string ip)
        {
            if (string.IsNullOrEmpty(ip)) return false;
            var p = ip.Split('.');
            return p.Length >= 2
                && int.TryParse(p[0], out var a) && a == 100
                && int.TryParse(p[1], out var b) && b >= 64 && b <= 127;
        }

        // ── VPN Traffic Flow ──────────────────────────────────────────────────

        /// <summary>
        /// Log luong traffic Client → VPN Server → App va nguoc lai.
        /// Direction: "C→S" (Client to Server) hoac "S→C" (Server to Client)
        /// </summary>
        public void LogVpnTraffic(
            string direction,      // "C->S" hoac "S->C"
            string action,         // "LOGIN", "REGISTER", "WATCH", "DEPOSIT"...
            string userName,
            string clientIp,
            string payloadType,    // "Encrypted" hoac "Decrypted"
            string algorithm,      // "AES-256-GCM", "BCrypt", "ChaCha20-Poly1305"
            string dataPreview,    // Mo ta du lieu (khong lo thong tin that)
            int    byteSize,       // Kich thuoc uoc tinh
            double ms)
        {
            var isTailscaleConn = IsTailscale(clientIp);
            var tunnel          = isTailscaleConn ? "Tailscale/WireGuard" : "HTTPS/TLS";

            Add(new SecurityEvent
            {
                Category  = direction == "C->S" ? "VPN_C2S" : "VPN_S2C",
                Level     = "CRYPTO",
                Message   = $"[{direction}] {action}: {userName}",
                UserName  = userName,
                IpAddress = clientIp,
                Detail    = $"Tunnel: {tunnel} | " +
                            $"Payload: {payloadType} ({algorithm}) | " +
                            $"Data: {dataPreview} | " +
                            $"~{byteSize}B | {ms:F1}ms"
            });
        }

        // Shortcut methods cho tung hanh dong

        public void LogVpnLogin(string userName, string ip, bool success, double ms)
        {
            // Client → Server: gui credentials
            LogVpnTraffic("C->S", "LOGIN_REQUEST", userName, ip,
                "Encrypted", "ChaCha20-Poly1305",
                success ? "credentials (ma hoa tunnel)" : "credentials (sai)",
                256, ms);

            // Server → Client: tra ve session token
            if (success)
                LogVpnTraffic("S->C", "LOGIN_RESPONSE", userName, ip,
                    "Encrypted", "ChaCha20-Poly1305",
                    "Session cookie (HttpOnly, SameSite=Strict)",
                    128, ms);
        }

        public void LogVpnRegister(string userName, string ip, double ms)
        {
            // Client → Server: gui form dang ky
            LogVpnTraffic("C->S", "REGISTER_REQUEST", userName, ip,
                "Encrypted", "ChaCha20-Poly1305",
                "username + email + password (ma hoa tunnel)",
                512, ms);

            // Server: ma hoa va luu DB
            LogVpnTraffic("S->C", "REGISTER_RESPONSE", userName, ip,
                "Encrypted", "AES-256-GCM",
                "Email→cipher, Password→BCrypt, Balance→cipher luu DB",
                256, ms);
        }

        public void LogVpnVideoRequest(string movieTitle, string userName,
                                       string ip, double ms)
        {
            // Client → Server: yeu cau xem phim
            LogVpnTraffic("C->S", "VIDEO_REQUEST", userName, ip,
                "Encrypted", "ChaCha20-Poly1305",
                $"Request: /Movie/Player/{movieTitle}",
                128, ms);

            // Server → Client: tra ve stream video (da giai ma URL)
            LogVpnTraffic("S->C", "VIDEO_STREAM", userName, ip,
                "Encrypted", "ChaCha20-Poly1305",
                $"Video stream: {movieTitle} (URL giai ma AES-256-GCM)",
                1024 * 1024, ms); // ~1MB uoc tinh
        }

        public void LogVpnDeposit(string userName, string ip,
                                  decimal amount, string currency, double ms)
        {
            // Client → Server: gui so tien nap
            LogVpnTraffic("C->S", "DEPOSIT_REQUEST", userName, ip,
                "Encrypted", "ChaCha20-Poly1305",
                $"amount={amount:N0} {currency} + CSRF token",
                256, ms);

            // Server → Client: xac nhan + tra ve so du moi
            LogVpnTraffic("S->C", "DEPOSIT_RESPONSE", userName, ip,
                "Encrypted", "AES-256-GCM",
                $"Amount→cipher luu DB, Balance→cipher cap nhat",
                128, ms);
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
