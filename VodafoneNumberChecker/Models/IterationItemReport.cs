namespace VodafoneNumberChecker.Models
{
    public class IterationItemReport
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Outcome { get; set; } = string.Empty;
        public int DiscountPercent { get; set; }
        public decimal GiftAmount { get; set; }
        public decimal MinTopupAmount { get; set; }
        public bool IsError { get; set; }
        public string? ErrorDescription { get; set; }
    }
}
