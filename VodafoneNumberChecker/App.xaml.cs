using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Windows;
using VodafoneNumberChecker.Data;
using VodafoneNumberChecker.Services;
using VodafoneNumberChecker.ViewModels;

namespace VodafoneNumberChecker
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

            // Setup global exception handlers
            SetupExceptionHandling();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            _serviceProvider = serviceCollection.BuildServiceProvider();

            try
            {
                // Get logger first
                var logger = _serviceProvider.GetRequiredService<ILoggerService>();
                logger.LogInfo("Application starting...");

                // Ensure database is created
                var dataService = _serviceProvider.GetRequiredService<IDataService>();
                await dataService.EnsureDatabaseCreatedAsync();

                // Load initial data for PhoneOffersViewModel
                var phoneOffersViewModel = _serviceProvider.GetRequiredService<PhoneOffersViewModel>();
                await phoneOffersViewModel.LoadDataAsync();
                
                // Load initial data for PropositionTypesViewModel
                var propositionTypesViewModel = _serviceProvider.GetRequiredService<PropositionTypesViewModel>();
                await propositionTypesViewModel.LoadDataAsync();

                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();

                logger.LogInfo("Application started successfully");
            }
            catch (Exception ex)
            {
                var logger = _serviceProvider?.GetService<ILoggerService>();
                logger?.LogError("Fatal error during application startup", ex);
                
                MessageBox.Show(
                    $"Fatal error during application startup:\n{ex.Message}\n\nCheck the log file for details.",
                    "Fatal Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                
                Shutdown(1);
            }
        }

        private void SetupExceptionHandling()
        {
            // Handle UI thread exceptions
            DispatcherUnhandledException += (sender, e) =>
            {
                var logger = _serviceProvider?.GetService<ILoggerService>();
                logger?.LogError("Unhandled exception on UI thread", e.Exception);
                
                e.Handled = true;
                
                MessageBox.Show(
                    $"An unhandled exception occurred:\n{e.Exception.Message}\n\nCheck the log file for details.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            };

            // Handle non-UI thread exceptions
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var logger = _serviceProvider?.GetService<ILoggerService>();
                if (e.ExceptionObject is Exception ex)
                {
                    logger?.LogError("Unhandled exception on non-UI thread", ex);
                }
                else
                {
                    logger?.LogError($"Unhandled exception: {e.ExceptionObject}");
                }
            };

            // Handle Task exceptions
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                var logger = _serviceProvider?.GetService<ILoggerService>();
                logger?.LogError("Unobserved task exception", e.Exception);
                e.SetObserved();
            };
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Register DbContext
            services.AddDbContextFactory<AppDbContext>(options =>
                options.UseSqlite("Data Source=vodafone.db"));

            // Register logger first (singleton)
            services.AddSingleton<ILoggerService, FileLoggerService>();

            // Register services
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IWebViewService, WebViewService>();
            services.AddSingleton<IDataService, DataService>();
            services.AddSingleton<IPhoneSearchService>(sp => 
                new PhoneSearchService(
                    sp.GetRequiredService<IWebViewService>(),
                    sp.GetRequiredService<IDataService>(),
                    sp.GetRequiredService<ILoggerService>()));

            // Register ViewModels - PhoneOffersViewModel and ConfigViewModel must be created first
            services.AddTransient<PhoneOffersViewModel>();
            services.AddTransient<PropositionTypesViewModel>();
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
                    configViewModel,
                    sp.GetRequiredService<ILoggerService>());
            });

            // Register Views
            services.AddTransient<MainWindow>(sp =>
            {
                var phoneOffersViewModel = sp.GetRequiredService<PhoneOffersViewModel>();
                var propositionTypesViewModel = sp.GetRequiredService<PropositionTypesViewModel>();
                return new MainWindow(
                    sp.GetRequiredService<MainWindowViewModel>(),
                    phoneOffersViewModel,
                    propositionTypesViewModel,
                    sp.GetRequiredService<IWebViewService>());
            });
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}
