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

        /// <summary>Mat khau — luu BCrypt hash, khong ma hoa AES (hash 1 chieu)</summary>
        [Required]
        [StringLength(256)]
        public string MK { get; set; } = string.Empty;

        /// <summary>Email — ma hoa AES-256-GCM luu DB</summary>
        [Required]
        [StringLength(512)]
        public string EMAIL { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string ROLE { get; set; } = string.Empty;

        /// <summary>So du vi — ma hoa AES-256-GCM luu DB (luu string)</summary>
        [StringLength(512)]
        public string BalanceEncrypted { get; set; } = string.Empty;

        /// <summary>So du thuc — chi dung runtime, KHONG map vao DB</summary>
        [NotMapped]
        public decimal Balance { get; set; } = 0;

        public DateTime? VIPExpiryDate { get; set; }

        [StringLength(500)]
        public string? Avatar { get; set; }

        // ── ENCRYPTED FIELDS (AES-256-GCM) ─────────────────────────────────

        /// <summary>So dien thoai — ma hoa AES-256-GCM luu DB</summary>
        [StringLength(512)]
        public string? Phone { get; set; }

        /// <summary>Dia chi — ma hoa AES-256-GCM luu DB</summary>
        [StringLength(1024)]
        public string? Address { get; set; }

        /// <summary>Token invalidation — tang khi doi password hoac admin khoa</summary>
        public int SecurityStamp { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsLocked { get; set; } = false;
    }
}
