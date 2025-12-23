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
    }
}

