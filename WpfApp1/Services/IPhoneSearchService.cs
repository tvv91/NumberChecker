using VodafoneLogin.Models;

namespace VodafoneLogin.Services
{
    public interface IPhoneSearchService
    {
        Task ProcessPhoneNumberAsync(
            string phoneNumber, 
            int index,
            ProcessingConfiguration configuration,
            IProgressReporter? progressReporter = null);
        
        Task ProcessPhoneOfferAsync(
            PhoneOffer phoneOffer,
            ProcessingConfiguration configuration,
            IProgressReporter? progressReporter = null,
            CancellationToken cancellationToken = default);
    }
}

