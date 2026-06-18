using System.ComponentModel.DataAnnotations;

namespace webxemphim.Models
{
    public class WatchHistory
    {
        [Key]
        public int WatchHistoryId { get; set; }

        public int UserId { get; set; }

        [StringLength(100)]
        public string UserName { get; set; } = string.Empty;

        public int MovieId { get; set; }

        [StringLength(200)]
        public string MovieTitle { get; set; } = string.Empty;

        // ── ENCRYPTED FIELDS (AES-256-GCM) ─────────────────────────────────
        /// <summary>
        /// Đường dẫn ảnh phim — mã hóa AES-256-GCM.
        /// QUAN TRỌNG: Luôn dùng EncryptionService.Encrypt() trước khi gán,
        /// và EncryptionService.Decrypt() trước khi hiển thị.
        /// </summary>
        [StringLength(1024)]
        public string MovieImage { get; set; } = string.Empty;

        public DateTime WatchedAt { get; set; } = DateTime.UtcNow;

        public int WatchDuration { get; set; } = 0;

        public bool IsCompleted { get; set; } = false;
    }
}
