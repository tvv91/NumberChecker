using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace VodafoneLogin.Services
{
    public class WebViewService : IWebViewService
    {
        public WebView2? WebView { get; set; }

        public async Task InitializeAsync()
        {
            if (WebView == null)
                throw new InvalidOperationException("WebView is not set");

            await WebView.EnsureCoreWebView2Async();
        }

        public async Task NavigateAsync(string url)
        {
            if (WebView?.CoreWebView2 == null)
                throw new InvalidOperationException("WebView is not initialized");

            WebView.CoreWebView2.Navigate(url);
            await Task.CompletedTask;
        }

        public async Task WaitForElementAsync(string selector)
        {
            if (WebView?.CoreWebView2 == null)
                throw new InvalidOperationException("WebView is not initialized");

            for (int i = 0; i < 50; i++) // максимум 5 секунд
            {
                string exists = await WebView.CoreWebView2.ExecuteScriptAsync($@"document.querySelector('{selector}') ? '1' : '0';");

                if (exists.Contains("1"))
                    return;

                await Task.Delay(100);
            }

            throw new Exception($"Element {selector} not found within timeout.");
        }

        public async Task WaitForButtonByTextAsync(string text, int timeoutMs = 5000)
        {
            if (WebView?.CoreWebView2 == null)
                throw new InvalidOperationException("WebView is not initialized");

            int elapsed = 0;
            int interval = 100;
            while (elapsed < timeoutMs)
            {
                string exists = await WebView.CoreWebView2.ExecuteScriptAsync($@"[...document.querySelectorAll('button')].some(b => b.innerText.includes('{text}')) ? '1' : '0';");

                if (exists.Contains("1")) return;

                await Task.Delay(interval);
                elapsed += interval;
            }

            throw new Exception($"Button '{text}' not found within timeout");
        }

        public async Task WaitForOffersLoadedAsync(int timeoutMs = 8000)
        {
            if (WebView?.CoreWebView2 == null)
                throw new InvalidOperationException("WebView is not initialized");

            int elapsed = 0;
            int step = 200;

            while (elapsed < timeoutMs)
            {
                // Проверяем ошибку сервера
                string error = await WebView.CoreWebView2.ExecuteScriptAsync(@"
            (function(){
                const labels = [...document.querySelectorAll('.mat-mdc-snack-bar-label')];
                return labels.some(l => l.innerText.includes('Внутрішня помилка сервера')) ? '1' : '0';
            })()
        ");

                if (error.Contains("1"))
                    throw new Exception("SERVER_ERROR");

                // Проверяем загрузку предложений
                string exists = await WebView.CoreWebView2.ExecuteScriptAsync(@"
            (function() {
                if (document.querySelector('mat-expansion-panel')) return '1';
                if ([...document.querySelectorAll('h6.text-center')].some(h => h.innerText.includes('не знайдено'))) return '0';
                return 'null';
            })()
        ");

                if (exists.Contains("1") || exists.Contains("0"))
                    return;

                await Task.Delay(step);
                elapsed += step;
            }
        }

        public async Task<bool> CheckServerErrorToastAsync()
        {
            if (WebView?.CoreWebView2 == null)
                throw new InvalidOperationException("WebView is not initialized");

            string result = await WebView.CoreWebView2.ExecuteScriptAsync(@"
        (function(){
            const labels = [...document.querySelectorAll('.mat-mdc-snack-bar-label')];
            return labels.some(l => l.innerText.includes('Внутрішня помилка сервера')) ? '1' : '0';
        })();
    ");

            return result.Contains("1");
        }

        public async Task<string> GetOffersJsonAsync()
        {
            if (WebView?.CoreWebView2 == null)
                throw new InvalidOperationException("WebView is not initialized");

            return await WebView.CoreWebView2.ExecuteScriptAsync(@"
                (function () {
                    const panels = [...document.querySelectorAll('mat-expansion-panel')];
                    const offers = [];

                    for (const panel of panels) {
                        const text = panel.innerText.replace(/\s+/g, ' ').trim();

                        const discount = text.match(/знижку\s+(\d+)%/i)?.[1] ?? null;
                        const topUp = text.match(/від\s+(\d+)\s*грн/i)?.[1] ?? null;
                        const validUntil = text.match(/до\s+(\d{4}-\d{2}-\d{2})/i)?.[1] ?? null;

                        if (discount || topUp || validUntil) {
                            offers.push({
                                discount,
                                topUp,
                                validUntil,
                                fullText: text
                            });
                        }
                    }

                    return JSON.stringify(offers);
                })();
            ");
        }

        public async Task ExecuteScriptAsync(string script)
        {
            if (WebView?.CoreWebView2 == null)
                throw new InvalidOperationException("WebView is not initialized");

            await WebView.CoreWebView2.ExecuteScriptAsync(script);
        }

        public async Task<bool> WaitForLoginOrNumberFieldAsync(string numberFieldSelector = "#phoneNumber", int timeoutMs = 5000)
        {
            if (WebView?.CoreWebView2 == null)
                throw new InvalidOperationException("WebView is not initialized");

            int elapsed = 0;
            int interval = 200;

            while (elapsed < timeoutMs)
            {
                // проверяем, залогинен ли пользователь
                string isLoginPage = await WebView.CoreWebView2.ExecuteScriptAsync(@"
            document.querySelector('#username') ? '1' : '0';
        ");
                if (isLoginPage.Contains("1"))
                {
                    return false; // не залогинен
                }

                // проверяем, появился ли input для номера
                string numberExists = await WebView.CoreWebView2.ExecuteScriptAsync($@"document.querySelector('{numberFieldSelector}') ? '1' : '0';");
                if (numberExists.Contains("1"))
                {
                    return true; // поле готово
                }

                await Task.Delay(interval);
                elapsed += interval;
            }

            return false; // таймаут
        }
    }
}

