using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Threading;
using VodafoneLogin.Models;
using VodafoneLogin.Services;

namespace VodafoneLogin.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged, IProgressReporter
    {
        private readonly IFileService _fileService;
        private readonly IWebViewService _webViewService;
        private readonly IPhoneSearchService _phoneSearchService;
        private readonly IDataService _dataService;
        private readonly PhoneOffersViewModel? _phoneOffersViewModel;
        private readonly ConfigViewModel _configViewModel;

        private List<string> _phoneNumbers = new();
        private int _totalNumbers;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isScanning;
        private int _processedCount;
        private int _offersFoundCount;
        private int _serverErrors;
        private string _currentNumber = "-";
        private double _progressInput;
        private double _progressSearch;
        private double _progressNext;

        public MainWindowViewModel(IFileService fileService, IWebViewService webViewService, IPhoneSearchService phoneSearchService, IDataService dataService, PhoneOffersViewModel? phoneOffersViewModel = null, ConfigViewModel? configViewModel = null)
        {
            _fileService = fileService;
            _webViewService = webViewService;
            _phoneSearchService = phoneSearchService;
            _dataService = dataService;
            _phoneOffersViewModel = phoneOffersViewModel;
            _configViewModel = configViewModel ?? new ConfigViewModel();

            ImportPhoneNumbersCommand = new RelayCommand(ImportPhoneNumbers);
            StartStopScanCommand = new RelayCommand(async () => await StartStopScanAsync());
            OpenConfigCommand = new RelayCommand(OpenConfig);
            ResetScannedCommand = new RelayCommand(async () => await ResetScannedAsync(), () => !_isScanning);
            ResetAllCommand = new RelayCommand(async () => await ResetAllAsync(), () => !_isScanning);
        }

        public ICommand ImportPhoneNumbersCommand { get; }
        public ICommand StartStopScanCommand { get; }
        public ICommand OpenConfigCommand { get; }
        public ICommand ResetScannedCommand { get; }
        public ICommand ResetAllCommand { get; }

        public bool IsScanning
        {
            get => _isScanning;
            set
            {
                _isScanning = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ScanButtonText));
                ((RelayCommand)ResetScannedCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ResetAllCommand).RaiseCanExecuteChanged();
            }
        }

        public string ScanButtonText => IsScanning ? "Остановить" : "Старт/Продолжить";

        public int TotalNumbers
        {
            get => _totalNumbers;
            set
            {
                _totalNumbers = value;
                OnPropertyChanged();
            }
        }

        public int ProcessedCount
        {
            get => _processedCount;
            set
            {
                _processedCount = value;
                OnPropertyChanged();
            }
        }

        public int OffersFoundCount
        {
            get => _offersFoundCount;
            set
            {
                _offersFoundCount = value;
                OnPropertyChanged();
            }
        }

        public int ServerErrors
        {
            get => _serverErrors;
            set
            {
                _serverErrors = value;
                OnPropertyChanged();
            }
        }

        public string CurrentNumber
        {
            get => _currentNumber;
            set
            {
                _currentNumber = value;
                OnPropertyChanged();
            }
        }

        public double ProgressInput
        {
            get => _progressInput;
            set
            {
                _progressInput = value;
                OnPropertyChanged();
            }
        }

        public double ProgressSearch
        {
            get => _progressSearch;
            set
            {
                _progressSearch = value;
                OnPropertyChanged();
            }
        }

        public double ProgressNext
        {
            get => _progressNext;
            set
            {
                _progressNext = value;
                OnPropertyChanged();
            }
        }

        private async void ImportPhoneNumbers()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Title = "Выберите файл с номерами телефонов"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _phoneNumbers = _fileService.ReadPhoneNumbers(dialog.FileName);
                    
                    // Import phone numbers to database with default states
                    int importedCount = await _dataService.ImportPhoneNumbersAsync(_phoneNumbers);
                    TotalNumbers = _phoneNumbers.Count;
                    
                    // Refresh PhoneOffersViewModel to show newly imported numbers
                    if (_phoneOffersViewModel != null)
                    {
                        await _phoneOffersViewModel.LoadDataAsync();
                    }
                    
                    System.Windows.MessageBox.Show(
                        $"Импортировано {importedCount} из {_phoneNumbers.Count} номеров телефонов в базу данных",
                        "Импорт завершен",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"Ошибка при импорте файла: {ex.Message}",
                        "Ошибка",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
            }
        }

        public async Task InitializeWebViewAsync()
        {
            await _webViewService.InitializeAsync();
            await _webViewService.NavigateAsync("https://partner.vodafone.ua/dashboard/personal-offers-main/personal-offers");
        }

        public async Task StartPhoneSearchAsync()
        {
            // This method is kept for backward compatibility with webView navigation
            await StartContinueScanAsync();
        }

        public async Task StartStopScanAsync()
        {
            if (IsScanning)
            {
                await StopScanAsync();
                return;
            }

            await StartContinueScanAsync();
        }

        private async Task StartContinueScanAsync()
        {

            var unprocessedOffers = await _dataService.GetUnprocessedPhoneOffersAsync();
            
            if (unprocessedOffers.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "Нет необработанных номеров для сканирования",
                    "Нет данных",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
                return;
            }

            // Create cancellation token source
            _cancellationTokenSource = new CancellationTokenSource();
            IsScanning = true;

            TotalNumbers = unprocessedOffers.Count;
            ProcessedCount = 0;
            OffersFoundCount = 0;
            ServerErrors = 0;

            var configuration = _configViewModel.GetConfiguration();

            try
            {
                foreach (var phoneOffer in unprocessedOffers)
                {
                    // Check for cancellation
                    _cancellationTokenSource.Token.ThrowIfCancellationRequested();

                    // Get current configuration (in case user changed it in config window)
                    configuration = _configViewModel.GetConfiguration();

                    await _phoneSearchService.ProcessPhoneOfferAsync(phoneOffer, configuration, this, _cancellationTokenSource.Token);
                }

                // Refresh PhoneOffersViewModel
                if (_phoneOffersViewModel != null)
                {
                    await _phoneOffersViewModel.LoadDataAsync();
                }

                System.Windows.MessageBox.Show("Обработка завершена", "^_^", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (OperationCanceledException)
            {
                // Reset progress bars
                ProgressInput = 0;
                ProgressSearch = 0;
                ProgressNext = 0;

                // Refresh PhoneOffersViewModel
                if (_phoneOffersViewModel != null)
                {
                    await _phoneOffersViewModel.LoadDataAsync();
                }

                System.Windows.MessageBox.Show(
                    "Сканирование остановлено пользователем",
                    "Остановлено",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            finally
            {
                // Ensure progress bars are reset
                ProgressInput = 0;
                ProgressSearch = 0;
                ProgressNext = 0;
                
                IsScanning = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private async Task StopScanAsync()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                
                // Reset progress bars
                ProgressInput = 0;
                ProgressSearch = 0;
                ProgressNext = 0;
            }
            await Task.CompletedTask;
        }

        private void OpenConfig()
        {
            var configWindow = new ConfigWindow(_configViewModel);
            configWindow.Owner = System.Windows.Application.Current.MainWindow;
            configWindow.ShowDialog();
        }

        public async Task ResetScannedAsync()
        {
            var result = System.Windows.MessageBox.Show(
                "Сбросить статус обработки для всех записей?",
                "Подтверждение",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                await _dataService.ResetScannedAsync();
                
                // Refresh PhoneOffersViewModel
                if (_phoneOffersViewModel != null)
                {
                    await _phoneOffersViewModel.LoadDataAsync();
                }

                System.Windows.MessageBox.Show(
                    "Статус обработки сброшен для всех записей",
                    "Готово",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
        }

        public async Task ResetAllAsync()
        {
            var result = System.Windows.MessageBox.Show(
                "Сбросить все записи к состоянию по умолчанию? Это действие нельзя отменить.",
                "Подтверждение",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                await _dataService.ResetAllAsync();
                
                // Refresh PhoneOffersViewModel
                if (_phoneOffersViewModel != null)
                {
                    await _phoneOffersViewModel.LoadDataAsync();
                }

                System.Windows.MessageBox.Show(
                    "Все записи сброшены к состоянию по умолчанию",
                    "Готово",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
        }

        // IProgressReporter implementation
        public void ReportInputProgress(double progress)
        {
            ProgressInput = progress;
        }

        public void ReportSearchProgress(double progress)
        {
            ProgressSearch = progress;
        }

        public void ReportNextProgress(double progress)
        {
            ProgressNext = progress;
        }

        public void ReportCurrentNumber(string phoneNumber)
        {
            CurrentNumber = phoneNumber;
        }

        public void ReportProcessed(int count)
        {
            ProcessedCount += count;
        }

        public void ReportOffersFound(int count)
        {
            OffersFoundCount += count;
        }

        public void ReportServerErrors(int count)
        {
            ServerErrors += count;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

