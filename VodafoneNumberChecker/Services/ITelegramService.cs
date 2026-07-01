namespace VodafoneNumberChecker.Services
{
    public interface ITelegramService
    {
        Task SendDocumentAsync(
            string botToken,
            string chatId,
            byte[] fileContent,
            string fileName,
            string? caption = null,
            CancellationToken cancellationToken = default);
    }
}
