using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webxemphim.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string UserName { get; set; } = string.Empty;

        /// <summary>Mật khẩu — lưu BCrypt hash, không mã hóa AES (hash 1 chiều)</summary>
        [Required]
        [StringLength(256)]
        public string MK { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string EMAIL { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ROLE { get; set; } = string.Empty;

        public decimal Balance { get; set; } = 0;

        public DateTime? VIPExpiryDate { get; set; }

        [StringLength(500)]
        public string? Avatar { get; set; }

        // ── ENCRYPTED FIELDS (AES-256-GCM) ─────────────────────────────────
        // Lưu ciphertext trong DB, app đọc/ghi qua EncryptionService

        /// <summary>Số điện thoại — mã hóa AES-256-GCM lưu DB</summary>
        [StringLength(512)]
        public string? Phone { get; set; }

        /// <summary>Địa chỉ — mã hóa AES-256-GCM lưu DB</summary>
        [StringLength(1024)]
        public string? Address { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
