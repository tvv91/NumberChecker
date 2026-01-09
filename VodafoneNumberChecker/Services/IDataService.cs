using VodafoneNumberChecker.Models;

namespace VodafoneNumberChecker.Services
{
    public interface IDataService
    {
        Task<int> SavePhoneOfferAsync(string phoneNumber, Offer offer, bool isPropositionsNotFound = false, bool isPropositionsNotSuitable = false);
        Task<int> ImportPhoneNumberAsync(string phoneNumber);
        Task<int> ImportPhoneNumbersAsync(List<string> phoneNumbers, Action<int, int, string?>? progressCallback = null);
        Task<int?> GetLastProcessedPhoneIdAsync();
        Task SetLastProcessedPhoneIdAsync(int? phoneId);
        Task<List<PhoneOffer>> GetPhoneOffersAsync(int skip = 0, int take = 50, string? phoneFilter = null, bool? hasDiscount = null, bool? hasGift = null, bool? isEmptyProposition = null, bool? hasError = null, bool? isPropositionsNotFound = null, bool? isPropositionsNotSuitable = null);
        Task<int> GetPhoneOffersCountAsync(string? phoneFilter = null, bool? hasDiscount = null, bool? hasGift = null, bool? isEmptyProposition = null, bool? hasError = null, bool? isPropositionsNotFound = null, bool? isPropositionsNotSuitable = null);
        Task<List<PhoneOffer>> GetUnprocessedPhoneOffersAsync();
        Task<List<PhoneOffer>> GetEmptyPropositionsAsync();
        Task<List<PhoneOffer>> GetErrorNumbersAsync();
        Task MarkPhoneOfferAsProcessedAsync(int phoneOfferId);
        Task IncrementIterationCountAsync(int phoneOfferId);
        Task SetPhoneOfferErrorAsync(int phoneOfferId, string error);
        Task ClearPhoneOfferErrorAsync(int phoneOfferId);
        Task ResetScannedAsync();
        Task ResetAllAsync();
        Task ClearAllPhoneOffersAsync();
        Task EnsureDatabaseCreatedAsync();
        Task SavePropositionTypeAsync(string title, string content);
        Task<List<PropositionType>> GetPropositionTypesAsync();
        Task<List<PhoneOffer>> GetAllPhoneOffersForExportAsync(string? phoneFilter = null, bool? hasDiscount = null, bool? hasGift = null, bool? isEmptyProposition = null, bool? hasError = null, bool? isPropositionsNotFound = null, bool? isPropositionsNotSuitable = null);
    }
}

