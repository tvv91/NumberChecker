namespace VodafoneNumberChecker.Models
{
    public class ProcessingConfiguration
    {
        public double DelayInputMin { get; set; } = 3;
        public double DelayInputMax { get; set; } = 5;
        public double DelaySearchMin { get; set; } = 3;
        public double DelaySearchMax { get; set; } = 5;
        public double DelayNextMin { get; set; } = 3;
        public double DelayNextMax { get; set; } = 5;
        public int EmptyPropositionsRepeats { get; set; } = 0;
        public int ErrorNumbersRepeats { get; set; } = 0;
        public bool Is24x7Mode { get; set; } = false;
        public bool ShouldTopUpNumbers { get; set; } = false;
        public List<TopUpRule> TopUpRules { get; set; } = [];

        public ProcessingConfiguration Clone() =>
            new()
            {
                DelayInputMin = DelayInputMin,
                DelayInputMax = DelayInputMax,
                DelaySearchMin = DelaySearchMin,
                DelaySearchMax = DelaySearchMax,
                DelayNextMin = DelayNextMin,
                DelayNextMax = DelayNextMax,
                EmptyPropositionsRepeats = EmptyPropositionsRepeats,
                ErrorNumbersRepeats = ErrorNumbersRepeats,
                Is24x7Mode = Is24x7Mode,
                ShouldTopUpNumbers = ShouldTopUpNumbers,
                TopUpRules = TopUpRules.Select(rule => rule.Clone()).ToList()
            };
    }
}


