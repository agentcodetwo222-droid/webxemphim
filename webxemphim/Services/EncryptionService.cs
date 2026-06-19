using System.Security.Cryptography;
using System.Text;

namespace webxemphim.Services
{
    /// <summary>
    /// Mã hóa/giải mã dữ liệu nhạy cảm lưu trong database.
    /// Thuật toán: AES-256-GCM (chuẩn NIST SP 800-38D, dùng trong TLS 1.3 / SSTP).
    /// - Key 256-bit từ biến môi trường / user-secrets
    /// - Mỗi lần mã hóa sinh Nonce ngẫu nhiên 96-bit riêng (chống replay attack)
    /// - Tag 128-bit xác thực toàn vẹn dữ liệu (chống tamper)
    /// - Định dạng lưu DB: Base64(nonce[12] + tag[16] + ciphertext)
    /// </summary>
    public class EncryptionService
    {
        private readonly byte[] _key;
        private readonly SecurityLogService? _secLog;
        private const int NonceSize = 12;
        private const int TagSize   = 16;

        public EncryptionService(IConfiguration configuration, SecurityLogService? secLog = null)
        {
            var keyBase64 = configuration["Encryption:Key"]
                ?? throw new InvalidOperationException(
                    "Encryption:Key chua duoc cau hinh. " +
                    "Chay: dotnet user-secrets set \"Encryption:Key\" \"<base64-256bit-key>\"");

            _key    = Convert.FromBase64String(keyBase64);
            _secLog = secLog;

            if (_key.Length != 32)
                throw new InvalidOperationException("Encryption:Key phai la 256-bit (32 bytes, Base64).");
        }

        /// <summary>Ma hoa chuoi plaintext -> Base64 ciphertext luu DB.</summary>
        public string Encrypt(string plaintext)
        {
            if (string.IsNullOrEmpty(plaintext)) return plaintext;

            var sw             = System.Diagnostics.Stopwatch.StartNew();
            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            var nonce          = new byte[NonceSize];
            var tag            = new byte[TagSize];
            var ciphertext     = new byte[plaintextBytes.Length];

            RandomNumberGenerator.Fill(nonce);

            using var aesGcm = new AesGcm(_key, TagSize);
            aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

            var combined = new byte[NonceSize + TagSize + ciphertext.Length];
            Buffer.BlockCopy(nonce,       0, combined, 0,                    NonceSize);
            Buffer.BlockCopy(tag,         0, combined, NonceSize,            TagSize);
            Buffer.BlockCopy(ciphertext,  0, combined, NonceSize + TagSize,  ciphertext.Length);

            var result = Convert.ToBase64String(combined);
            sw.Stop();

            // Ghi log neu la du lieu dang chu y (khong log decimal ngan)
            if (plaintextBytes.Length > 3)
            {
                var preview = plaintext.Length > 20 ? plaintext[..20] + "..." : plaintext;
                _secLog?.LogEncrypt("AES-256-GCM", preview,
                    $"Nonce={Convert.ToHexString(nonce)[..8]}... Tag={Convert.ToHexString(tag)[..8]}...",
                    sw.Elapsed.TotalMilliseconds);
            }

            return result;
        }

        /// <summary>Giai ma Base64 ciphertext tu DB -> plaintext.</summary>
        public string Decrypt(string encryptedBase64)
        {
            if (string.IsNullOrEmpty(encryptedBase64)) return encryptedBase64;

            byte[] combined;
            try { combined = Convert.FromBase64String(encryptedBase64); }
            catch { return encryptedBase64; }

            if (combined.Length < NonceSize + TagSize) return encryptedBase64;

            var nonce      = new byte[NonceSize];
            var tag        = new byte[TagSize];
            var ciphertext = new byte[combined.Length - NonceSize - TagSize];
            var plaintext  = new byte[ciphertext.Length];

            Buffer.BlockCopy(combined, 0,                    nonce,      0, NonceSize);
            Buffer.BlockCopy(combined, NonceSize,            tag,        0, TagSize);
            Buffer.BlockCopy(combined, NonceSize + TagSize,  ciphertext, 0, ciphertext.Length);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var aesGcm = new AesGcm(_key, TagSize);
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
            sw.Stop();

            var result = Encoding.UTF8.GetString(plaintext);

            // Ghi log giai ma neu du lieu dang chu y
            if (ciphertext.Length > 3)
                _secLog?.LogDecrypt("AES-256-GCM", sw.Elapsed.TotalMilliseconds);

            return result;
        }

        /// <summary>Mã hóa số decimal → string lưu DB.</summary>
        public string EncryptDecimal(decimal value) => Encrypt(value.ToString("G29"));

        /// <summary>Giải mã string từ DB → decimal.</summary>
        public decimal DecryptDecimal(string encryptedBase64)
        {
            var decrypted = Decrypt(encryptedBase64);
            return decimal.TryParse(decrypted, out var result) ? result : 0m;
        }

        /// <summary>Sinh key AES-256 ngẫu nhiên (dùng một lần khi setup).</summary>
        public static string GenerateKey()
        {
            var key = new byte[32];
            RandomNumberGenerator.Fill(key);
            return Convert.ToBase64String(key);
        }
    }
}
