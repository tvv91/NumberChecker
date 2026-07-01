using VodafoneNumberChecker.Models;

namespace VodafoneNumberChecker.Services
{
    public interface IPhoneOffersExcelExporter
    {
        Task<byte[]> ExportAsync(IReadOnlyList<PhoneOffer> offers, CancellationToken cancellationToken = default);
    }
}
