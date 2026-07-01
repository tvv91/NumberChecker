using VodafoneNumberChecker.Models;

namespace VodafoneNumberChecker.Services
{
    public interface IAppSettingsService
    {
        AppSettings Load();
    }
}
