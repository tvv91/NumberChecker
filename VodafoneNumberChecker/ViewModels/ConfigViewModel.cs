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
        private string _timingValidationMessage = string.Empty;

        public string TimingValidationMessage
        {
            get => _timingValidationMessage;
            private set
            {
                if (_timingValidationMessage == value)
                {
                    return;
                }

                _timingValidationMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasTimingValidationMessage));
            }
        }

        public bool HasTimingValidationMessage => !string.IsNullOrEmpty(TimingValidationMessage);

        public double DelayInputMin
        {
            get => _delayInputMin;
            set => SetDelayMin(ref _delayInputMin, value, _delayInputMax, nameof(DelayInputMin), "Ввод номера");
        }

        public double DelayInputMax
        {
            get => _delayInputMax;
            set => SetDelayMax(ref _delayInputMax, value, _delayInputMin, nameof(DelayInputMax), "Ввод номера");
        }

        public double DelaySearchMin
        {
            get => _delaySearchMin;
            set => SetDelayMin(ref _delaySearchMin, value, _delaySearchMax, nameof(DelaySearchMin), "Нажатие на поиск");
        }

        public double DelaySearchMax
        {
            get => _delaySearchMax;
            set => SetDelayMax(ref _delaySearchMax, value, _delaySearchMin, nameof(DelaySearchMax), "Нажатие на поиск");
        }

        public double DelayNextMin
        {
            get => _delayNextMin;
            set => SetDelayMin(ref _delayNextMin, value, _delayNextMax, nameof(DelayNextMin), "Следующий номер");
        }

        public double DelayNextMax
        {
            get => _delayNextMax;
            set => SetDelayMax(ref _delayNextMax, value, _delayNextMin, nameof(DelayNextMax), "Следующий номер");
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
            DelayInputMin = Math.Min(config.DelayInputMin, config.DelayInputMax);
            DelayInputMax = Math.Max(config.DelayInputMin, config.DelayInputMax);
            DelaySearchMin = Math.Min(config.DelaySearchMin, config.DelaySearchMax);
            DelaySearchMax = Math.Max(config.DelaySearchMin, config.DelaySearchMax);
            DelayNextMin = Math.Min(config.DelayNextMin, config.DelayNextMax);
            DelayNextMax = Math.Max(config.DelayNextMin, config.DelayNextMax);
            EmptyPropositionsRepeats = config.EmptyPropositionsRepeats;
            ErrorNumbersRepeats = config.ErrorNumbersRepeats;
            Is24x7Mode = config.Is24x7Mode;
            ShouldTopUpNumbers = config.ShouldTopUpNumbers;
            TimingValidationMessage = string.Empty;
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

        private void SetDelayMin(ref double field, double requestedValue, double maxValue, string propertyName, string timingLabel)
        {
            var clampedValue = Math.Min(requestedValue, maxValue);
            var wasClamped = requestedValue > maxValue;

            if (wasClamped)
            {
                TimingValidationMessage = $"Минимальная задержка («{timingLabel}») не может быть больше максимальной";
            }
            else if (HasTimingValidationMessage)
            {
                TimingValidationMessage = string.Empty;
            }

            if (field == clampedValue && !wasClamped)
            {
                return;
            }

            field = clampedValue;
            OnPropertyChanged(propertyName);
            NotifyTimeEstimateChanged();
        }

        private void SetDelayMax(ref double field, double requestedValue, double minValue, string propertyName, string timingLabel)
        {
            var clampedValue = Math.Max(requestedValue, minValue);
            var wasClamped = requestedValue < minValue;

            if (wasClamped)
            {
                TimingValidationMessage = $"Максимальная задержка («{timingLabel}») не может быть меньше минимальной";
            }
            else if (HasTimingValidationMessage)
            {
                TimingValidationMessage = string.Empty;
            }

            if (field == clampedValue && !wasClamped)
            {
                return;
            }

            field = clampedValue;
            OnPropertyChanged(propertyName);
            NotifyTimeEstimateChanged();
        }

        private double GetAverageFirstPassSeconds()
        {
            var inputAverage = (DelayInputMin + DelayInputMax) / 2.0;
            var searchAverage = (DelaySearchMin + DelaySearchMax) / 2.0;
            var nextAverage = (DelayNextMin + DelayNextMax) / 2.0;
            return PhoneNumbersCount * (inputAverage + searchAverage + nextAverage);
        }

        private double GetMaxFirstPassSeconds()
        {
            return PhoneNumbersCount * (DelayInputMax + DelaySearchMax + DelayNextMax);
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

