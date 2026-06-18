using System.ComponentModel.DataAnnotations;

namespace webxemphim.Models
{
    public class Movie
    {
        [Key]
        public int MovieId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [StringLength(100)]
        public string Genre { get; set; } = string.Empty;

        [StringLength(100)]
        public string Country { get; set; } = string.Empty;

        [StringLength(50)]
        public string Year { get; set; } = string.Empty;

        // ── ENCRYPTED FIELDS (AES-256-GCM) ─────────────────────────────────
        /// <summary>Đường dẫn ảnh thumbnail — mã hóa AES-256-GCM lưu DB</summary>
        [StringLength(1024)]
        public string ImageUrl { get; set; } = string.Empty;

        /// <summary>Đường dẫn video — mã hóa AES-256-GCM lưu DB (bảo vệ URL nội dung)</summary>
        [StringLength(1024)]
        public string VideoUrl { get; set; } = string.Empty;

        public bool IsVipOnly { get; set; } = false;

        public bool IsAvailable { get; set; } = true;

        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Director { get; set; }

        [StringLength(500)]
        public string? Actors { get; set; }

        public int? Duration { get; set; }

        public int TotalViews { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
