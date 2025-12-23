using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VodafoneLogin.Models;
using VodafoneLogin.Services;

namespace VodafoneLogin.ViewModels
{
    public class PhoneOffersViewModel : INotifyPropertyChanged
    {
        private readonly IDataService _dataService;
        private ObservableCollection<PhoneOffer> _phoneOffers = new();
        private string _phoneFilter = string.Empty;
        private bool? _hasDiscount;
        private bool? _hasGift;
        private int _currentPage = 1;
        private int _pageSize = 50;
        private int _totalCount;
        private int _totalPages;

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
                CurrentPage = 1; 
                await LoadDataAsync(); 
            });
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
                _phoneFilter = value;
                OnPropertyChanged();
            }
        }

        public bool? HasDiscount
        {
            get => _hasDiscount;
            set
            {
                _hasDiscount = value;
                OnPropertyChanged();
            }
        }

        public bool? HasGift
        {
            get => _hasGift;
            set
            {
                _hasGift = value;
                OnPropertyChanged();
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

        public ICommand LoadDataCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand FirstPageCommand { get; }
        public ICommand LastPageCommand { get; }
        public ICommand ApplyFilterCommand { get; }
        public ICommand ClearFilterCommand { get; }

        public async Task LoadDataAsync()
        {
            TotalCount = await _dataService.GetPhoneOffersCountAsync(PhoneFilter, HasDiscount, HasGift);
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);

            if (CurrentPage > TotalPages && TotalPages > 0)
                CurrentPage = TotalPages;

            var skip = (CurrentPage - 1) * PageSize;
            var offers = await _dataService.GetPhoneOffersAsync(skip, PageSize, PhoneFilter, HasDiscount, HasGift);
            
            PhoneOffers.Clear();
            foreach (var offer in offers)
            {
                PhoneOffers.Add(offer);
            }

            // Update command states
            ((RelayCommand)NextPageCommand).RaiseCanExecuteChanged();
            ((RelayCommand)PreviousPageCommand).RaiseCanExecuteChanged();
            ((RelayCommand)FirstPageCommand).RaiseCanExecuteChanged();
            ((RelayCommand)LastPageCommand).RaiseCanExecuteChanged();
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

