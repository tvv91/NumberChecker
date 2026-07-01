using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

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
            var messageCaption = BuildCaption(fileName, caption);

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(chatId), "chat_id");

            var fileContentPart = new ByteArrayContent(fileContent);
            fileContentPart.Headers.ContentType = new MediaTypeHeaderValue(
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

            var wireFileName = TelegramFileNameHelper.EncodeUtf8FileNameForMultipart(fileName);
            var escapedFileName = TelegramFileNameHelper.EscapeContentDispositionFileName(wireFileName);
            fileContentPart.Headers.TryAddWithoutValidation(
                "Content-Disposition",
                $"form-data; name=\"document\"; filename=\"{escapedFileName}\"");
            content.Add(fileContentPart);

            if (!string.IsNullOrWhiteSpace(messageCaption))
            {
                content.Add(new StringContent(messageCaption, Encoding.UTF8), "caption");
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

        private static string BuildCaption(string fileName, string? caption)
        {
            if (string.IsNullOrWhiteSpace(caption))
            {
                return fileName;
            }

            return caption;
        }
    }
}
