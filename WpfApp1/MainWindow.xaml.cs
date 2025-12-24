using Microsoft.Web.WebView2.Core;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VodafoneLogin.ViewModels;
using VodafoneLogin.Services;

namespace VodafoneLogin
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;
        private readonly PhoneOffersViewModel _phoneOffersViewModel;
        private readonly IWebViewService _webViewService;

        public MainWindow(MainWindowViewModel viewModel, PhoneOffersViewModel phoneOffersViewModel, IWebViewService webViewService)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _phoneOffersViewModel = phoneOffersViewModel;
            _webViewService = webViewService;
            
            // Set main ViewModel as DataContext
            DataContext = _viewModel;
            
            // Set PhoneOffersViewModel for the second tab
            Loaded += async (s, e) =>
            {
                if (phoneOffersTab != null)
                {
                    phoneOffersTab.DataContext = _phoneOffersViewModel;
                    // Load data when window is loaded
                    await _phoneOffersViewModel.LoadDataAsync();
                    
                    // Subscribe to ShowAllFields changes to show/hide columns
                    _phoneOffersViewModel.PropertyChanged += PhoneOffersViewModel_PropertyChanged;
                    
                    // Set initial column visibility
                    UpdateColumnVisibility();
                }
            };
            
            // Set the WebView instance in the service
            _webViewService.WebView = webView;
        }

        private async void webView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            // Navigation completed - no auto-start scan
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitializeWebViewAsync();
        }

        private void PhoneOffersViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PhoneOffersViewModel.ShowAllFields) && phoneOffersDataGrid != null)
            {
                UpdateColumnVisibility();
            }
        }

        private void UpdateColumnVisibility()
        {
            if (phoneOffersDataGrid == null || _phoneOffersViewModel == null)
                return;

            bool showAllFields = _phoneOffersViewModel.ShowAllFields;

            // Define columns with their headers and original widths
            var columnConfigs = new Dictionary<string, double>
            {
                { "Обновлено", 140.0 },
                { "IsSynchronized", 100.0 },
                { "SyncedAt", 140.0 },
                { "LastSyncAttempt", 140.0 },
                { "SyncAttempts", 100.0 },
                { "SyncError", 200.0 },
                { "IsProcessed", 100.0 },
                { "Ошибка", 60.0 },
                { "Описание ошибки", 200.0 }
            };

            foreach (var (header, originalWidth) in columnConfigs)
            {
                var column = phoneOffersDataGrid.Columns.FirstOrDefault(c => c.Header?.ToString() == header);

                if (column != null)
                {
                    if (showAllFields)
                    {
                        // Show column - restore original width
                        column.Width = new DataGridLength(originalWidth);
                        column.MinWidth = 0;
                        column.MaxWidth = double.PositiveInfinity;
                        column.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        // Hide column completely
                        column.Width = new DataGridLength(0);
                        column.MinWidth = 0;
                        column.MaxWidth = 0;
                        column.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }
    }
}