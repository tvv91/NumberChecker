using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using OfficeOpenXml;
using VodafoneNumberChecker.Models;
using VodafoneNumberChecker.Services;

namespace VodafoneNumberChecker.ViewModels
{
    public class PhoneOffersViewModel : INotifyPropertyChanged
    {
        private readonly IDataService _dataService;
        private ObservableCollection<PhoneOffer> _phoneOffers = new();
        private string _phoneFilter = string.Empty;
        private bool? _hasDiscount;
        private bool? _hasGift;
        private bool? _isEmptyProposition;
        private bool? _hasError;
        private bool? _isPropositionsNotFound;
        private bool? _isPropositionsNotSuitable;
        private int _currentPage = 1;
        private int _pageSize = 50;
        private int _totalCount;
        private int _totalPages;
        private bool _isRealtime;
        private bool _useColors;
        private bool _showAllFields;
        private DispatcherTimer? _realtimeTimer;
        private DispatcherTimer? _filterDebounceTimer;
        private int _discountCount;
        private int _giftCount;
        private int _errorCount;
        private int _notFoundCount;
        private int _notSuitableCount;

        public PhoneOffersViewModel(IDataService dataService)
        {
            _dataService = dataService;
            LoadDataCommand = new RelayCommand(async () => await LoadDataAsync());
            NextPageCommand = new RelayCommand(async () => { CurrentPage++; await LoadDataAsync(); }, () => CurrentPage < TotalPages);
            PreviousPageCommand = new RelayCommand(async () => { CurrentPage--; await LoadDataAsync(); }, () => CurrentPage > 1);
            FirstPageCommand = new RelayCommand(async () => { CurrentPage = 1; await LoadDataAsync(); }, () => CurrentPage > 1);
            LastPageCommand = new RelayCommand(async () => { CurrentPage = TotalPages; await LoadDataAsync(); }, () => CurrentPage < TotalPages);
            ApplyFilterCommand = new RelayCommand(async () => { CurrentPage = 1; await LoadDataAsync(); });
            ClearFilterCommand = new RelayCommand(async () => 
            { 
                PhoneFilter = string.Empty; 
                HasDiscount = null; 
                HasGift = null; 
                HasError = null; 
                IsPropositionsNotFound = null; 
                IsPropositionsNotSuitable = null; 
                CurrentPage = 1; 
                await LoadDataAsync(); 
            });
            ExportToExcelCommand = new RelayCommand(async () => await ExportToExcelAsync(), () => TotalCount > 0);
        }

        public ObservableCollection<PhoneOffer> PhoneOffers
        {
            get => _phoneOffers;
            set
            {
                _phoneOffers = value;
                OnPropertyChanged();
            }
        }

        public string PhoneFilter
        {
            get => _phoneFilter;
            set
            {
                if (_phoneFilter != value)
                {
                    _phoneFilter = value;
                    OnPropertyChanged();
                    // Auto-apply filter only if real-time is enabled
                    if (_isRealtime)
                    {
                        DebounceFilterApply();
                    }
                }
            }
        }

        public bool? HasDiscount
        {
            get => _hasDiscount;
            set
            {
                if (_hasDiscount != value)
                {
                    _hasDiscount = value;
                    OnPropertyChanged();
                    // Auto-apply filter only if real-time is enabled
                    if (_isRealtime)
                    {
                        DebounceFilterApply();
                    }
                }
            }
        }

        public bool? HasGift
        {
            get => _hasGift;
            set
            {
                if (_hasGift != value)
                {
                    _hasGift = value;
                    OnPropertyChanged();
                    // Auto-apply filter only if real-time is enabled
                    if (_isRealtime)
                    {
                        DebounceFilterApply();
                    }
                }
            }
        }

        public bool? IsEmptyProposition
        {
            get => _isEmptyProposition;
            set
            {
                _isEmptyProposition = value;
                OnPropertyChanged();
            }
        }

        public bool? HasError
        {
            get => _hasError;
            set
            {
                if (_hasError != value)
                {
                    _hasError = value;
                    OnPropertyChanged();
                    // Auto-apply filter only if real-time is enabled
                    if (_isRealtime)
                    {
                        DebounceFilterApply();
                    }
                }
            }
        }

        public bool? IsPropositionsNotFound
        {
            get => _isPropositionsNotFound;
            set
            {
                if (_isPropositionsNotFound != value)
                {
                    _isPropositionsNotFound = value;
                    OnPropertyChanged();
                    // Auto-apply filter only if real-time is enabled
                    if (_isRealtime)
                    {
                        DebounceFilterApply();
                    }
                }
            }
        }

