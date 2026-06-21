namespace VodafoneNumberChecker.Models
{
    public class TopUpRule
    {
        public int DiscountPercentFrom { get; set; }
        public int DiscountPercentTo { get; set; }
        public decimal MinTopupAmountFrom { get; set; }
        public decimal MinTopupAmountTo { get; set; }
        public decimal GiftAmountFrom { get; set; }
        public decimal GiftAmountTo { get; set; }

        public TopUpRule Clone() =>
            new()
            {
                DiscountPercentFrom = DiscountPercentFrom,
                DiscountPercentTo = DiscountPercentTo,
                MinTopupAmountFrom = MinTopupAmountFrom,
                MinTopupAmountTo = MinTopupAmountTo,
                GiftAmountFrom = GiftAmountFrom,
                GiftAmountTo = GiftAmountTo
            };
    }
}
