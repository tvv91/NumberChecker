using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using VodafoneNumberChecker.Models;
using VodafoneNumberChecker.Services;

namespace VodafoneNumberCheckerTests
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class WebViewServiceTests
    {
        private WebViewService _webViewService = null!;
        private WebView2 _webView = null!;
        private Window _testWindow = null!;
        private string _testDataPath = null!;
        private string _userDataFolder = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Initialize WPF Application if not already running
            if (Application.Current == null)
            {
                var app = new Application();
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            }

            // Create a temporary user data folder for WebView2
            var tempPath = Path.GetTempPath();
            _userDataFolder = Path.Combine(tempPath, "WebView2TestData", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_userDataFolder);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            // Clean up user data folder
            try
            {
                if (Directory.Exists(_userDataFolder))
                {
                    Directory.Delete(_userDataFolder, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        [SetUp]
        public async Task SetUp()
        {
            var dispatcher = Application.Current.Dispatcher;
            
            // Create a hidden window to host the WebView2 (required for proper initialization)
            dispatcher.Invoke(() =>
            {
                _testWindow = new Window
                {
                    Width = 1,
                    Height = 1,
                    WindowStyle = WindowStyle.None,
                    ShowInTaskbar = false,
                    Visibility = Visibility.Hidden
                };

                _webView = new WebView2();
                _webViewService = new WebViewService
                {
                    WebView = _webView
                };

                // Add WebView2 to the window (needed for proper initialization)
                _testWindow.Content = _webView;
                _testWindow.Show();
            });

            // Process dispatcher messages to ensure window is shown
            DoEvents(dispatcher);

            // Create WebView2 with user data folder
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: _userDataFolder);

            // Get the path to the test data directory (relative to test output directory)
            var assemblyLocation = typeof(WebViewServiceTests).Assembly.Location;
            var assemblyDir = Path.GetDirectoryName(assemblyLocation)!;
            _testDataPath = Path.Combine(assemblyDir, "TestData");

            // Initialize WebView2 with the environment - run directly since we're on STA thread
            var initTask = _webView.EnsureCoreWebView2Async(environment);
            
            // Pump dispatcher while waiting
            while (!initTask.IsCompleted)
            {
                DoEvents(dispatcher);
                await Task.Delay(10);
            }
            
            await initTask;
            
            // Now initialize the service (it will set up event handlers)
            // Since WebView is already initialized, EnsureCoreWebView2Async should return immediately
            var serviceInitTask = _webViewService.InitializeAsync();
            
            // Pump dispatcher while waiting
            while (!serviceInitTask.IsCompleted)
            {
                DoEvents(dispatcher);
                await Task.Delay(10);
            }
            
            await serviceInitTask;
        }

        private void DoEvents(Dispatcher dispatcher)
        {
            var frame = new DispatcherFrame();
            dispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(ExitFrame), frame);
            Dispatcher.PushFrame(frame);
        }

        private object? ExitFrame(object? frame)
        {
            ((DispatcherFrame)frame!).Continue = false;
            return null;
        }

        /// <summary>
        /// Waits for a task to complete while pumping the dispatcher to process UI messages
        /// </summary>
        private async Task WaitForTaskWithDispatcherPumpingAsync(Task task)
        {
            var dispatcher = Application.Current.Dispatcher;
            while (!task.IsCompleted)
            {
                DoEvents(dispatcher);
                await Task.Delay(10);
            }
            await task;
        }

        /// <summary>
        /// Waits for a task with result to complete while pumping the dispatcher to process UI messages
        /// </summary>
        private async Task<T> WaitForTaskWithDispatcherPumpingAsync<T>(Task<T> task)
        {
            var dispatcher = Application.Current.Dispatcher;
            while (!task.IsCompleted)
            {
                DoEvents(dispatcher);
                await Task.Delay(10);
            }
            return await task;
        }

        /// <summary>
        /// Loads an HTML file in WebView, waits for navigation, and returns the deserialized offers response
        /// </summary>
        private async Task<OffersResponseDto> LoadHtmlFileAndGetOffersAsync(string fileName)
        {
            // Arrange
            var htmlFilePath = Path.Combine(_testDataPath, fileName);
            Assert.That(File.Exists(htmlFilePath), Is.True, $"Test HTML file '{fileName}' should exist");

            var fileUri = new Uri(htmlFilePath).ToString();
            
            // Act - Navigate to the HTML file
            var dispatcher = Application.Current.Dispatcher;
            var navigationCompleted = false;
            _webViewService.NavigationCompleted += (s, e) => navigationCompleted = true;
            
            // Navigate - run directly since we're on STA thread
            await WaitForTaskWithDispatcherPumpingAsync(_webViewService.NavigateAsync(fileUri));
            
            // Wait for navigation to complete
            var timeout = 5000; // 5 seconds
            var elapsed = 0;
            while (!navigationCompleted && elapsed < timeout)
            {
                DoEvents(dispatcher);
                await Task.Delay(100);
                elapsed += 100;
            }
            
            // Give a bit more time for DOM to be ready
            await Task.Delay(500);
            DoEvents(dispatcher);

            // Execute GetOffersJsonAsync - run directly since we're on STA thread
            var rawOffersJson = await WaitForTaskWithDispatcherPumpingAsync(_webViewService.GetOffersJsonAsync());
            
            // Deserialize JSON response
            // rawOffersJson is already a JSON string, need to deserialize it first
            // (ExecuteScriptAsync returns the result as a JSON-encoded string)
            var offersJson = JsonSerializer.Deserialize<string>(rawOffersJson) ?? "{}";

            Assert.That(offersJson, Is.Not.Null.And.Not.Empty);

            var response = JsonSerializer.Deserialize<OffersResponseDto>(offersJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.That(response, Is.Not.Null);
            return response!;
        }

        [TearDown]
        public void TearDown()
        {
            var dispatcher = Application.Current.Dispatcher;
            dispatcher.Invoke(() =>
            {
                _testWindow?.Close();
                _webView?.Dispose();
            });
        }

        [Test]
        public async Task ShouldDetectPercentProposition()
        {
            // Act
            var response = await LoadHtmlFileAndGetOffersAsync("percent_proposition.mhtml");

            // Assert
            Assert.That(response.Offers, Is.Not.Null);
            Assert.That(response.Offers.Count, Is.GreaterThan(0), "Should detect at least one percent proposition");
            Assert.That(response.IsPropositionsNotFound, Is.False, "Should not have 'propositions not found' flag");
            
            var hasDiscount = response.Offers.Any(o => o.Discount > 0);
            Assert.That(hasDiscount, Is.True, "Should detect a discount/percent proposition");
        }

        [Test]
        public async Task ShouldDetectGiftProposition()
        {
            // Act
            var response = await LoadHtmlFileAndGetOffersAsync("gift_proposition.mhtml");

            // Assert
            Assert.That(response.Offers, Is.Not.Null);
            Assert.That(response.Offers.Count, Is.GreaterThan(0), "Should detect at least one gift proposition");
            Assert.That(response.IsPropositionsNotFound, Is.False, "Should not have 'propositions not found' flag");
            
            var hasGift = response.Offers.Any(o => o.Gift > 0);
            Assert.That(hasGift, Is.True, "Should detect a gift proposition");
        }

        [Test]
        public async Task ShouldNotDetectPoposition()
        {
            // Act
            var response = await LoadHtmlFileAndGetOffersAsync("no_propositions.mhtml");

            // Assert
            Assert.That(response.Offers, Is.Not.Null);
            Assert.That(response.IsPropositionsNotFound, Is.True, "Should have 'propositions not found' flag");
            Assert.That(response.Offers.Count, Is.EqualTo(0), "Should not detect any propositions");
        }

        [Test]
        public async Task ShouldDetectNotSuitableProposition()
        {
            // Act
            var response = await LoadHtmlFileAndGetOffersAsync("no_suitable.mhtml");

            // Assert
            Assert.That(response.Offers, Is.Not.Null);
            Assert.That(response.IsPropositionsNotFound, Is.False, "Should not have 'propositions not found' flag");
        }
    }
}
