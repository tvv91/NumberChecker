using System.Configuration;
using VodafoneNumberChecker.Models;

namespace VodafoneNumberChecker.Services
{
    public class AppSettingsService(ILoggerService logger) : IAppSettingsService
    {
        public AppSettings Load()
        {
            try
            {
                return new AppSettings
                {
                    Telegram = new TelegramOptions
                    {
                        BotToken = ConfigurationManager.AppSettings["Telegram.BotToken"] ?? string.Empty,
                        ChatId = ConfigurationManager.AppSettings["Telegram.ChatId"] ?? string.Empty
                    }
                };
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to load application settings from App.config", ex);
                return new AppSettings();
            }
        }
    }
}
