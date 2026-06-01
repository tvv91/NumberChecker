using System.Collections.ObjectModel;

namespace VodafoneNumberChecker.Models
{
    public class IterationReport
    {
        public int Id { get; set; }
        public int IterationNumber { get; set; }
        public string IterationLabel { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime CompletedAt { get; set; }
        public int PlannedCount { get; set; }
        public int ProcessedCount { get; set; }
        public int FoundCount { get; set; }
        public int ErrorCount { get; set; }
        public int NotFoundCount { get; set; }
        public int NotSuitableCount { get; set; }
        public int NoOfferCount { get; set; }
        public ObservableCollection<IterationItemReport> Items { get; set; } = new();

        public string DurationText => FormatDuration(CompletedAt - StartedAt);

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
            {
                return $"{(int)duration.TotalHours} ч {duration.Minutes} мин {duration.Seconds} сек";
            }

            if (duration.TotalMinutes >= 1)
            {
                return $"{duration.Minutes} мин {duration.Seconds} сек";
            }

            return $"{Math.Max(0, duration.Seconds)} сек";
        }
    }
}
