using Microsoft.Web.WebView2.Core;
using System.Windows;
using System.Windows.Controls;
using VodafoneNumberChecker.ViewModels;
using VodafoneNumberChecker.Services;

namespace VodafoneNumberChecker
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;
        private readonly PhoneOffersViewModel _phoneOffersViewModel;
        private readonly PropositionTypesViewModel _propositionTypesViewModel;
        private readonly IWebViewService _webViewService;
        private bool _isWebViewVisible = true;

        public MainWindow(MainWindowViewModel viewModel, PhoneOffersViewModel phoneOffersViewModel, PropositionTypesViewModel propositionTypesViewModel, IWebViewService webViewService)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _phoneOffersViewModel = phoneOffersViewModel;
            _propositionTypesViewModel = propositionTypesViewModel;
            _webViewService = webViewService;
            
            // Set main ViewModel as DataContext
            DataContext = _viewModel;
            
            // Subscribe to authentication changes to show/hide WebView tab
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            
            // Set PhoneOffersViewModel for the second tab and PropositionTypesViewModel for the third tab
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
                
                if (propositionTypesTab != null)
                {
                    propositionTypesTab.DataContext = _propositionTypesViewModel;
                    // Load data when window is loaded
                    await _propositionTypesViewModel.LoadDataAsync();
                    
                    // Subscribe to PropertyChanged to auto-size Title column when data changes
                    _propositionTypesViewModel.PropertyChanged += PropositionTypesViewModel_PropertyChanged;
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
                { "IsProcessed", 100.0 },
                { "Ошибка", 60.0 },
                { "Описание ошибки", 200.0 },
                { "Нет предложений", 150.0 },
                { "Нет подходящих", 150.0 }
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

        private void PropositionTypesDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            // Auto-size Title column when DataGrid is loaded
            UpdateTitleColumnWidth();
        }

        private void PropositionTypesViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PropositionTypesViewModel.PropositionTypes) && propositionTypesDataGrid != null)
            {
                // Auto-size Title column when data changes
                Dispatcher.BeginInvoke(new Action(() => UpdateTitleColumnWidth()), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        private void UpdateTitleColumnWidth()
        {
            if (propositionTypesDataGrid == null || _propositionTypesViewModel == null)
                return;

            if (_propositionTypesViewModel.PropositionTypes == null || _propositionTypesViewModel.PropositionTypes.Count == 0)
                return;

            // Find the Title column
            DataGridTemplateColumn? titleColumn = null;
            foreach (var column in propositionTypesDataGrid.Columns)
            {
                if (column is DataGridTemplateColumn templateColumn && templateColumn.Header?.ToString() == "Title")
                {
                    titleColumn = templateColumn;
                    break;
                }
            }

            if (titleColumn == null)
                return;

            // Create a temporary TextBlock to measure text with the same properties as in the template
            var tempTextBlock = new TextBlock
            {
                FontSize = 12
            };

            double maxWidth = 200; // Start with MinWidth

            // Measure each title
            foreach (var propType in _propositionTypesViewModel.PropositionTypes)
            {
                if (string.IsNullOrEmpty(propType.Title))
                    continue;

                tempTextBlock.Text = propType.Title;
                tempTextBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                // Add only the actual padding from template (left 5 + right 5 = 10) plus minimal margin for grid lines
                double width = tempTextBlock.DesiredSize.Width + 12;

                if (width > maxWidth)
                    maxWidth = width;
            }

            // Also measure the header text
            tempTextBlock.Text = "Title";
            tempTextBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            // Header typically has more padding, add minimal extra
            double headerWidth = tempTextBlock.DesiredSize.Width + 16;

            if (headerWidth > maxWidth)
                maxWidth = headerWidth;

            // Set the column width
            titleColumn.Width = new DataGridLength(maxWidth, DataGridLengthUnitType.Pixel);
        }

        private void PropositionTypesDataGrid_CopyingRowClipboardContent(object sender, DataGridRowClipboardEventArgs e)
        {
            if (e.Item is Models.PropositionType propType)
            {
                // Clear the default clipboard content
                e.ClipboardRowContent.Clear();
                
                // Replace newlines in Content with spaces to maintain tab-separated structure
                string formattedContent = (propType.Content ?? string.Empty)
                    .Replace("\r\n", " ")
                    .Replace("\n", " ")
                    .Replace("\r", " ")
                    .Trim();
                
                // Add all columns: ID, Title, Count, Content
                e.ClipboardRowContent.Add(new DataGridClipboardCellContent(e.Item, propositionTypesDataGrid.Columns[0], propType.Id.ToString()));
                e.ClipboardRowContent.Add(new DataGridClipboardCellContent(e.Item, propositionTypesDataGrid.Columns[1], propType.Title ?? string.Empty));
                e.ClipboardRowContent.Add(new DataGridClipboardCellContent(e.Item, propositionTypesDataGrid.Columns[2], propType.Count.ToString()));
                e.ClipboardRowContent.Add(new DataGridClipboardCellContent(e.Item, propositionTypesDataGrid.Columns[3], formattedContent));
            }
        }
    }
}