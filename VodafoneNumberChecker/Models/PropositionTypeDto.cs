using System.Text.Json.Serialization;

namespace VodafoneNumberChecker.Models
{
    internal class PropositionTypeDto
    {
        [JsonPropertyName("Title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("Content")]
        public string Content { get; set; } = string.Empty;
    }
}
