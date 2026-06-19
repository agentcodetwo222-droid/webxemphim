using webxemphim.Models;

namespace webxemphim.Services
{
    /// <summary>
    /// Trung tam xu ly User voi ma hoa/giai ma EMAIL + Balance.
    /// Moi noi can doc/ghi User deu di qua service nay.
    /// </summary>
    public class UserService
    {
        private readonly EncryptionService _enc;

        public UserService(EncryptionService enc)
        {
            _enc = enc;
        }

        // ── Giai ma USER tu DB -> runtime ─────────────────────────────────

        /// <summary>Giai ma EMAIL + Balance. Goi ngay sau khi lay tu DB.</summary>
        public User Decrypt(User user)
        {
            if (user == null) return user!;

            // Giai ma EMAIL
            if (!string.IsNullOrEmpty(user.EMAIL))
            {
                try { user.EMAIL = _enc.Decrypt(user.EMAIL); }
                catch { /* du lieu cu chua ma hoa → giu nguyen */ }
            }

            // Giai ma Balance
            if (!string.IsNullOrEmpty(user.BalanceEncrypted))
            {
                try { user.Balance = _enc.DecryptDecimal(user.BalanceEncrypted); }
                catch { user.Balance = 0; }
            }

            return user;
        }

        /// <summary>Giai ma danh sach User.</summary>
        public List<User> DecryptList(IEnumerable<User> users)
            => users.Select(Decrypt).ToList();

        // ── Ma hoa User truoc khi luu DB ──────────────────────────────────

        /// <summary>Ma hoa EMAIL + Balance. Goi truoc khi SaveChanges.</summary>
        public void EncryptForSave(User user)
        {
            if (!string.IsNullOrEmpty(user.EMAIL))
                user.EMAIL = _enc.Encrypt(user.EMAIL);

            user.BalanceEncrypted = _enc.EncryptDecimal(user.Balance);
        }

        /// <summary>Chi ma hoa Balance (dung khi chi cap nhat so du).</summary>
        public void EncryptBalance(User user)
        {
            user.BalanceEncrypted = _enc.EncryptDecimal(user.Balance);
        }

        // ── Kiem tra EMAIL trung (so sanh ciphertext khong duoc) ──────────

        /// <summary>
        /// Kiem tra email da ton tai chua.
        /// Phai giai ma tat ca email trong DB roi so sanh.
        /// </summary>
        public bool EmailExists(IEnumerable<User> allUsers, string emailPlaintext)
        {
            var normalized = emailPlaintext.Trim().ToLowerInvariant();
            foreach (var u in allUsers)
            {
                try
                {
                    var dec = _enc.Decrypt(u.EMAIL);
                    if (dec.Trim().ToLowerInvariant() == normalized)
                        return true;
                }
                catch { }
            }
            return false;
        }

        // ── Password complexity ────────────────────────────────────────────

        /// <summary>Kiem tra do manh cua password. Tra ve null neu hop le.</summary>
        public static string? ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return "Mat khau khong duoc de trong.";
            if (password.Length < 8)
                return "Mat khau phai co it nhat 8 ky tu.";
            if (!password.Any(char.IsUpper))
                return "Mat khau phai co it nhat 1 chu hoa.";
            if (!password.Any(char.IsLower))
                return "Mat khau phai co it nhat 1 chu thuong.";
            if (!password.Any(char.IsDigit))
                return "Mat khau phai co it nhat 1 so.";
            if (!password.Any(c => "!@#$%^&*()_+-=[]{}|;':\",./<>?".Contains(c)))
                return "Mat khau phai co it nhat 1 ky tu dac biet (!@#$...).";
            return null; // hop le
        }
    }
}
