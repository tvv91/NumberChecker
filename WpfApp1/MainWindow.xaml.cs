using Microsoft.Web.WebView2.Core;
using System.Windows;
using VodafoneLogin.ViewModels;
using VodafoneLogin.Services;

namespace VodafoneLogin
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;
        private readonly IWebViewService _webViewService;

        public MainWindow(MainWindowViewModel viewModel, IWebViewService webViewService)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _webViewService = webViewService;
            DataContext = _viewModel;
            
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