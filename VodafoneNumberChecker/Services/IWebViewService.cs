using Microsoft.Web.WebView2.Wpf;

namespace VodafoneNumberChecker.Services
{
    public interface IWebViewService
    {
        WebView2 WebView { get; set; }
        Task InitializeAsync();
        Task NavigateAsync(string url);
        Task WaitForElementAsync(string selector);
        Task WaitForButtonByTextAsync(string text, int timeoutMs = 5000);
        Task WaitForOffersLoadedAsync(int timeoutMs = 8000);
        Task<bool> CheckServerErrorToastAsync();
        Task<string> GetOffersJsonAsync();
        Task<string> GetPropositionTypesJsonAsync();
        Task ExecuteScriptAsync(string script);
        Task<bool> WaitForLoginOrNumberFieldAsync(string numberFieldSelector = "#phoneNumber", int timeoutMs = 5000);
        Task<bool> CheckAuthenticationAsync();
        string? CurrentUrl { get; }
        event EventHandler<string>? NavigationCompleted;
    }
}

