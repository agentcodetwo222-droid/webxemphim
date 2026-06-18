using System.ComponentModel.DataAnnotations;

namespace webxemphim.Models
{
    public class Bill
    {
        [Key]
        public int BillId { get; set; }

        // ── ENCRYPTED FIELDS (AES-256-GCM) ─────────────────────────────────
        /// <summary>Mã hóa đơn — mã hóa AES-256-GCM</summary>
        [Required]
        [StringLength(256)]
        public string BillCode { get; set; } = string.Empty;

        public int UserId { get; set; }

        [StringLength(100)]
        public string UserName { get; set; } = string.Empty;

        /// <summary>Email user — mã hóa AES-256-GCM (PII)</summary>
        [StringLength(512)]
        public string UserEmail { get; set; } = string.Empty;

        /// <summary>ID giao dịch — mã hóa AES-256-GCM</summary>
        [StringLength(256)]
        public string TransactionId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty;

        [StringLength(200)]
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>Số tiền gốc — mã hóa AES-256-GCM</summary>
        [StringLength(256)]
        public string Amount { get; set; } = string.Empty;

        /// <summary>Mã tiền tệ — mã hóa AES-256-GCM</summary>
        [StringLength(256)]
        public string CurrencyCode { get; set; } = string.Empty;

        /// <summary>Số tiền VNĐ — mã hóa AES-256-GCM</summary>
        [StringLength(256)]
        public string AmountInVND { get; set; } = string.Empty;

        /// <summary>Số dư trước giao dịch — mã hóa AES-256-GCM</summary>
        [StringLength(256)]
        public string BalanceBefore { get; set; } = string.Empty;

        /// <summary>Số dư sau giao dịch — mã hóa AES-256-GCM</summary>
        [StringLength(256)]
        public string BalanceAfter { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string Status { get; set; } = "Completed";

        [StringLength(500)]
        public string Note { get; set; } = string.Empty;
    }
}
