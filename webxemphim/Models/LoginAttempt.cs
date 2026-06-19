using System.ComponentModel.DataAnnotations;

namespace webxemphim.Models
{
    /// <summary>
    /// Luu lich su dang nhap that bai — thay the in-memory Dictionary.
    /// Ben vung qua restart, khong mat lockout.
    /// </summary>
    public class LoginAttempt
    {
        [Key]
        public int Id { get; set; }

        [StringLength(200)]
        public string ClientKey { get; set; } = ""; // IP:identifier

        public int    FailCount    { get; set; } = 0;
        public DateTime LastAttempt { get; set; } = DateTime.UtcNow;
        public bool   IsLocked    { get; set; } = false;
        public DateTime? LockedUntil { get; set; }
    }
}
