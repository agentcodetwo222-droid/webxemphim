using System.ComponentModel.DataAnnotations;

namespace webxemphim.Models
{
    /// <summary>
    /// Audit log ben vung luu DB — ghi lai moi hanh dong quan trong.
    /// Khac SecurityLogService (in-memory): AuditLog ben vung qua restart.
    /// </summary>
    public class AuditLog
    {
        [Key]
        public long AuditLogId { get; set; }

        public DateTime Timestamp   { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string Category      { get; set; } = ""; // LOGIN, DEPOSIT, BUY_VIP, ...

        [StringLength(50)]
        public string Level         { get; set; } = "INFO";

        [StringLength(500)]
        public string Message       { get; set; } = "";

        public int?   UserId        { get; set; }

        [StringLength(100)]
        public string UserName      { get; set; } = "";

        [StringLength(45)]
        public string IpAddress     { get; set; } = "";

        [StringLength(1000)]
        public string Detail        { get; set; } = "";
    }
}
