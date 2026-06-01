namespace VodafoneNumberChecker.Models
{
    public class SimDataValidationResult
    {
        public bool IsValid => MissingNumbers.Count == 0;
        public int CheckedCount { get; set; }
        public List<string> MissingNumbers { get; init; } = new();
    }
}
