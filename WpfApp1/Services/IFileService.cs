namespace VodafoneLogin.Services
{
    public interface IFileService
    {
        List<string> ReadPhoneNumbers(string filePath);
        void WriteProgress(int index, string filePath);
        int ReadProgress(string filePath);
        void AppendErrorNumber(string number, string filePath);
        void AppendResult(string line, string filePath);
        void AppendLog(string message, string filePath);
    }
}

