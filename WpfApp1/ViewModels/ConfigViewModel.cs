using System.ComponentModel;
using System.Runtime.CompilerServices;
using VodafoneLogin.Models;

namespace VodafoneLogin.ViewModels
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
                ErrorNumbersRepeats = ErrorNumbersRepeats
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
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

