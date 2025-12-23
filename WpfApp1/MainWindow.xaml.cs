using Microsoft.Web.WebView2.Core;
using System.Windows;
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
            Loaded += (s, e) =>
            {
                if (phoneOffersTab != null)
                {
                    phoneOffersTab.DataContext = _phoneOffersViewModel;
                }
            };
            
            // Set the WebView instance in the service
            _webViewService.WebView = webView;
        }

        private async void webView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            await _viewModel.StartPhoneSearchAsync();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitializeWebViewAsync();
        }
    }
}