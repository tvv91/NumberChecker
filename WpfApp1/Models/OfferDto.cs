using System.Text.Json.Serialization;

namespace VodafoneLogin.Models
{
    internal class OfferDto
    {
        [JsonPropertyName("Discount")]
        public int Discount { get; set; }

        [JsonPropertyName("MinTopUp")]
        public decimal MinTopUp { get; set; }

        [JsonPropertyName("Gift")]
        public decimal Gift { get; set; }

        [JsonPropertyName("ActiveDays")]
        public int ActiveDays { get; set; }

        [JsonPropertyName("ValidUntil")]
        public string? ValidUntil { get; set; }

        [JsonPropertyName("FullText")]
        public string? FullText { get; set; }

        public Offer ToOffer()
        {
            DateTime? validUntil = null;
            if (!string.IsNullOrEmpty(ValidUntil) && DateTime.TryParse(ValidUntil, out DateTime parsedDate))
            {
                validUntil = parsedDate;
            }

            return new Offer
            {
                Discount = Discount,
                MinTopUp = MinTopUp,
                Gift = Gift,
                ActiveDays = ActiveDays,
                ValidUntil = validUntil,
                FullText = FullText
            };
        }
    }
}

