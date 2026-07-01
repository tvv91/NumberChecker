using System.Net.Http;
using System.Net.Http.Headers;

namespace VodafoneNumberChecker.Services
{
    public class TelegramService(ILoggerService logger, HttpClient httpClient) : ITelegramService
    {
        public async Task SendDocumentAsync(
            string botToken,
            string chatId,
            byte[] fileContent,
            string fileName,
            string? caption = null,
            CancellationToken cancellationToken = default)
        {
            var requestUri = $"https://api.telegram.org/bot{botToken}/sendDocument";

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(chatId), "chat_id");

            var fileContentPart = new ByteArrayContent(fileContent);
            fileContentPart.Headers.ContentType = new MediaTypeHeaderValue(
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            content.Add(fileContentPart, "document", fileName);

            if (!string.IsNullOrWhiteSpace(caption))
            {
                content.Add(new StringContent(caption), "caption");
            }

            using var response = await httpClient.PostAsync(requestUri, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError($"Telegram sendDocument failed: {response.StatusCode}. Response: {responseBody}");
                throw new InvalidOperationException($"Не удалось отправить файл в Telegram: {response.StatusCode}");
            }

            logger.LogInfo($"Telegram document sent: {fileName}");
        }
    }
}
