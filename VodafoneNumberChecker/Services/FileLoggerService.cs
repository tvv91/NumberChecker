using System;
using System.IO;
using System.Threading.Tasks;

namespace VodafoneNumberChecker.Services
{
    public class FileLoggerService : ILoggerService
    {
        private readonly string _logFilePath;
        private readonly object _lockObject = new object();

        public FileLoggerService()
        {
            var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            var logFileName = $"app_{DateTime.Now:yyyy-MM-dd}.log";
            _logFilePath = Path.Combine(logDirectory, logFileName);
        }

        public void LogInfo(string message)
        {
            Log("INFO", message);
        }

        public void LogWarning(string message)
        {
            Log("WARN", message);
        }

        public void LogError(string message, Exception? exception = null)
        {
            var fullMessage = message;
            if (exception != null)
            {
                fullMessage += $"\nException: {exception.GetType().Name}\nMessage: {exception.Message}\nStack Trace: {exception.StackTrace}";
                if (exception.InnerException != null)
                {
                    fullMessage += $"\nInner Exception: {exception.InnerException.GetType().Name}\nInner Message: {exception.InnerException.Message}";
                }
            }
            Log("ERROR", fullMessage);
        }

        public void LogDebug(string message)
        {
            Log("DEBUG", message);
        }

        private void Log(string level, string message)
        {
            try
            {
                lock (_lockObject)
                {
                    var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}\n";
                    File.AppendAllText(_logFilePath, logEntry);
                }
            }
            catch
            {
                // Silently fail if logging fails to prevent infinite loops
            }
        }
    }
}

