using VodafoneLogin.Models;

namespace VodafoneLogin.Services
{
    public interface IDataService
    {
        Task<int> SavePhoneOfferAsync(string phoneNumber, Offer offer);
        Task<int> ImportPhoneNumberAsync(string phoneNumber);
        Task<int> ImportPhoneNumbersAsync(List<string> phoneNumbers);
        Task<int?> GetLastProcessedPhoneIdAsync();
        Task SetLastProcessedPhoneIdAsync(int? phoneId);
        Task<List<PhoneOffer>> GetPhoneOffersAsync(int skip = 0, int take = 50, string? phoneFilter = null, bool? hasDiscount = null, bool? hasGift = null, bool? isEmptyProposition = null);
        Task<int> GetPhoneOffersCountAsync(string? phoneFilter = null, bool? hasDiscount = null, bool? hasGift = null, bool? isEmptyProposition = null);
        Task<List<PhoneOffer>> GetUnprocessedPhoneOffersAsync();
        Task<List<PhoneOffer>> GetEmptyPropositionsAsync();
        Task<List<PhoneOffer>> GetErrorNumbersAsync();
        Task MarkPhoneOfferAsProcessedAsync(int phoneOfferId);
        Task SetPhoneOfferErrorAsync(int phoneOfferId, string error);
        Task ClearPhoneOfferErrorAsync(int phoneOfferId);
        Task ResetScannedAsync();
        Task ResetAllAsync();
        Task ClearAllPhoneOffersAsync();
        Task EnsureDatabaseCreatedAsync();
    }
}

