using System.ComponentModel;
using System.Runtime.CompilerServices;
using VodafoneNumberChecker.Models;
using VodafoneNumberChecker.Services;

namespace VodafoneNumberChecker.ViewModels
{
    public class ConfigViewModel : INotifyPropertyChanged
    {
        private double _delayInputMin = 3;
        private double _delayInputMax = 5;
        private double _delaySearchMin = 3;
        private double _delaySearchMax = 5;
        private double _delayNextMin = 3;
        private double _delayNextMax = 5;
        private int _emptyPropositionsRepeats = 0;
        private int _errorNumbersRepeats = 0;
        private bool _is24x7Mode;
        private bool _shouldTopUpNumbers;
        private int _phoneNumbersCount;

        public double DelayInputMin
        {
            get => _delayInputMin;
            set
            {
                _delayInputMin = value;
                OnPropertyChanged();
                NotifyTimeEstimateChanged();
            }
        }

        public double DelayInputMax
        {
            get => _delayInputMax;
            set
            {
                _delayInputMax = value;
                OnPropertyChanged();
                NotifyTimeEstimateChanged();
            }
        }

        public double DelaySearchMin
        {
            get => _delaySearchMin;
            set
            {
                _delaySearchMin = value;
                OnPropertyChanged();
                NotifyTimeEstimateChanged();
            }
        }

        public double DelaySearchMax
        {
            get => _delaySearchMax;
            set
            {
                _delaySearchMax = value;
                OnPropertyChanged();
                NotifyTimeEstimateChanged();
            }
        }

        public double DelayNextMin
        {
            get => _delayNextMin;
            set
            {
                _delayNextMin = value;
                OnPropertyChanged();
                NotifyTimeEstimateChanged();
            }
        }

        public double DelayNextMax
        {
            get => _delayNextMax;
            set
            {
                _delayNextMax = value;
                OnPropertyChanged();
                NotifyTimeEstimateChanged();
            }
        }

        public int EmptyPropositionsRepeats
        {
            get => _emptyPropositionsRepeats;
            set
            {
                _emptyPropositionsRepeats = value;
                OnPropertyChanged();
            }
        }

        public int ErrorNumbersRepeats
        {
            get => _errorNumbersRepeats;
            set
            {
                _errorNumbersRepeats = value;
                OnPropertyChanged();
            }
        }

        public bool Is24x7Mode
        {
            get => _is24x7Mode;
            set
            {
                if (_is24x7Mode == value)
                {
                    return;
                }

                _is24x7Mode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AreIterationSlidersEnabled));
            }
        }

        public bool AreIterationSlidersEnabled => !Is24x7Mode;

        public int PhoneNumbersCount
        {
            get => _phoneNumbersCount;
            private set
            {
                if (_phoneNumbersCount == value)
                {
                    return;
                }

                _phoneNumbersCount = value;
                OnPropertyChanged();
                NotifyTimeEstimateChanged();
            }
        }

        public bool ShouldTopUpNumbers
        {
            get => _shouldTopUpNumbers;
            set
            {
                if (_shouldTopUpNumbers == value)
                {
                    return;
                }

                _shouldTopUpNumbers = value;
                OnPropertyChanged();
            }
        }

        public string EstimatedAverageFirstPassDuration => FormatDuration(GetAverageFirstPassSeconds());

        public string EstimatedMaxFirstPassDuration => FormatDuration(GetMaxFirstPassSeconds());

        public async Task RefreshPhoneNumbersCountAsync(IDataService dataService)
        {
            PhoneNumbersCount = await dataService.GetPhoneOffersCountAsync();
        }

        public ProcessingConfiguration GetConfiguration()
        {
            return new ProcessingConfiguration
            {
                DelayInputMin = DelayInputMin,
                DelayInputMax = DelayInputMax,
                DelaySearchMin = DelaySearchMin,
                DelaySearchMax = DelaySearchMax,
                DelayNextMin = DelayNextMin,
                DelayNextMax = DelayNextMax,
                EmptyPropositionsRepeats = EmptyPropositionsRepeats,
                ErrorNumbersRepeats = ErrorNumbersRepeats,
                Is24x7Mode = Is24x7Mode,
                ShouldTopUpNumbers = ShouldTopUpNumbers
            };
        }

        public void LoadFromConfiguration(ProcessingConfiguration config)
        {
            DelayInputMin = config.DelayInputMin;
            DelayInputMax = config.DelayInputMax;
            DelaySearchMin = config.DelaySearchMin;
            DelaySearchMax = config.DelaySearchMax;
            DelayNextMin = config.DelayNextMin;
            DelayNextMax = config.DelayNextMax;
            EmptyPropositionsRepeats = config.EmptyPropositionsRepeats;
            ErrorNumbersRepeats = config.ErrorNumbersRepeats;
            Is24x7Mode = config.Is24x7Mode;
            ShouldTopUpNumbers = config.ShouldTopUpNumbers;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void NotifyTimeEstimateChanged()
        {
            OnPropertyChanged(nameof(EstimatedAverageFirstPassDuration));
            OnPropertyChanged(nameof(EstimatedMaxFirstPassDuration));
        }

        private double GetAverageFirstPassSeconds()
        {
            var inputAverage = (Math.Min(DelayInputMin, DelayInputMax) + Math.Max(DelayInputMin, DelayInputMax)) / 2.0;
            var searchAverage = (Math.Min(DelaySearchMin, DelaySearchMax) + Math.Max(DelaySearchMin, DelaySearchMax)) / 2.0;
            var nextAverage = (Math.Min(DelayNextMin, DelayNextMax) + Math.Max(DelayNextMin, DelayNextMax)) / 2.0;
            return PhoneNumbersCount * (inputAverage + searchAverage + nextAverage);
        }

        private double GetMaxFirstPassSeconds()
        {
            var inputMax = Math.Max(DelayInputMin, DelayInputMax);
            var searchMax = Math.Max(DelaySearchMin, DelaySearchMax);
            var nextMax = Math.Max(DelayNextMin, DelayNextMax);
            return PhoneNumbersCount * (inputMax + searchMax + nextMax);
        }

        private static string FormatDuration(double totalSeconds)
        {
            var duration = TimeSpan.FromSeconds(Math.Max(0, Math.Ceiling(totalSeconds)));

            if (duration.TotalHours >= 1)
            {
                return $"{(int)duration.TotalHours} ч {duration.Minutes} мин {duration.Seconds} сек";
            }

            if (duration.TotalMinutes >= 1)
            {
                return $"{duration.Minutes} мин {duration.Seconds} сек";
            }

            return $"{duration.Seconds} сек";
        }
    }
}

