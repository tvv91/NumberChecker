using VodafoneNumberChecker.Models;

namespace VodafoneNumberChecker.Services
{
    public interface ITopUpSyncService
    {
        Task TrySyncAsync(PhoneOffer offer, ProcessingConfiguration configuration, CancellationToken cancellationToken = default);
    }
}
