using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using VodafoneLogin.Data;
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

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            _serviceProvider = serviceCollection.BuildServiceProvider();

            // Ensure database is created
            var dataService = _serviceProvider.GetRequiredService<IDataService>();
            await dataService.EnsureDatabaseCreatedAsync();

            // Load initial data for PhoneOffersViewModel
            var phoneOffersViewModel = _serviceProvider.GetRequiredService<PhoneOffersViewModel>();
            await phoneOffersViewModel.LoadDataAsync();

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Register DbContext
            services.AddDbContextFactory<AppDbContext>(options =>
                options.UseSqlite("Data Source=vodafone.db"));

            // Register services
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IWebViewService, WebViewService>();
            services.AddSingleton<IDataService, DataService>();
            services.AddSingleton<IPhoneSearchService, PhoneSearchService>();

            // Register ViewModels - PhoneOffersViewModel and ConfigViewModel must be created first
            services.AddTransient<PhoneOffersViewModel>();
            services.AddSingleton<ConfigViewModel>();
            services.AddTransient<MainWindowViewModel>(sp =>
            {
                var phoneOffersViewModel = sp.GetRequiredService<PhoneOffersViewModel>();
                var configViewModel = sp.GetRequiredService<ConfigViewModel>();
                return new MainWindowViewModel(
                    sp.GetRequiredService<IFileService>(),
                    sp.GetRequiredService<IWebViewService>(),
                    sp.GetRequiredService<IPhoneSearchService>(),
                    sp.GetRequiredService<IDataService>(),
                    phoneOffersViewModel,
                    configViewModel);
            });

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
