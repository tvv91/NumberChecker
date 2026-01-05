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

        public void UpdateProgress(int current, int total, string? currentNumber = null)
        {
            if (total > 0)
            {
                Progress = (current * 100.0) / total;
                ProgressText = $"{current} из {total}";
                
                if (current < total && !string.IsNullOrEmpty(currentNumber))
                {
                    StatusText = $"Импорт номера: {currentNumber}";
                }
                else if (current == total)
                {
                    StatusText = $"Импорт завершен: {current} из {total} номеров";
                }
                else
                {
                    StatusText = $"Импортировано {current} из {total} номеров";
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
