using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using VodafoneLogin.Services;
using VodafoneLogin.ViewModels;

namespace VodafoneLogin
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            _serviceProvider = serviceCollection.BuildServiceProvider();

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Register services
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IWebViewService, WebViewService>();
            services.AddSingleton<IPhoneSearchService, PhoneSearchService>();

            // Register ViewModels
            services.AddTransient<MainWindowViewModel>();

            // Register Views
            services.AddTransient<MainWindow>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}
