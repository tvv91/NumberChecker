namespace VodafoneNumberChecker.Models
{
  public static class OfferClassification
  {
    public static bool HasDiscountOrGift(Offer offer) =>
      offer.Discount != 0 || offer.Gift != 0;

    public static bool HasDiscountOrGift(PhoneOffer offer) =>
      offer.DiscountPercent != 0 || offer.GiftAmount != 0;
  }
}
