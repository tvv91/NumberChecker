using System.Text.Json;
using VodafoneLogin.Models;

namespace VodafoneLogin.Services
{
    public class PhoneSearchService : IPhoneSearchService
    {
        private readonly IWebViewService _webViewService;
        private readonly IFileService _fileService;
        private readonly Random _rand = new();

        private const string _phoneNumbersFile = "numbers.txt";
        private const string _logFile = "errors.log";
        private const string _errorNumbers = "errornumbers.log";
        private const string _progressFile = "progress.txt";
        private const string _resultFile = "results.txt";

        public PhoneSearchService(IWebViewService webViewService, IFileService fileService)
        {
            _webViewService = webViewService;
            _fileService = fileService;
        }

        public async Task ProcessPhoneNumberAsync(
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
            Action<int>? serverErrorsCallback = null)
        {
            currentNumberCallback?.Invoke(phoneNumber);

            try
            {
                await InputPhoneNumberAsync(phoneNumber, delayInputMin, delayInputMax, progressInputCallback);
                await ClickSearchButtonAsync(delaySearchMin, delaySearchMax, progressSearchCallback);
                
                bool hasServerError = await ProcessSearchResultsAsync(
                    phoneNumber, 
                    offersFoundCallback, 
                    serverErrorsCallback);

                if (hasServerError)
                {
                    await HandleServerErrorAsync(phoneNumber, index, processedCallback, serverErrorsCallback);
                    return;
                }

                await SaveProgressAsync(index, processedCallback);
                await DelayBeforeNextAsync(delayNextMin, delayNextMax, progressNextCallback);
            }
            catch (Exception ex) when (ex.Message == "SERVER_ERROR")
            {
                await HandleServerErrorAsync(phoneNumber, index, processedCallback, serverErrorsCallback);
            }
            catch (Exception ex)
            {
                await HandleGeneralErrorAsync(phoneNumber, index, ex, processedCallback);
            }
        }

        private async Task InputPhoneNumberAsync(
            string phoneNumber,
            double delayInputMin,
            double delayInputMax,
            Action<double>? progressInputCallback)
        {
            await _webViewService.WaitForElementAsync("#phoneNumber");

            int delayInput = GetRandomDelay(delayInputMin, delayInputMax);
            await DelayWithProgressAsync(delayInput, progressInputCallback);

            await _webViewService.ExecuteScriptAsync($@"
            (function(){{
                const input = document.getElementById('phoneNumber');
                if (input) {{
                    input.focus();
                    input.value = '{phoneNumber}';
                    input.dispatchEvent(new InputEvent('input', {{ bubbles: true, cancelable: true, composed: true }}));
                    input.dispatchEvent(new Event('change', {{ bubbles: true }}));
                }}
            }})()
            ");
        }

        private async Task ClickSearchButtonAsync(
            double delaySearchMin,
            double delaySearchMax,
            Action<double>? progressSearchCallback)
        {
            await _webViewService.WaitForButtonByTextAsync("Пошук");

            int delaySearch = GetRandomDelay(delaySearchMin, delaySearchMax);
            await DelayWithProgressAsync(delaySearch, progressSearchCallback);

            await _webViewService.ExecuteScriptAsync(@"
            (function(){
                const buttons = [...document.querySelectorAll('button')];
                const searchBtn = buttons.find(b => b.innerText.includes('Пошук'));
                if (searchBtn) {
                    searchBtn.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true, view: window }));
                }
            })()
            ");
        }

        private async Task<bool> ProcessSearchResultsAsync(
            string phoneNumber,
            Action<int>? offersFoundCallback,
            Action<int>? serverErrorsCallback)
        {
            await _webViewService.WaitForOffersLoadedAsync();

            if (await _webViewService.CheckServerErrorToastAsync())
            {
                return true;
            }

            string rawOffersJson = await _webViewService.GetOffersJsonAsync();
            string offersJson = JsonSerializer.Deserialize<string>(rawOffersJson) ?? "[]";
            var offers = JsonSerializer.Deserialize<List<Offer>>(offersJson);

            if (offers != null && offers.Count > 0)
            {
                await SaveOfferResultAsync(phoneNumber, offers[0], offersFoundCallback);
            }

            return false;
        }

        private async Task SaveOfferResultAsync(
            string phoneNumber,
            Offer offer,
            Action<int>? offersFoundCallback)
        {
            offersFoundCallback?.Invoke(1);
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string line = $"[{timestamp}] Номер: {phoneNumber}, Скидка: {offer.discount}%, Мин. пополнение: {offer.topUp} грн, Действует до: {offer.validUntil}{Environment.NewLine}";
            _fileService.AppendResult(line, _resultFile);
            await Task.CompletedTask;
        }

        private async Task HandleServerErrorAsync(
            string phoneNumber,
            int index,
            Action<int>? processedCallback,
            Action<int>? serverErrorsCallback)
        {
            serverErrorsCallback?.Invoke(1);
            _fileService.AppendErrorNumber(phoneNumber, _errorNumbers);
            processedCallback?.Invoke(1);
            _fileService.WriteProgress(index, _progressFile);
            await Task.Delay(5000);
        }

        private async Task HandleGeneralErrorAsync(
            string phoneNumber,
            int index,
            Exception ex,
            Action<int>? processedCallback)
        {
            _fileService.AppendErrorNumber(phoneNumber, _errorNumbers);
            _fileService.AppendLog(
                $"{DateTime.Now}: Ошибка при обработке номера {phoneNumber}\n{ex}\n",
                _logFile);
            processedCallback?.Invoke(1);
            _fileService.WriteProgress(index, _progressFile);
            await Task.CompletedTask;
        }

        private async Task SaveProgressAsync(
            int index,
            Action<int>? processedCallback)
        {
            processedCallback?.Invoke(1);
            _fileService.WriteProgress(index, _progressFile);
            await Task.CompletedTask;
        }

        private async Task DelayBeforeNextAsync(
            double delayNextMin,
            double delayNextMax,
            Action<double>? progressNextCallback)
        {
            int delayNext = GetRandomDelay(delayNextMin, delayNextMax);
            await DelayWithProgressAsync(delayNext, progressNextCallback);
        }

        private async Task DelayWithProgressAsync(int milliseconds, Action<double>? progressCallback)
        {
            if (progressCallback == null)
            {
                await Task.Delay(milliseconds);
                return;
            }

            int interval = 50;
            int steps = milliseconds / interval;
            for (int i = 0; i <= steps; i++)
            {
                progressCallback((i * 100.0) / steps);
                await Task.Delay(interval);
            }
            progressCallback(0);
        }

        public int GetRandomDelay(double minSeconds, double maxSeconds)
        {
            if (minSeconds > maxSeconds)
            {
                var temp = minSeconds;
                minSeconds = maxSeconds;
                maxSeconds = temp;
            }
            return _rand.Next((int)(minSeconds * 1000), (int)(maxSeconds * 1000) + 1);
        }
    }
}

