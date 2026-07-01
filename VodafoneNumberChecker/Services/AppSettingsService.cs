using System.IO;
using System.Text.Json;
using VodafoneNumberChecker.Models;

namespace VodafoneNumberChecker.Services
{
    public class AppSettingsService(ILoggerService logger) : IAppSettingsService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly string _settingsFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "appsettings.json");

        public AppSettings Load()
        {
            if (!File.Exists(_settingsFilePath))
            {
                logger.LogWarning("Application settings file not found: appsettings.json");
                return new AppSettings();
            }

            try
            {
                var json = File.ReadAllText(_settingsFilePath);
                return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to load application settings from appsettings.json", ex);
                return new AppSettings();
            }
        }
    }
}
