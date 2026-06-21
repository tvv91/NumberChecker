using System.ComponentModel.DataAnnotations;

namespace VodafoneNumberChecker.Models
{
    public class IterationHistory
    {
        [Key]
        public int Id { get; set; }
        public int IterationNumber { get; set; }
        public string IterationLabel { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime CompletedAt { get; set; }
        public int PlannedCount { get; set; }
        public int ProcessedCount { get; set; }
        public int FoundCount { get; set; }
        public int PropositionsFoundCount { get; set; }
        public int GiftsFoundCount { get; set; }
        public int ErrorCount { get; set; }
        public int NotFoundCount { get; set; }
        public int NotSuitableCount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<IterationHistoryItem> Items { get; set; } = new List<IterationHistoryItem>();
    }
}
