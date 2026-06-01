using System.ComponentModel;
using System.Windows;

namespace VodafoneNumberChecker
{
    public partial class ImportProgressWindow : Window, INotifyPropertyChanged
    {
        private string _statusText = "Импорт номеров...";
        private double _progress;
        private string _progressText = "";
        private bool _canClose = false;

        public ImportProgressWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // Prevent closing until import is complete
            if (!_canClose)
            {
                e.Cancel = true;
            }
        }

        public void AllowClose()
        {
            _canClose = true;
            Close();
        }

        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }

        public double Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged();
            }
        }

        public string ProgressText
        {
            get => _progressText;
            set
            {
                _progressText = value;
                OnPropertyChanged();
            }
        }

        public void UpdateProgress(int current, int total, string? currentNumber = null, string? operationLabel = null)
        {
            if (total > 0)
            {
                Progress = (current * 100.0) / total;
                ProgressText = $"{current} из {total}";

                var operation = string.IsNullOrWhiteSpace(operationLabel) ? "Импорт номера" : operationLabel;
                
                if (current < total && !string.IsNullOrEmpty(currentNumber))
                {
                    StatusText = $"{operation}: {currentNumber}";
                }
                else if (current == total)
                {
                    StatusText = $"{operation} завершен: {current} из {total}";
                }
                else
                {
                    StatusText = $"{operation}: {current} из {total}";
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
