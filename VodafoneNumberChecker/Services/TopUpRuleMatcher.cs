using VodafoneNumberChecker.Models;

namespace VodafoneNumberChecker.Services
{
    public static class TopUpRuleMatcher
    {
        public static bool MatchesAnyRule(PhoneOffer offer, IReadOnlyList<TopUpRule> rules)
        {
            if (rules.Count == 0)
            {
                return false;
            }

            return rules.Any(rule => MatchesRule(offer, rule));
        }

        public static bool MatchesRule(PhoneOffer offer, TopUpRule rule) =>
            IsWithinRange(offer.DiscountPercent, rule.DiscountPercentFrom, rule.DiscountPercentTo) &&
            IsWithinRange(offer.MinTopupAmount, rule.MinTopupAmountFrom, rule.MinTopupAmountTo) &&
            IsWithinRange(offer.GiftAmount, rule.GiftAmountFrom, rule.GiftAmountTo);

        private static bool IsWithinRange(int value, int from, int to) =>
            value >= Math.Min(from, to) && value <= Math.Max(from, to);

        private static bool IsWithinRange(decimal value, decimal from, decimal to) =>
            value >= Math.Min(from, to) && value <= Math.Max(from, to);
    }
}
