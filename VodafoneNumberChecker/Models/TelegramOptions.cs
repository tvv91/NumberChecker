namespace VodafoneNumberChecker.Models
{
    public class TelegramOptions
    {
        public string BotToken { get; set; } = string.Empty;

        public string ChatId { get; set; } = string.Empty;

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(BotToken) && !string.IsNullOrWhiteSpace(ChatId);
    }
}
