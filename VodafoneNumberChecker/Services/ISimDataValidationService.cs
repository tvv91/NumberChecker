using VodafoneNumberChecker.Models;

namespace VodafoneNumberChecker.Services
{
    public interface ISimDataValidationService
    {
        Task<SimDataValidationResult> ValidateNumbersExistAsync(
            IReadOnlyCollection<string> numbers,
            Action<int, int, string?>? progressCallback = null,
            CancellationToken cancellationToken = default);
    }
}
