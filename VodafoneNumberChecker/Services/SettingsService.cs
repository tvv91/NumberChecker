using System.IO;
using System.Text.Json;
using VodafoneNumberChecker.Models;

namespace VodafoneNumberChecker.Services
{
    public class SettingsService(ILoggerService logger) : ISettingsService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly string _settingsFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "settings.json");

        public ProcessingConfiguration Load()
        {
            if (!File.Exists(_settingsFilePath))
            {
                return new ProcessingConfiguration();
            }

            try
            {
                var json = File.ReadAllText(_settingsFilePath);
                return JsonSerializer.Deserialize<ProcessingConfiguration>(json, JsonOptions)
                       ?? new ProcessingConfiguration();
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to load application settings", ex);
                return new ProcessingConfiguration();
            }
        }

        public void Save(ProcessingConfiguration configuration)
        {
            try
            {
                var json = JsonSerializer.Serialize(configuration, JsonOptions);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to save application settings", ex);
                throw;
            }
        }
    }
}
