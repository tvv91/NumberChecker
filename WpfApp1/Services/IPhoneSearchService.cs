using VodafoneLogin.Models;

namespace VodafoneLogin.Services
{
    public interface IPhoneSearchService
    {
        Task ProcessPhoneNumberAsync(
            string phoneNumber, 
            int index,
            double delayInputMin,
            double delayInputMax,
            double delaySearchMin,
            double delaySearchMax,
            double delayNextMin,
            double delayNextMax,
            Action<double>? progressInputCallback = null,
            Action<double>? progressSearchCallback = null,
            Action<double>? progressNextCallback = null,
            Action<string>? currentNumberCallback = null,
            Action<int>? processedCallback = null,
            Action<int>? offersFoundCallback = null,
            Action<int>? serverErrorsCallback = null);
    }
}

