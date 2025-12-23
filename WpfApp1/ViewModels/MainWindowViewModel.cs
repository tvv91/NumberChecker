using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VodafoneLogin.Models;
using VodafoneLogin.Services;

namespace VodafoneLogin.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged, IProgressReporter
    {
        private readonly IFileService _fileService;
        private readonly IWebViewService _webViewService;
        private readonly IPhoneSearchService _phoneSearchService;

        private const string _phoneNumbersFile = "numbers.txt";
        private const string _progressFile = "progress.txt";

        private int _totalNumbers;
        private int _processedCount;
        private int _offersFoundCount;
        private int _serverErrors;
        private string _currentNumber = "-";
        private double _delayInputMin = 3;
        private double _delayInputMax = 5;
        private double _delaySearchMin = 3;
        private double _delaySearchMax = 5;
        private double _delayNextMin = 3;
        private double _delayNextMax = 5;
        private double _progressInput;
        private double _progressSearch;
        private double _progressNext;

        public MainWindowViewModel(IFileService fileService, IWebViewService webViewService, IPhoneSearchService phoneSearchService)
        {
            _fileService = fileService;
            _webViewService = webViewService;
            _phoneSearchService = phoneSearchService;

            LoadPhoneNumbers();
        }

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

        public double DelayInputMin
        {
            get => _delayInputMin;
            set
            {
                _delayInputMin = value;
                OnPropertyChanged();
            }
        }

        public double DelayInputMax
        {
            get => _delayInputMax;
            set
            {
                _delayInputMax = value;
                OnPropertyChanged();
            }
        }

        public double DelaySearchMin
        {
            get => _delaySearchMin;
            set
            {
                _delaySearchMin = value;
                OnPropertyChanged();
            }
        }

        public double DelaySearchMax
        {
            get => _delaySearchMax;
            set
            {
                _delaySearchMax = value;
                OnPropertyChanged();
            }
        }

        public double DelayNextMin
        {
            get => _delayNextMin;
            set
            {
                _delayNextMin = value;
                OnPropertyChanged();
            }
        }

        public double DelayNextMax
        {
            get => _delayNextMax;
            set
            {
                _delayNextMax = value;
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

        private void LoadPhoneNumbers()
        {
            var phoneNumbers = _fileService.ReadPhoneNumbers(_phoneNumbersFile);
            TotalNumbers = phoneNumbers.Count;
        }

        public async Task InitializeWebViewAsync()
        {
            await _webViewService.InitializeAsync();
            await _webViewService.NavigateAsync("https://partner.vodafone.ua/dashboard/personal-offers-main/personal-offers");
        }

        public async Task StartPhoneSearchAsync()
        {
            var phoneNumbers = _fileService.ReadPhoneNumbers(_phoneNumbersFile);
            if (phoneNumbers.Count == 0)
            {
                System.Windows.MessageBox.Show($"Файл {_phoneNumbersFile} не найден или пуст!", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            TotalNumbers = phoneNumbers.Count;
            ProcessedCount = 0;
            OffersFoundCount = 0;
            ServerErrors = 0;

            int lastProcessedIndex = _fileService.ReadProgress(_progressFile);

            var configuration = new ProcessingConfiguration
            {
                DelayInputMin = DelayInputMin,
                DelayInputMax = DelayInputMax,
                DelaySearchMin = DelaySearchMin,
                DelaySearchMax = DelaySearchMax,
                DelayNextMin = DelayNextMin,
                DelayNextMax = DelayNextMax
            };

            for (int i = lastProcessedIndex + 1; i < phoneNumbers.Count; i++)
            {
                var number = phoneNumbers[i];
                
                // Update configuration from current slider values (in case user changed them)
                configuration.DelayInputMin = DelayInputMin;
                configuration.DelayInputMax = DelayInputMax;
                configuration.DelaySearchMin = DelaySearchMin;
                configuration.DelaySearchMax = DelaySearchMax;
                configuration.DelayNextMin = DelayNextMin;
                configuration.DelayNextMax = DelayNextMax;

                await _phoneSearchService.ProcessPhoneNumberAsync(number, i, configuration, this);
            }

            System.Windows.MessageBox.Show("Обработка завершена", "^_^", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
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

