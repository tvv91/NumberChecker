namespace VodafoneNumberChecker.Services
{
    public interface IDailyReportService
    {
        Task SendRoundReportsAsync(DateTime roundStartedAt, DateTime roundCompletedAt, CancellationToken cancellationToken = default);
    }
}
