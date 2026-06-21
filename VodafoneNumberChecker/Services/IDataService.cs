using VodafoneNumberChecker.Models;

namespace VodafoneNumberChecker.Services
{
    public interface IDataService
    {
        Task<int> SavePhoneOfferAsync(string phoneNumber, Offer offer, bool isPropositionsNotFound = false, bool isPropositionsNotSuitable = false);
        Task<int> ImportPhoneNumberAsync(string phoneNumber, bool isPriority = false);
        Task<int> ImportPhoneNumbersAsync(List<string> phoneNumbers, bool isPriority = false, bool addToExisting = false, Action<int, int, string?>? progressCallback = null);
        Task<int?> GetLastProcessedPhoneIdAsync();
        Task SetLastProcessedPhoneIdAsync(int? phoneId);
        Task<List<PhoneOffer>> GetPhoneOffersAsync(int skip = 0, int take = 50, string? phoneFilter = null, bool? hasDiscount = null, bool? hasGift = null, bool? isEmptyProposition = null, bool? hasError = null, bool? isPropositionsNotFound = null, bool? isPropositionsNotSuitable = null, bool? isPriority = null);
        Task<int> GetPhoneOffersCountAsync(string? phoneFilter = null, bool? hasDiscount = null, bool? hasGift = null, bool? isEmptyProposition = null, bool? hasError = null, bool? isPropositionsNotFound = null, bool? isPropositionsNotSuitable = null, bool? isPriority = null);
        Task<PhoneOffer?> GetPhoneOfferByIdAsync(int phoneOfferId);
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
        Task<List<PhoneOffer>> GetAllPhoneOffersForExportAsync(string? phoneFilter = null, bool? hasDiscount = null, bool? hasGift = null, bool? isEmptyProposition = null, bool? hasError = null, bool? isPropositionsNotFound = null, bool? isPropositionsNotSuitable = null, bool? isPriority = null);
        Task<int> GetDiscountCountAsync(string? phoneFilter = null, bool? isPriority = null);
        Task<int> GetGiftCountAsync(string? phoneFilter = null, bool? isPriority = null);
        Task<int> GetErrorCountAsync(string? phoneFilter = null, bool? isPriority = null);
        Task<int> GetNotFoundCountAsync(string? phoneFilter = null, bool? isPriority = null);
        Task<int> GetNotSuitableCountAsync(string? phoneFilter = null, bool? isPriority = null);
        Task<IterationReport> SaveIterationReportAsync(IterationReport report);
        Task<List<IterationReport>> GetIterationReportsAsync();
        Task ClearIterationReportsAsync();
        Task SetTopUpSyncSuccessAsync(int phoneOfferId);
        Task SetTopUpSyncFailureAsync(int phoneOfferId, string error);
    }
}

