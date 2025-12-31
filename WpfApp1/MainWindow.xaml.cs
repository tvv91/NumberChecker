using Microsoft.Web.WebView2.Core;
using System.Windows;
using System.Windows.Controls;
using VodafoneLogin.ViewModels;
using VodafoneLogin.Services;

namespace VodafoneLogin
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;
        private readonly PhoneOffersViewModel _phoneOffersViewModel;
        private readonly IWebViewService _webViewService;
        private bool _isWebViewVisible = true;

        public MainWindow(MainWindowViewModel viewModel, PhoneOffersViewModel phoneOffersViewModel, IWebViewService webViewService)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _phoneOffersViewModel = phoneOffersViewModel;
            _webViewService = webViewService;
            
            // Set main ViewModel as DataContext
            DataContext = _viewModel;
            
            // Subscribe to authentication changes to show/hide WebView tab
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            
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
                }
                
                // Set initial WebView tab visibility
                UpdateWebViewTabVisibility();
            };
            
            // Set the WebView instance in the service
            _webViewService.WebView = webView;
        }

        public void ToggleWebViewVisibility()
        {
            _isWebViewVisible = !_isWebViewVisible;
            UpdateWebViewTabVisibility();
        }

        private async void webView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            // Check authentication status after navigation
            if (_viewModel != null)
            {
                await _viewModel.CheckAuthenticationStatusAsync();
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitializeWebViewAsync();
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Authentication changes no longer affect WebView visibility
            // WebView is controlled only by the toggle button
        }

        private void PhoneOffersDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            // Update column visibility when DataGrid is loaded
            // Use Dispatcher to ensure columns are fully initialized
            Dispatcher.BeginInvoke(new Action(() => UpdateColumnVisibility()), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void PhoneOffersViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PhoneOffersViewModel.ShowAllFields) && phoneOffersDataGrid != null)
            {
                // Use Dispatcher to ensure columns are fully initialized
                Dispatcher.BeginInvoke(new Action(() => UpdateColumnVisibility()), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        private void UpdateWebViewTabVisibility()
        {
            if (tabControl == null || webViewTab == null)
                return;

            // Simply show or hide based on toggle state
            if (_isWebViewVisible)
            {
                // Show WebView tab
                if (!tabControl.Items.Contains(webViewTab))
                {
                    // Insert at the beginning
                    tabControl.Items.Insert(0, webViewTab);
                }
            }
            else
            {
                // Hide WebView tab
                if (tabControl.Items.Contains(webViewTab))
                {
                    tabControl.Items.Remove(webViewTab);
                }
            }
        }

        private void ToggleWebViewButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleWebViewVisibility();
            if (toggleWebViewButton != null)
            {
                toggleWebViewButton.Content = _isWebViewVisible ? "👁 Скрыть WebView" : "👁 Показать WebView";
            }
        }

        private void UpdateColumnVisibility()
        {
            if (phoneOffersDataGrid == null || _phoneOffersViewModel == null)
                return;

            // Wait for columns to be initialized
            if (phoneOffersDataGrid.Columns.Count == 0)
                return;

            bool showAllFields = _phoneOffersViewModel.ShowAllFields;

            // Define columns with their headers and original widths
            var columnConfigs = new Dictionary<string, double>
            {
                { "Итераций", 70.0 },
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

            // Find and update all columns
            foreach (var column in phoneOffersDataGrid.Columns)
            {
                var header = column.Header?.ToString();
                
                // Check if this column should be toggled
                if (columnConfigs.ContainsKey(header))
                {
                    var originalWidth = columnConfigs[header];
                    
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