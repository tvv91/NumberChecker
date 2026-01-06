using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using VodafoneNumberChecker.Models;
using VodafoneNumberChecker.Services;

namespace VodafoneNumberChecker.ViewModels
{
    public class PropositionTypesViewModel : INotifyPropertyChanged
    {
        private readonly IDataService _dataService;
        private ObservableCollection<PropositionType> _propositionTypes = new();
        private bool _isRealtime;
        private DispatcherTimer? _realtimeTimer;

        public PropositionTypesViewModel(IDataService dataService)
        {
            _dataService = dataService;
            LoadDataCommand = new RelayCommand(async () => await LoadDataAsync());
        }

        public ObservableCollection<PropositionType> PropositionTypes
        {
            get => _propositionTypes;
            set
            {
                _propositionTypes = value;
                OnPropertyChanged();
            }
        }

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

        public ICommand LoadDataCommand { get; }

        public async Task LoadDataAsync()
        {
            var types = await _dataService.GetPropositionTypesAsync();
            
            PropositionTypes.Clear();
            foreach (var type in types)
            {
                PropositionTypes.Add(type);
            }
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
}
