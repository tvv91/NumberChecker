using System.Text.Json.Serialization;

namespace VodafoneNumberChecker.Models
{
    internal class OffersResponseDto
    {
        [JsonPropertyName("Offers")]
        public List<OfferDto> Offers { get; set; } = new();

        [JsonPropertyName("IsPropositionsNotFound")]
        public bool IsPropositionsNotFound { get; set; }
    }
}
