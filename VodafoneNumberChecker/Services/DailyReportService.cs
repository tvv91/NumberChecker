using VodafoneNumberChecker.Models;

namespace VodafoneNumberChecker.Services
{
    public class DailyReportService(
        IAppSettingsService appSettingsService,
        IDataService dataService,
        IPhoneOffersExcelExporter excelExporter,
        ITelegramService telegramService,
        ILoggerService logger) : IDailyReportService
    {
        public async Task SendRoundReportsAsync(
            DateTime roundStartedAt,
            DateTime roundCompletedAt,
            CancellationToken cancellationToken = default)
        {
            var telegram = appSettingsService.Load().Telegram;
            if (!telegram.IsConfigured)
            {
                logger.LogWarning("Telegram is not configured. Daily report was skipped.");
                return;
            }

            var allOffers = await dataService.GetAllPhoneOffersForExportAsync();
            var toppedUpInRound = allOffers
                .Where(offer => IsToppedUpDuringRound(offer, roundStartedAt, roundCompletedAt))
                .ToList();

            var reportDateLabel = roundCompletedAt.ToString("dd.MM.yyyy");
            var fullReportFileName = $"{reportDateLabel} - полный отчёт.xlsx";
            var toppedUpReportFileName = $"{reportDateLabel} - {toppedUpInRound.Count} шт пополнены.xlsx";

            logger.LogInfo(
                $"Preparing daily Telegram reports for {reportDateLabel}. " +
                $"Total offers: {allOffers.Count}, topped up in round: {toppedUpInRound.Count}");

            var fullReportContent = await excelExporter.ExportAsync(allOffers, cancellationToken);
            await telegramService.SendDocumentAsync(
                telegram.BotToken,
                telegram.ChatId,
                fullReportContent,
                fullReportFileName,
                caption: $"Полный отчёт за {reportDateLabel}",
                cancellationToken);

            var toppedUpReportContent = await excelExporter.ExportAsync(toppedUpInRound, cancellationToken);
            await telegramService.SendDocumentAsync(
                telegram.BotToken,
                telegram.ChatId,
                toppedUpReportContent,
                toppedUpReportFileName,
                caption: $"Пополнённые номера за {reportDateLabel}: {toppedUpInRound.Count} шт.",
                cancellationToken);

            logger.LogInfo($"Daily Telegram reports sent for {reportDateLabel}");
        }

        private static bool IsToppedUpDuringRound(PhoneOffer offer, DateTime roundStartedAt, DateTime roundCompletedAt)
        {
            if (!offer.IsTopUpSynced || offer.TopUpSyncedAt == null)
            {
                return false;
            }

            var syncedAt = offer.TopUpSyncedAt.Value.ToLocalTime();
            return syncedAt >= roundStartedAt && syncedAt <= roundCompletedAt;
        }
    }
}
