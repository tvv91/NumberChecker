using System.Text;

namespace VodafoneNumberChecker.Services
{
    internal static class TelegramFileNameHelper
    {
        // Telegram Bot API не декодирует RFC 2047/2231 (=base64-мусор в чате).
        // Обходной путь: UTF-8 байты имени передаются в filename как Latin-1 без перекодирования.
        public static string EncodeUtf8FileNameForMultipart(string fileName) =>
            Encoding.GetEncoding("ISO-8859-1").GetString(Encoding.UTF8.GetBytes(fileName));

        public static string EscapeContentDispositionFileName(string fileName) =>
            fileName
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("\"", "\\\"", StringComparison.Ordinal);
    }
}
