namespace VodafoneLogin.Models
{
    public class ProcessingConfiguration
    {
        public double DelayInputMin { get; set; } = 3;
        public double DelayInputMax { get; set; } = 5;
        public double DelaySearchMin { get; set; } = 3;
        public double DelaySearchMax { get; set; } = 5;
        public double DelayNextMin { get; set; } = 3;
        public double DelayNextMax { get; set; } = 5;
    }
}

