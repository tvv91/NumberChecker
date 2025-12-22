using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace VodafoneLogin
{
    public partial class MainWindow : Window
    {
        // файл с номерами телефонов
        private const string _phoneNumbersFile = "numbers.txt";
        // пока что логируем ошибки в файл если что-то пойдет не так
        private const string _logFile = "errors.log";
        private const string _errorNumbers = "errornumbers.log";
        // файл где сохраняется последний обработанный номер, и если этот файл есть, продолжаем обработку с этого номера
        private const string _progressFile = "progress.txt";
        // файл с результатами
        private const string _resultFile = "results.txt";
        // список номеров
        private List<string> _phoneNumbers = new();
        private Random _rand = new Random();

        // вывод статистики на форму
        private int totalNumbers = 0;
        private int processedCount = 0;
        private int offersFoundCount = 0;

        public MainWindow()
        {
            // загружам номера из файла
            if (File.Exists(_phoneNumbersFile))
            {
                _phoneNumbers = File.ReadAllLines(_phoneNumbersFile)
                    .Select(l => l.Trim())      // убираем пробелы
                    .Where(l => !string.IsNullOrEmpty(l)) // игнорируем пустые строки
                    .ToList();
            }
            else
            {
                MessageBox.Show($"Файл {_phoneNumbersFile} не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            InitializeComponent();
            Init();            
        }

        private async void Init()
        {
            // инициализируем WebView2
            await webView.EnsureCoreWebView2Async();

            // перемещаемся на страницу поиска
            webView.CoreWebView2.Navigate("https://partner.vodafone.ua/dashboard/personal-offers-main/personal-offers");
        }

        private int GetRandomDelay(double minSeconds, double maxSeconds)
        {
            if (minSeconds > maxSeconds)
            {
                // переставляем, если пользователь поставил Min больше Max
                var temp = minSeconds;
                minSeconds = maxSeconds;
                maxSeconds = temp;
            }
            return _rand.Next((int)(minSeconds * 1000), (int)(maxSeconds * 1000) + 1);
        }

        private async Task DelayWithProgress(int milliseconds, ProgressBar progressBar)
        {
            int interval = 50; // обновляем каждые 50 мс
            int steps = milliseconds / interval;
            for (int i = 0; i <= steps; i++)
            {
                progressBar.Value = (i * 100.0) / steps;
                await Task.Delay(interval);
            }
            progressBar.Value = 0;
        }

        
        // тут происходит магия))
        private async Task StartPhoneSearch()
        {
            totalNumbers = _phoneNumbers.Count;
            txtTotalNumbers.Text = $"Всего номеров: {totalNumbers}";

            int lastProcessedIndex = -1;
            if (File.Exists(_progressFile))
            {
                var text = File.ReadAllText(_progressFile);
                if (int.TryParse(text, out int index))
                    lastProcessedIndex = index;
            }

            for (int i = lastProcessedIndex + 1; i < _phoneNumbers.Count; i++)
            {
                var number = _phoneNumbers[i];
                txtCurrentNumber.Text = $"Текущий номер: {number}";

                try
                {
                    await WaitForElement("#phoneNumber");

                    // Задержка перед вводом номера
                    await DelayWithProgress(GetRandomDelay(DelayInputMin.Value, DelayInputMax.Value), ProgressInput);

                    await webView.CoreWebView2.ExecuteScriptAsync($@"
            (function(){{
                const input = document.getElementById('phoneNumber');
                if (input) {{
                    input.focus();
                    input.value = '{number}';
                    input.dispatchEvent(new InputEvent('input', {{ bubbles: true, cancelable: true, composed: true }}));
                    input.dispatchEvent(new Event('change', {{ bubbles: true }}));
                }}
            }})()
            ");

                    await WaitForButtonByText("Пошук");

                    // Задержка перед нажатием поиска
                    await DelayWithProgress(GetRandomDelay(DelaySearchMin.Value, DelaySearchMax.Value), ProgressSearch);

                    await webView.CoreWebView2.ExecuteScriptAsync(@"
            (function(){
                const buttons = [...document.querySelectorAll('button')];
                const searchBtn = buttons.find(b => b.innerText.includes('Пошук'));
                if (searchBtn) {
                    searchBtn.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true, view: window }));
                }
            })()
            ");

                    await WaitForOffersLoaded();

                    if (await CheckServerErrorToast())
                    {
                        File.AppendAllText(_errorNumbers, $"{number}{Environment.NewLine}");
                        processedCount++;
                        txtProcessed.Text = $"Обработано: {processedCount}";
                        File.WriteAllText(_progressFile, i.ToString());
                        await Task.Delay(2000);
                        continue;
                    }

                    string rawOffersJson = await GetOffersJson();
                    string offersJson = System.Text.Json.JsonSerializer.Deserialize<string>(rawOffersJson);
                    var offers = System.Text.Json.JsonSerializer.Deserialize<List<Offer>>(offersJson ?? "[]");

                    if (offers != null && offers.Count > 0)
                    {
                        offersFoundCount++;
                        txtOffersFound.Text = $"Найдено предложений: {offersFoundCount}";
                        var offer = offers[0];
                        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        string line = $"[{timestamp}] Номер: {number}, Скидка: {offer.discount}%, Мин. пополнение: {offer.topUp} грн, Действует до: {offer.validUntil}{Environment.NewLine}";
                        File.AppendAllText(_resultFile, line);
                    }

                    processedCount++;
                    txtProcessed.Text = $"Обработано: {processedCount}";
                    File.WriteAllText(_progressFile, i.ToString());

                }
                catch (Exception ex) when (ex.Message == "SERVER_ERROR")
                {
                    processedCount++;
                    txtProcessed.Text = $"Обработано: {processedCount}";
                    File.AppendAllText(_errorNumbers, $"{number}{Environment.NewLine}");
                    File.WriteAllText(_progressFile, i.ToString());
                    await Task.Delay(5000);
                    continue;
                }
                catch (Exception ex)
                {
                    processedCount++;
                    txtProcessed.Text = $"Обработано: {processedCount}";
                    File.AppendAllText(_logFile,
                        $"{DateTime.Now}: Ошибка при обработке номера {number}\n{ex}\n");

                    MessageBox.Show($"Произошла ошибка с номером {number}, продолжаем...",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);

                    File.WriteAllText(_progressFile, i.ToString());
                    continue;
                }

                // Задержка перед обработкой следующего номера
                await DelayWithProgress(GetRandomDelay(DelayNextMin.Value, DelayNextMax.Value), ProgressNext);
            }

            MessageBox.Show("Обработка завершена", "^_^", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        async Task<bool> CheckServerErrorToast()
        {
            string result = await webView.CoreWebView2.ExecuteScriptAsync(@"
        (function(){
            const labels = [...document.querySelectorAll('.mat-mdc-snack-bar-label')];
            return labels.some(l => l.innerText.includes('Внутрішня помилка сервера')) ? '1' : '0';
        })();
    ");

            return result.Contains("1");
        }

        private async void webView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {            
            await StartPhoneSearch();
        }

        // Этот метод необходим для проверки что загрузился Angular компонент полем для ввода номера телефона
        // иначе скрипт в webView_NavigationCompleted пытается сразу заполнить поле номера, которого еще нет на странице
        async Task WaitForElement(string selector)
        {
            for (int i = 0; i < 50; i++) // максимум 5 секунд
            {
                string exists = await webView.CoreWebView2.ExecuteScriptAsync($@"document.querySelector('{selector}') ? '1' : '0';");

                if (exists.Contains("1"))
                    return;

                await Task.Delay(100);
            }

            throw new Exception($"Element {selector} not found within timeout.");
        }

        async Task<string> GetOffersJson()
        {
            return await webView.CoreWebView2.ExecuteScriptAsync(@"
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

        // метод ожидания появления предложения
        async Task WaitForOffersLoaded(int timeoutMs = 8000)
        {
            int elapsed = 0;
            int step = 200;

            while (elapsed < timeoutMs)
            {
                // Проверяем ошибку сервера
                string error = await webView.CoreWebView2.ExecuteScriptAsync(@"
            (function(){
                const labels = [...document.querySelectorAll('.mat-mdc-snack-bar-label')];
                return labels.some(l => l.innerText.includes('Внутрішня помилка сервера')) ? '1' : '0';
            })()
        ");

                if (error.Contains("1"))
                    throw new Exception("SERVER_ERROR");

                // Проверяем загрузку предложений
                string exists = await webView.CoreWebView2.ExecuteScriptAsync(@"
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

        async Task<bool> WaitForLoginOrNumberField(string numberFieldSelector = "#phoneNumber", int timeoutMs = 5000)
        {
            int elapsed = 0;
            int interval = 200;

            while (elapsed < timeoutMs)
            {
                // проверяем, залогинен ли пользователь
                string isLoginPage = await webView.CoreWebView2.ExecuteScriptAsync(@"
            document.querySelector('#username') ? '1' : '0';
        ");
                if (isLoginPage.Contains("1"))
                {
                    return false; // не залогинен
                }

                // проверяем, появился ли input для номера
                string numberExists = await webView.CoreWebView2.ExecuteScriptAsync($@"document.querySelector('{numberFieldSelector}') ? '1' : '0';");
                if (numberExists.Contains("1"))
                {
                    return true; // поле готово
                }

                await Task.Delay(interval);
                elapsed += interval;
            }

            return false; // таймаут
        }

        async Task WaitForButtonByText(string text, int timeoutMs = 5000)
        {
            int elapsed = 0;
            int interval = 100;
            while (elapsed < timeoutMs)
            {
                string exists = await webView.CoreWebView2.ExecuteScriptAsync($@"[...document.querySelectorAll('button')].some(b => b.innerText.includes('{text}')) ? '1' : '0';");

                if (exists.Contains("1")) return;

                await Task.Delay(interval);
                elapsed += interval;
            }

            throw new Exception($"Button '{text}' not found within timeout");
        }
    }
}