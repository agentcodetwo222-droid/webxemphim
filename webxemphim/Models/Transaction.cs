using System.ComponentModel.DataAnnotations;

namespace webxemphim.Models
{
    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }

        public int UserId { get; set; }

        [StringLength(100)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty;

        // ── ENCRYPTED FIELDS (AES-256-GCM) ─────────────────────────────────
        /// <summary>Số tiền gốc — mã hóa AES-256-GCM</summary>
        [Required]
        [StringLength(256)]
        public string Amount { get; set; } = string.Empty;

        /// <summary>Mã tiền tệ — mã hóa AES-256-GCM</summary>
        [StringLength(256)]
        public string CurrencyCode { get; set; } = string.Empty;

        /// <summary>Số tiền VNĐ — mã hóa AES-256-GCM</summary>
        [Required]
        [StringLength(256)]
        public string AmountInVND { get; set; } = string.Empty;

        [StringLength(200)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string Status { get; set; } = "Pending";
    }
}
