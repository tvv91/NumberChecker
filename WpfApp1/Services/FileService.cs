using System.IO;

namespace VodafoneLogin.Services
{
    public class FileService : IFileService
    {
        public List<string> ReadPhoneNumbers(string filePath)
        {
            if (!File.Exists(filePath))
                return new List<string>();

            return File.ReadAllLines(filePath)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrEmpty(l))
                .ToList();
        }

        public void WriteProgress(int index, string filePath)
        {
            File.WriteAllText(filePath, index.ToString());
        }

        public int ReadProgress(string filePath)
        {
            if (!File.Exists(filePath))
                return -1;

            var text = File.ReadAllText(filePath);
            if (int.TryParse(text, out int index))
                return index;

            return -1;
        }

        public void AppendErrorNumber(string number, string filePath)
        {
            File.AppendAllText(filePath, $"{number}{Environment.NewLine}");
        }

        public void AppendResult(string line, string filePath)
        {
            File.AppendAllText(filePath, line);
        }

        public void AppendLog(string message, string filePath)
        {
            File.AppendAllText(filePath, message);
        }
    }
}