        public bool? IsPropositionsNotSuitable
        {
            get => _isPropositionsNotSuitable;
            set
            {
                if (_isPropositionsNotSuitable != value)
                {
                    _isPropositionsNotSuitable = value;
                    OnPropertyChanged();
                    // Auto-apply filter only if real-time is enabled
                    if (_isRealtime)
                    {
                        DebounceFilterApply();
                    }
                }
            }
        }

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PageInfo));
            }
        }

        public int PageSize
        {
            get => _pageSize;
            set
            {
                _pageSize = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PageInfo));
            }
        }

        public int TotalCount
        {
            get => _totalCount;
            set
            {
                _totalCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PageInfo));
                // Update export command state
                ((RelayCommand)ExportToExcelCommand).RaiseCanExecuteChanged();
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            set
            {
                _totalPages = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PageInfo));
            }
        }

        public string PageInfo => $"Страница {CurrentPage} из {TotalPages} (Всего: {TotalCount})";

        public bool IsRealtime
        {
            get => _isRealtime;
            set
            {
                if (_isRealtime != value)
                {
                    _isRealtime = value;
                    OnPropertyChanged();
                    if (value)
                    {
                        StartRealtimeUpdates();
                    }
                    else
                    {
                        StopRealtimeUpdates();
                    }
                }
            }
        }

        public bool UseColors
        {
            get => _useColors;
            set
            {
                _useColors = value;
                OnPropertyChanged();
            }
        }

        public bool ShowAllFields
        {
            get => _showAllFields;
            set
            {
                _showAllFields = value;
                OnPropertyChanged();
            }
        }

        public int DiscountCount
        {
            get => _discountCount;
            set
            {
                _discountCount = value;
                OnPropertyChanged();
            }
        }

        public int GiftCount
        {
            get => _giftCount;
            set
            {
                _giftCount = value;
                OnPropertyChanged();
            }
        }

        public int ErrorCount
        {
            get => _errorCount;
            set
            {
                _errorCount = value;
                OnPropertyChanged();
            }
        }

        public int NotFoundCount
        {
            get => _notFoundCount;
            set
            {
                _notFoundCount = value;
                OnPropertyChanged();
            }
        }

        public int NotSuitableCount
        {
            get => _notSuitableCount;
            set
            {
                _notSuitableCount = value;
                OnPropertyChanged();
            }
        }

        public ICommand LoadDataCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand FirstPageCommand { get; }
        public ICommand LastPageCommand { get; }
        public ICommand ApplyFilterCommand { get; }
        public ICommand ClearFilterCommand { get; }
        public ICommand ExportToExcelCommand { get; }

        public async Task LoadDataAsync()
        {
            TotalCount = await _dataService.GetPhoneOffersCountAsync(PhoneFilter, HasDiscount, HasGift, null, HasError, IsPropositionsNotFound, IsPropositionsNotSuitable);
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);

            if (CurrentPage > TotalPages && TotalPages > 0)
                CurrentPage = TotalPages;

            var skip = (CurrentPage - 1) * PageSize;
            var offers = await _dataService.GetPhoneOffersAsync(skip, PageSize, PhoneFilter, HasDiscount, HasGift, null, HasError, IsPropositionsNotFound, IsPropositionsNotSuitable);
            
            PhoneOffers.Clear();
            foreach (var offer in offers)
            {
                PhoneOffers.Add(offer);
            }

            // Update category counts (respecting phone filter only)
            DiscountCount = await _dataService.GetDiscountCountAsync(PhoneFilter);
            GiftCount = await _dataService.GetGiftCountAsync(PhoneFilter);
            ErrorCount = await _dataService.GetErrorCountAsync(PhoneFilter);
            NotFoundCount = await _dataService.GetNotFoundCountAsync(PhoneFilter);
            NotSuitableCount = await _dataService.GetNotSuitableCountAsync(PhoneFilter);

            // Update command states
            ((RelayCommand)NextPageCommand).RaiseCanExecuteChanged();
            ((RelayCommand)PreviousPageCommand).RaiseCanExecuteChanged();
            ((RelayCommand)FirstPageCommand).RaiseCanExecuteChanged();
            ((RelayCommand)LastPageCommand).RaiseCanExecuteChanged();
        }

        private void StartRealtimeUpdates()
        {
            if (_realtimeTimer == null)
            {
                _realtimeTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(2) // Update every 2 seconds
                };
                _realtimeTimer.Tick += async (s, e) => await LoadDataAsync();
            }
            _realtimeTimer.Start();
        }

        private void StopRealtimeUpdates()
        {
            if (_realtimeTimer != null)
            {
                _realtimeTimer.Stop();
                _realtimeTimer = null;
            }
        }

        private void DebounceFilterApply()
        {
            // Stop existing timer if any
            if (_filterDebounceTimer != null)
            {
                _filterDebounceTimer.Stop();
                _filterDebounceTimer = null;
            }

            // Create new timer to apply filter after a short delay
            _filterDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300) // 300ms debounce
            };
            _filterDebounceTimer.Tick += async (s, e) =>
            {
                _filterDebounceTimer.Stop();
                _filterDebounceTimer = null;
                CurrentPage = 1;
                await LoadDataAsync();
            };
            _filterDebounceTimer.Start();
        }

        public async Task ExportToExcelAsync()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                    FileName = $"PhoneOffers_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                    DefaultExt = "xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Get all filtered data (without pagination)
                    var allOffers = await _dataService.GetAllPhoneOffersForExportAsync(
                        PhoneFilter, HasDiscount, HasGift, null, HasError, IsPropositionsNotFound, IsPropositionsNotSuitable);

                    // Set license context for EPPlus (non-commercial use)
                    ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                    using var package = new ExcelPackage();
                    var worksheet = package.Workbook.Worksheets.Add("Phone Offers");

                    // Headers
                    worksheet.Cells[1, 1].Value = "ID";
                    worksheet.Cells[1, 2].Value = "Номер телефона";
                    worksheet.Cells[1, 3].Value = "Скидка %";
                    worksheet.Cells[1, 4].Value = "Мин. пополнение";
                    worksheet.Cells[1, 5].Value = "Подарок";
                    worksheet.Cells[1, 6].Value = "Дней действия";
                    worksheet.Cells[1, 7].Value = "Действует до";
                    worksheet.Cells[1, 8].Value = "Создано";
                    worksheet.Cells[1, 9].Value = "Обновлено";
                    worksheet.Cells[1, 10].Value = "Итераций";
                    worksheet.Cells[1, 11].Value = "IsProcessed";
                    worksheet.Cells[1, 12].Value = "Ошибка";
                    worksheet.Cells[1, 13].Value = "Описание ошибки";
                    worksheet.Cells[1, 14].Value = "Нет предложений";
                    worksheet.Cells[1, 15].Value = "Нет подходящих";

                    // Style header row
                    using (var range = worksheet.Cells[1, 1, 1, 15])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    }

                    // Data rows
                    for (int i = 0; i < allOffers.Count; i++)
                    {
                        var offer = allOffers[i];
                        int row = i + 2;
                        worksheet.Cells[row, 1].Value = offer.Id;
                        worksheet.Cells[row, 2].Value = offer.PhoneNumber;
                        worksheet.Cells[row, 3].Value = offer.DiscountPercent;
                        worksheet.Cells[row, 4].Value = offer.MinTopupAmount;
                        worksheet.Cells[row, 5].Value = offer.GiftAmount;
                        worksheet.Cells[row, 6].Value = offer.ActiveDays;
                        worksheet.Cells[row, 7].Value = offer.ValidUntil?.ToString("yyyy-MM-dd");
                        worksheet.Cells[row, 8].Value = offer.CreatedAt.ToString("yyyy-MM-dd HH:mm");
                        worksheet.Cells[row, 9].Value = offer.UpdatedAt?.ToString("yyyy-MM-dd HH:mm");
                        worksheet.Cells[row, 10].Value = offer.IterationCount;
                        worksheet.Cells[row, 11].Value = offer.IsProcessed;
                        worksheet.Cells[row, 12].Value = offer.IsError;
                        worksheet.Cells[row, 13].Value = offer.ErrorDescription;
                        worksheet.Cells[row, 14].Value = offer.IsPropositionsNotFound;
                        worksheet.Cells[row, 15].Value = offer.IsPropositionsNotSuitable;
                    }

                    // Auto-fit columns
                    worksheet.Cells.AutoFitColumns();

                    // Save file
                    var fileInfo = new FileInfo(saveFileDialog.FileName);
                    await package.SaveAsAsync(fileInfo);

                    System.Windows.MessageBox.Show($"Экспортировано {allOffers.Count} записей в файл:\n{saveFileDialog.FileName}", 
                        "Экспорт завершен", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка при экспорте: {ex.Message}", 
                    "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        // Cleanup method to be called when view model is no longer needed
        public void Dispose()
        {
            StopRealtimeUpdates();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Func<bool>? _canExecute;
        private readonly Action _execute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

