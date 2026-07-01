using OfficeOpenXml;
using VodafoneNumberChecker.Models;

namespace VodafoneNumberChecker.Services
{
    public class PhoneOffersExcelExporter : IPhoneOffersExcelExporter
    {
        private const int ColumnCount = 16;

        public async Task<byte[]> ExportAsync(IReadOnlyList<PhoneOffer> offers, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Phone Offers");

            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Номер телефона";
            worksheet.Cells[1, 3].Value = "Скидка %";
            worksheet.Cells[1, 4].Value = "Мин. пополнение";
            worksheet.Cells[1, 5].Value = "Подарок";
            worksheet.Cells[1, 6].Value = "Дней действия";
            worksheet.Cells[1, 7].Value = "Действует до";
            worksheet.Cells[1, 8].Value = "Создано";
            worksheet.Cells[1, 9].Value = "Обновлено";
            worksheet.Cells[1, 10].Value = "Итераций";
            worksheet.Cells[1, 11].Value = "Обработано";
            worksheet.Cells[1, 12].Value = "Ошибка";
            worksheet.Cells[1, 13].Value = "Описание ошибки";
            worksheet.Cells[1, 14].Value = "Нет предложений";
            worksheet.Cells[1, 15].Value = "Нет подходящих";
            worksheet.Cells[1, 16].Value = "Пополнён";

            using (var range = worksheet.Cells[1, 1, 1, ColumnCount])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            for (int i = 0; i < offers.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var offer = offers[i];
                int row = i + 2;
                worksheet.Cells[row, 1].Value = offer.Id;
                worksheet.Cells[row, 2].Value = offer.PhoneNumber;
                worksheet.Cells[row, 3].Value = offer.DiscountPercent;
                worksheet.Cells[row, 4].Value = offer.MinTopupAmount;
                worksheet.Cells[row, 5].Value = offer.GiftAmount;
                worksheet.Cells[row, 6].Value = offer.ActiveDays;
                worksheet.Cells[row, 7].Value = offer.ValidUntil?.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 8].Value = offer.CreatedAt.ToString("yyyy-MM-dd HH:mm");
                worksheet.Cells[row, 9].Value = offer.UpdatedAt?.ToString("yyyy-MM-dd HH:mm");
                worksheet.Cells[row, 10].Value = offer.IterationCount;
                worksheet.Cells[row, 11].Value = offer.IsProcessed;
                worksheet.Cells[row, 12].Value = offer.IsError;
                worksheet.Cells[row, 13].Value = offer.ErrorDescription;
                worksheet.Cells[row, 14].Value = offer.IsPropositionsNotFound;
                worksheet.Cells[row, 15].Value = offer.IsPropositionsNotSuitable;
                worksheet.Cells[row, 16].Value = offer.IsTopUpSynced ? "Да" : "Нет";
            }

            worksheet.Cells.AutoFitColumns();

            return await package.GetAsByteArrayAsync(cancellationToken);
        }
    }
}
