using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Threading;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using VodafoneNumberChecker.Models;
using VodafoneNumberChecker.Services;

namespace VodafoneNumberChecker.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged, IProgressReporter
    {
        private readonly IFileService _fileService;
        private readonly IWebViewService _webViewService;
        private readonly IPhoneSearchService _phoneSearchService;
        private readonly IDataService _dataService;
        private readonly ISimDataValidationService _simDataValidationService;
        private readonly PhoneOffersViewModel? _phoneOffersViewModel;
        private readonly ConfigViewModel _configViewModel;
        private readonly ILoggerService _logger;

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
        private bool _isAuthenticated;
        private string _iterationInfo = string.Empty;
        private int _currentIteration = 0;
        private int _totalIterations = 0;
        private IterationReport? _selectedIterationReport;

        public MainWindowViewModel(IFileService fileService, IWebViewService webViewService, IPhoneSearchService phoneSearchService, IDataService dataService, ISimDataValidationService simDataValidationService, PhoneOffersViewModel? phoneOffersViewModel = null, ConfigViewModel? configViewModel = null, ILoggerService? logger = null)
        {
            _fileService = fileService;
            _webViewService = webViewService;
            _phoneSearchService = phoneSearchService;
            _dataService = dataService;
            _simDataValidationService = simDataValidationService;
            _phoneOffersViewModel = phoneOffersViewModel;
            _configViewModel = configViewModel ?? throw new ArgumentNullException(nameof(configViewModel));
            _logger = logger ?? new FileLoggerService();

            // Subscribe to navigation events to check authentication
            _webViewService.NavigationCompleted += async (s, url) =>
            {
                await CheckAuthenticationStatusAsync();
            };

            ImportPhoneNumbersCommand = new RelayCommand(ImportPhoneNumbers, () => _isAuthenticated);
            StartStopScanCommand = new RelayCommand(async () => await StartStopScanAsync(), () => _isAuthenticated);
            OpenConfigCommand = new RelayCommand(OpenConfig, () => _isAuthenticated);
            ResetScannedCommand = new RelayCommand(async () => await ResetScannedAsync(), () => _isAuthenticated && !_isScanning);
            ResetAllCommand = new RelayCommand(async () => await ResetAllAsync(), () => _isAuthenticated && !_isScanning);
            ClearIterationHistoryCommand = new RelayCommand(
                async () => await ClearIterationHistoryAsync(),
                () => _isAuthenticated && !_isScanning && IterationReports.Count > 0);

            IterationReports.CollectionChanged += IterationReports_CollectionChanged;
            _configViewModel.PropertyChanged += ConfigViewModel_PropertyChanged;
        }

        private void ConfigViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ConfigViewModel.Is24x7Mode))
            {
                OnPropertyChanged(nameof(Is24x7Mode));
            }
        }

        public ICommand ImportPhoneNumbersCommand { get; }
        public ICommand StartStopScanCommand { get; }
        public ICommand OpenConfigCommand { get; }
        public ICommand ResetScannedCommand { get; }
        public ICommand ResetAllCommand { get; }
        public ICommand ClearIterationHistoryCommand { get; }

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
                ((RelayCommand)ClearIterationHistoryCommand).RaiseCanExecuteChanged();
            }
        }

        public string ScanButtonText => IsScanning ? "Остановить" : "Старт/Продолжить";

        public bool Is24x7Mode => _configViewModel.Is24x7Mode;

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

        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            set
            {
                if (_isAuthenticated != value)
                {
                    _isAuthenticated = value;
                    OnPropertyChanged();
                    // Update command states
                    ((RelayCommand)ImportPhoneNumbersCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)StartStopScanCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)OpenConfigCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)ResetScannedCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)ResetAllCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public string IterationInfo
        {
            get => _iterationInfo;
            set
            {
                _iterationInfo = value;
                OnPropertyChanged();
            }
        }

        public int CurrentIteration
        {
            get => _currentIteration;
            set
            {
                _currentIteration = value;
                OnPropertyChanged();
                UpdateIterationInfo();
            }
        }

        public int TotalIterations
        {
            get => _totalIterations;
            set
            {
                _totalIterations = value;
                OnPropertyChanged();
                UpdateIterationInfo();
            }
        }

        public ObservableCollection<IterationReport> IterationReports { get; } = new();

        public IterationReport? SelectedIterationReport
        {
            get => _selectedIterationReport;
            set
            {
                _selectedIterationReport = value;
                OnPropertyChanged();
            }
        }

        public async Task LoadIterationReportsAsync()
        {
            var reports = await _dataService.GetIterationReportsAsync();
            IterationReports.Clear();
            foreach (var report in reports)
            {
                IterationReports.Add(report);
            }

            SelectedIterationReport = IterationReports.FirstOrDefault();
        }

        private void UpdateIterationInfo()
        {
            if (TotalIterations > 0)
            {
                IterationInfo = $"Итерация {CurrentIteration} из {TotalIterations}";
            }
            else
            {
                IterationInfo = string.Empty;
            }
        }

        private async void ImportPhoneNumbers()
        {
            try
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
                        var importOptionsWindow = new ImportOptionsWindow
                        {
                            Owner = System.Windows.Application.Current.MainWindow
                        };

                        if (importOptionsWindow.ShowDialog() != true)
                        {
                            return;
                        }

                        bool isPriorityImport = importOptionsWindow.IsPriority;
                        bool addToExisting = importOptionsWindow.AddToExisting;

                        _logger.LogInfo($"Starting import from file: {dialog.FileName}. Priority: {isPriorityImport}. AddToExisting: {addToExisting}");
                        _phoneNumbers = _fileService.ReadPhoneNumbers(dialog.FileName);
                        
                        if (_phoneNumbers.Count == 0)
                        {
                            System.Windows.MessageBox.Show(
                                "Файл не содержит номеров телефонов",
                                "Предупреждение",
                                System.Windows.MessageBoxButton.OK,
                                System.Windows.MessageBoxImage.Warning);
                            return;
                        }
                        
                        // Show progress dialog (modal)
                        var progressWindow = new ImportProgressWindow
                        {
                            Owner = System.Windows.Application.Current.MainWindow
                        };
                        
                        // Run import in background task
                        int importedCount = 0;
                        Exception? importException = null;
                        List<string>? missingNumbers = null;
                        
                        var importTask = Task.Run(async () =>
                        {
                            try
                            {
                                // Validate that all numbers exist in external sim_data before any import.
                                var validationResult = await _simDataValidationService.ValidateNumbersExistAsync(
                                    _phoneNumbers,
                                    (current, total, currentNumber) =>
                                    {
                                        progressWindow.Dispatcher.Invoke(() =>
                                        {
                                            progressWindow.UpdateProgress(current, total, currentNumber, "Проверка номера");
                                        });
                                    });

                                if (!validationResult.IsValid)
                                {
                                    missingNumbers = validationResult.MissingNumbers;
                                    _logger.LogInfo($"Import validation failed. Missing numbers count: {missingNumbers.Count}");
                                    return;
                                }

                                // Import phone numbers to database with default states
                                importedCount = await _dataService.ImportPhoneNumbersAsync(_phoneNumbers, isPriorityImport, addToExisting,
                                    (current, total, currentNumber) =>
                                    {
                                        // Update progress on UI thread
                                        progressWindow.Dispatcher.Invoke(() =>
                                        {
                                            progressWindow.UpdateProgress(current, total, currentNumber, "Импорт номера");
                                        });
                                    });
                                _logger.LogInfo($"Successfully imported {importedCount} out of {_phoneNumbers.Count} phone numbers");
                            }
                            catch (Exception ex)
                            {
                                importException = ex;
                                _logger.LogError("Error during phone numbers import", ex);
                            }
                            finally
                            {
                                // Close progress window when done
                                progressWindow.Dispatcher.Invoke(() => progressWindow.AllowClose());
                            }
                        });
                        
                        // Start import task
                        _ = importTask;
                        
                        // Show modal dialog (blocks until closed, which happens when import completes)
                        progressWindow.ShowDialog();
                        
                        // Wait for import to complete (should already be done, but ensure)
                        await importTask;

                        // Always refresh data on UI thread after import completes.
                        // This prevents stale list issues when import runs on background thread.
                        TotalNumbers = await _dataService.GetPhoneOffersCountAsync();
                        if (_phoneOffersViewModel != null)
                        {
                            _phoneOffersViewModel.CurrentPage = 1;
                            await _phoneOffersViewModel.LoadDataAsync();
                        }
                        
                        // Show result message
                        if (importException != null)
                        {
                            System.Windows.MessageBox.Show(
                                $"Ошибка при импорте файла: {importException.Message}",
                                "Ошибка",
                                System.Windows.MessageBoxButton.OK,
                                System.Windows.MessageBoxImage.Error);
                        }
                        else if (missingNumbers != null && missingNumbers.Count > 0)
                        {
                            var missingNumbersWindow = new MissingNumbersWindow(missingNumbers)
                            {
                                Owner = System.Windows.Application.Current.MainWindow
                            };
                            missingNumbersWindow.ShowDialog();
                        }
                        else
                        {
                            System.Windows.MessageBox.Show(
                                $"Импортировано {importedCount} из {_phoneNumbers.Count} номеров телефонов в базу данных",
                                "Импорт завершен",
                                System.Windows.MessageBoxButton.OK,
                                System.Windows.MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error during phone numbers import", ex);
                        System.Windows.MessageBox.Show(
                            $"Ошибка при импорте файла: {ex.Message}",
                            "Ошибка",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error opening file dialog for import", ex);
                System.Windows.MessageBox.Show(
                    $"Ошибка при открытии диалога выбора файла: {ex.Message}",
                    "Ошибка",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        public async Task InitializeWebViewAsync()
        {
            try
            {
                _logger.LogInfo("Initializing WebView...");
                await _webViewService.InitializeAsync();
                await _webViewService.NavigateAsync("https://partner.vodafone.ua/dashboard/personal-offers-main/personal-offers");
                // Check authentication after navigation
                await CheckAuthenticationStatusAsync();
                _logger.LogInfo("WebView initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error initializing WebView", ex);
                throw;
            }
        }

        public async Task CheckAuthenticationStatusAsync()
        {
            try
            {
                bool authenticated = await _webViewService.CheckAuthenticationAsync();
                IsAuthenticated = authenticated;
                _logger.LogInfo($"Authentication status checked: {(authenticated ? "Authenticated" : "Not authenticated")}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error checking authentication status", ex);
                IsAuthenticated = false;
            }
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
            try
            {
                _logger.LogInfo("Starting phone scan...");

                _cancellationTokenSource = new CancellationTokenSource();
                IsScanning = true;

                ProcessedCount = 0;
                OffersFoundCount = 0;
                ServerErrors = 0;

                try
                {
                    int roundNumber = 0;

                    while (true)
                    {
                        roundNumber++;
                        _cancellationTokenSource.Token.ThrowIfCancellationRequested();

                        var configuration = _configViewModel.GetConfiguration();
                        bool mightHaveWork = await MightHaveWorkAsync(configuration);

                        if (roundNumber == 1 && !mightHaveWork && !configuration.Is24x7Mode)
                        {
                            _logger.LogInfo("No unprocessed phone offers found");
                            System.Windows.MessageBox.Show(
                                "Нет необработанных номеров для сканирования",
                                "Нет данных",
                                System.Windows.MessageBoxButton.OK,
                                System.Windows.MessageBoxImage.Information);
                            break;
                        }

                        await RunScanRoundAsync(configuration, roundNumber);

                        if (_phoneOffersViewModel != null)
                        {
                            await _phoneOffersViewModel.LoadDataAsync();
                        }

                        if (!configuration.Is24x7Mode)
                        {
                            _logger.LogInfo("Phone scan completed successfully");
                            IterationInfo = string.Empty;
                            System.Windows.MessageBox.Show(
                                "Обработка завершена",
                                "^_^",
                                System.Windows.MessageBoxButton.OK,
                                System.Windows.MessageBoxImage.Information);
                            break;
                        }

                        await WaitUntilNextDailyRoundStartAsync();
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInfo("Phone scan cancelled by user");
                    // Reset progress bars
                    ProgressInput = 0;
                    ProgressSearch = 0;
                    ProgressNext = 0;
                    IterationInfo = string.Empty;

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
                catch (Exception ex)
                {
                    _logger.LogError("Error during phone scan", ex);
                    System.Windows.MessageBox.Show(
                        $"Ошибка во время сканирования: {ex.Message}",
                        "Ошибка",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                }
                finally
                {
                    // Ensure progress bars are reset
                    ProgressInput = 0;
                    ProgressSearch = 0;
                    ProgressNext = 0;
                    IterationInfo = string.Empty;
                    CurrentIteration = 0;
                    TotalIterations = 0;
                    
                    IsScanning = false;
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error starting phone scan", ex);
                IsScanning = false;
                System.Windows.MessageBox.Show(
                    $"Ошибка при запуске сканирования: {ex.Message}",
                    "Ошибка",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private async Task<bool> MightHaveWorkAsync(ProcessingConfiguration configuration)
        {
            var unprocessedOffers = await _dataService.GetUnprocessedPhoneOffersAsync();
            return unprocessedOffers.Count > 0
                || configuration.EmptyPropositionsRepeats > 0
                || configuration.ErrorNumbersRepeats > 0;
        }

        private async Task<bool> RunScanRoundAsync(ProcessingConfiguration configuration, int roundNumber)
        {
            bool hadWork = false;
            int totalIterations = 1 + configuration.EmptyPropositionsRepeats + configuration.ErrorNumbersRepeats;
            TotalIterations = totalIterations;
            string roundPrefix = configuration.Is24x7Mode ? $"Раунд {roundNumber}: " : string.Empty;

            CurrentIteration = 1;
            var unprocessedOffers = await _dataService.GetUnprocessedPhoneOffersAsync();

            if (unprocessedOffers.Count > 0)
            {
                hadWork = true;
                TotalNumbers = unprocessedOffers.Count;
                IterationInfo = $"{roundPrefix}Итерация {CurrentIteration} из {TotalIterations}: Обработка всех номеров";

                var firstReport = await ProcessPhoneOffersListAsync(
                    unprocessedOffers,
                    configuration,
                    BuildRoundIterationLabel(configuration, roundNumber, "Обработка всех номеров"));
                await AddIterationReportAsync(firstReport);
            }

            if (configuration.EmptyPropositionsRepeats > 0)
            {
                for (int i = 1; i <= configuration.EmptyPropositionsRepeats; i++)
                {
                    _cancellationTokenSource!.Token.ThrowIfCancellationRequested();
                    CurrentIteration = 1 + i;
                    IterationInfo = $"{roundPrefix}Итерация {CurrentIteration} из {TotalIterations}: Обработка пустых предложений (цикл {i}/{configuration.EmptyPropositionsRepeats})";

                    var emptyPropositions = await _dataService.GetEmptyPropositionsAsync();
                    if (emptyPropositions.Count == 0)
                    {
                        _logger.LogInfo($"No empty propositions found for iteration {i}");
                        break;
                    }

                    hadWork = true;
                    TotalNumbers = emptyPropositions.Count;
                    var emptyReport = await ProcessPhoneOffersListAsync(
                        emptyPropositions,
                        configuration,
                        BuildRoundIterationLabel(
                            configuration,
                            roundNumber,
                            $"Пустые предложения (цикл {i}/{configuration.EmptyPropositionsRepeats})"),
                        excludeOnSuccess: true);
                    await AddIterationReportAsync(emptyReport);
                }
            }

            if (configuration.ErrorNumbersRepeats > 0)
            {
                for (int i = 1; i <= configuration.ErrorNumbersRepeats; i++)
                {
                    _cancellationTokenSource!.Token.ThrowIfCancellationRequested();
                    CurrentIteration = 1 + configuration.EmptyPropositionsRepeats + i;
                    IterationInfo = $"{roundPrefix}Итерация {CurrentIteration} из {TotalIterations}: Обработка номеров с ошибками (цикл {i}/{configuration.ErrorNumbersRepeats})";

                    var errorNumbers = await _dataService.GetErrorNumbersAsync();
                    if (errorNumbers.Count == 0)
                    {
                        _logger.LogInfo($"No error numbers found for iteration {i}");
                        break;
                    }

                    hadWork = true;
                    TotalNumbers = errorNumbers.Count;
                    var errorReport = await ProcessPhoneOffersListAsync(
                        errorNumbers,
                        configuration,
                        BuildRoundIterationLabel(
                            configuration,
                            roundNumber,
                            $"Номера с ошибками (цикл {i}/{configuration.ErrorNumbersRepeats})"),
                        excludeOnSuccess: true);
                    await AddIterationReportAsync(errorReport);
                }
            }

            return hadWork;
        }

        private static string BuildRoundIterationLabel(ProcessingConfiguration configuration, int roundNumber, string label)
        {
            return configuration.Is24x7Mode ? $"Раунд {roundNumber}: {label}" : label;
        }

        private const int DailyRoundStartHour = 4;

        private async Task WaitUntilNextDailyRoundStartAsync()
        {
            while (true)
            {
                _cancellationTokenSource!.Token.ThrowIfCancellationRequested();

                var now = DateTime.Now;
                var nextStart = GetNextDailyRoundStartTime(now);
                var remaining = nextStart - now;

                if (remaining <= TimeSpan.Zero)
                {
                    break;
                }

                IterationInfo = $"Раунд завершён. Следующий раунд: {nextStart:dd.MM.yyyy HH:mm} (осталось {FormatTimeRemaining(remaining)})";
                var delayMs = (int)Math.Min(1000, remaining.TotalMilliseconds);
                await Task.Delay(delayMs, _cancellationTokenSource.Token);
            }
        }

        private static DateTime GetNextDailyRoundStartTime(DateTime now)
        {
            var todayAtStart = new DateTime(now.Year, now.Month, now.Day, DailyRoundStartHour, 0, 0, DateTimeKind.Local);
            return now < todayAtStart ? todayAtStart : todayAtStart.AddDays(1);
        }

        private static string FormatTimeRemaining(TimeSpan remaining)
        {
            if (remaining.TotalHours >= 1)
            {
                return $"{(int)remaining.TotalHours} ч {remaining.Minutes} мин";
            }

            if (remaining.TotalMinutes >= 1)
            {
                return $"{remaining.Minutes} мин {remaining.Seconds} сек";
            }

            return $"{remaining.Seconds} сек";
        }

        private async Task StopScanAsync()
        {
            try
            {
                _logger.LogInfo("Stopping phone scan...");
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
            catch (Exception ex)
            {
                _logger.LogError("Error stopping phone scan", ex);
            }
        }

        private async void OpenConfig()
        {
            try
            {
                await _configViewModel.RefreshPhoneNumbersCountAsync(_dataService);
                _configViewModel.BeginEditing();

                var configWindow = new ConfigWindow(_configViewModel);
                configWindow.Owner = System.Windows.Application.Current.MainWindow;

                if (configWindow.ShowDialog() == true)
                {
                    _configViewModel.CommitEditing();
                }
                else
                {
                    _configViewModel.CancelEditing();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error opening config window", ex);
                System.Windows.MessageBox.Show(
                    $"Ошибка при открытии окна настроек: {ex.Message}",
                    "Ошибка",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        public async Task ResetScannedAsync()
        {
            try
            {
                var result = System.Windows.MessageBox.Show(
                    "Сбросить статус обработки для всех записей?",
                    "Подтверждение",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    _logger.LogInfo("Resetting scanned status...");
                    await _dataService.ResetScannedAsync();
                    
                    // Refresh PhoneOffersViewModel
                    if (_phoneOffersViewModel != null)
                    {
                        await _phoneOffersViewModel.LoadDataAsync();
                    }

                    _logger.LogInfo("Scanned status reset successfully");
                    System.Windows.MessageBox.Show(
                        "Статус обработки сброшен для всех записей",
                        "Готово",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error resetting scanned status", ex);
                System.Windows.MessageBox.Show(
                    $"Ошибка при сбросе статуса обработки: {ex.Message}",
                    "Ошибка",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        public async Task ResetAllAsync()
        {
            try
            {
                var result = System.Windows.MessageBox.Show(
                    "Сбросить все записи к состоянию по умолчанию? Это действие нельзя отменить.",
                    "Подтверждение",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    _logger.LogInfo("Resetting all data...");
                    await _dataService.ResetAllAsync();
                    
                    // Refresh PhoneOffersViewModel
                    if (_phoneOffersViewModel != null)
                    {
                        await _phoneOffersViewModel.LoadDataAsync();
                    }

                    TotalNumbers = 0;
                    ProcessedCount = 0;
                    OffersFoundCount = 0;
                    ServerErrors = 0;

                    _logger.LogInfo("All data reset successfully");
                    System.Windows.MessageBox.Show(
                        "Все записи сброшены к состоянию по умолчанию",
                        "Готово",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error resetting all data", ex);
                System.Windows.MessageBox.Show(
                    $"Ошибка при сбросе всех данных: {ex.Message}",
                    "Ошибка",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private async Task ClearIterationHistoryAsync()
        {
            try
            {
                var result = System.Windows.MessageBox.Show(
                    "Очистить всю историю отчетов по итерациям?",
                    "Подтверждение",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result != System.Windows.MessageBoxResult.Yes)
                {
                    return;
                }

                await _dataService.ClearIterationReportsAsync();
                IterationReports.Clear();
                SelectedIterationReport = null;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error clearing iteration history", ex);
                System.Windows.MessageBox.Show(
                    $"Ошибка при очистке истории итераций: {ex.Message}",
                    "Ошибка",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
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

        private async Task<IterationReport> ProcessPhoneOffersListAsync(
            List<PhoneOffer> offers,
            ProcessingConfiguration configuration,
            string iterationLabel,
            bool excludeOnSuccess = false)
        {
            var report = new IterationReport
            {
                IterationNumber = CurrentIteration,
                IterationLabel = iterationLabel,
                StartedAt = DateTime.Now,
                PlannedCount = offers.Count
            };

            foreach (var phoneOffer in offers.OrderByDescending(o => o.IsPriority).ThenBy(o => o.Id))
            {
                // Check for cancellation
                _cancellationTokenSource!.Token.ThrowIfCancellationRequested();

                // Get current configuration (in case user changed it in config window)
                var currentConfig = _configViewModel.GetConfiguration();

                await _phoneSearchService.ProcessPhoneOfferAsync(phoneOffer, currentConfig, this, _cancellationTokenSource.Token);

                var updatedOffer = await _dataService.GetPhoneOfferByIdAsync(phoneOffer.Id);
                var itemReport = BuildIterationItemReport(phoneOffer.PhoneNumber, updatedOffer);
                report.Items.Add(itemReport);
                report.ProcessedCount++;

                switch (itemReport.Outcome)
                {
                    case "Найдено":
                        report.FoundCount++;
                        if (itemReport.DiscountPercent > 0)
                        {
                            report.PropositionsFoundCount++;
                        }
                        if (itemReport.GiftAmount > 0)
                        {
                            report.GiftsFoundCount++;
                        }
                        break;
                    case "Ошибка":
                        report.ErrorCount++;
                        break;
                    case "Нет предложений":
                        report.NotFoundCount++;
                        break;
                    case "Не подходящие":
                        report.NotSuitableCount++;
                        break;
                }
                
                // Note: If excludeOnSuccess is true, numbers that get propositions will be automatically
                // excluded from next iterations because GetEmptyPropositionsAsync and GetErrorNumbersAsync
                // filter by DiscountPercent == 0 && GiftAmount == 0 (for empty) or IsError == true (for errors)
            }

            report.CompletedAt = DateTime.Now;
            return report;
        }

        private async Task AddIterationReportAsync(IterationReport report)
        {
            var savedReport = await _dataService.SaveIterationReportAsync(report);
            IterationReports.Insert(0, savedReport);
            SelectedIterationReport = savedReport;
        }

        private static IterationItemReport BuildIterationItemReport(string fallbackPhoneNumber, PhoneOffer? offer)
        {
            if (offer == null)
            {
                return new IterationItemReport
                {
                    PhoneNumber = fallbackPhoneNumber,
                    Outcome = "Ошибка",
                    IsError = true,
                    ErrorDescription = "Не удалось загрузить запись после обработки"
                };
            }

            bool hasValidOffer = OfferClassification.HasDiscountOrGift(offer);

            string outcome;
            if (offer.IsError)
            {
                outcome = "Ошибка";
            }
            else if (hasValidOffer)
            {
                outcome = "Найдено";
            }
            else if (offer.IsPropositionsNotFound)
            {
                outcome = "Нет предложений";
            }
            else
            {
                outcome = "Не подходящие";
            }

            return new IterationItemReport
            {
                PhoneNumber = offer.PhoneNumber,
                Outcome = outcome,
                DiscountPercent = offer.DiscountPercent,
                GiftAmount = offer.GiftAmount,
                MinTopupAmount = offer.MinTopupAmount,
                IsError = offer.IsError,
                ErrorDescription = offer.ErrorDescription
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void IterationReports_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            ((RelayCommand)ClearIterationHistoryCommand).RaiseCanExecuteChanged();
        }
    }
}

