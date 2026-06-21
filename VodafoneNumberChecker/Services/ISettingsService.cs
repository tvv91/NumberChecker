using VodafoneNumberChecker.Models;

namespace VodafoneNumberChecker.Services
{
    public interface ISettingsService
    {
        ProcessingConfiguration Load();
        void Save(ProcessingConfiguration configuration);
    }
}
