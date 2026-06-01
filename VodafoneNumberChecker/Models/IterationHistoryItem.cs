using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VodafoneNumberChecker.Models
{
    public class IterationHistoryItem
    {
        [Key]
        public int Id { get; set; }
        public int IterationHistoryId { get; set; }

        [ForeignKey(nameof(IterationHistoryId))]
        public IterationHistory? IterationHistory { get; set; }

        [MaxLength(50)]
        public string PhoneNumber { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Outcome { get; set; } = string.Empty;

        public int DiscountPercent { get; set; }
        public decimal GiftAmount { get; set; }
        public decimal MinTopupAmount { get; set; }
        public bool IsError { get; set; }
        public string? ErrorDescription { get; set; }
    }
}
