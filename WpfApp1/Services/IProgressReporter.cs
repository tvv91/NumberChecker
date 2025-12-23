namespace VodafoneLogin.Services
{
    public interface IProgressReporter
    {
        void ReportInputProgress(double progress);
        void ReportSearchProgress(double progress);
        void ReportNextProgress(double progress);
        void ReportCurrentNumber(string phoneNumber);
        void ReportProcessed(int count);
        void ReportOffersFound(int count);
        void ReportServerErrors(int count);
    }
}

