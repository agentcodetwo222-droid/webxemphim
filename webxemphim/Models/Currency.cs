using System.ComponentModel.DataAnnotations;

namespace webxemphim.Models
{
    public class Currency
    {
        [Key]
        public int CurrencyId { get; set; }

        [Required]
        [StringLength(10)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string Symbol { get; set; } = string.Empty;

        [Required]
        public decimal ExchangeRate { get; set; }

        [Required]
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;
    }
}
