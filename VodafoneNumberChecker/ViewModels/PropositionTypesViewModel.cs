using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using OfficeOpenXml;
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
            ExportToExcelCommand = new RelayCommand(async () => await ExportToExcelAsync(), () => PropositionTypes.Count > 0);
            
            // Subscribe to collection changes to update export button state
            _propositionTypes.CollectionChanged += PropositionTypes_CollectionChanged;
        }

        public ObservableCollection<PropositionType> PropositionTypes
        {
            get => _propositionTypes;
            set
            {
                // Unsubscribe from old collection if it exists
                if (_propositionTypes != null)
                {
                    _propositionTypes.CollectionChanged -= PropositionTypes_CollectionChanged;
                }
                
                _propositionTypes = value;
                
                // Subscribe to new collection
                if (_propositionTypes != null)
                {
                    _propositionTypes.CollectionChanged += PropositionTypes_CollectionChanged;
                }
                
                OnPropertyChanged();
                // Update export command state
                ((RelayCommand)ExportToExcelCommand).RaiseCanExecuteChanged();
            }
        }

        private void PropositionTypes_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Update export command state when collection changes
            ((RelayCommand)ExportToExcelCommand).RaiseCanExecuteChanged();
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
        public ICommand ExportToExcelCommand { get; }

        public async Task LoadDataAsync()
        {
            var types = await _dataService.GetPropositionTypesAsync();
            
            PropositionTypes.Clear();
            foreach (var type in types)
            {
                PropositionTypes.Add(type);
            }
            // CollectionChanged event will automatically update export command state
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

        public async Task ExportToExcelAsync()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                    FileName = $"PropositionTypes_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                    DefaultExt = "xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Get all proposition types
                    var allTypes = await _dataService.GetPropositionTypesAsync();

                    // Set license context for EPPlus (non-commercial use)
                    ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                    using var package = new ExcelPackage();
                    var worksheet = package.Workbook.Worksheets.Add("Proposition Types");

                    // Headers
                    worksheet.Cells[1, 1].Value = "ID";
                    worksheet.Cells[1, 2].Value = "Title";
                    worksheet.Cells[1, 3].Value = "Count";
                    worksheet.Cells[1, 4].Value = "Content";

                    // Style header row
                    using (var range = worksheet.Cells[1, 1, 1, 4])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    }

                    // Data rows
                    for (int i = 0; i < allTypes.Count; i++)
                    {
                        var type = allTypes[i];
                        int row = i + 2;
                        worksheet.Cells[row, 1].Value = type.Id;
                        worksheet.Cells[row, 2].Value = type.Title;
                        worksheet.Cells[row, 3].Value = type.Count;
                        worksheet.Cells[row, 4].Value = type.Content;
                    }

                    // Set column widths to match the view
                    worksheet.Column(1).Width = 6; // ID column (50px ≈ 6 Excel units)
                    worksheet.Column(3).Width = 10; // Count column (80px ≈ 10 Excel units)
                    
                    // Title column: auto-fit with minimum width (200px ≈ 25 Excel units)
                    // First set a reasonable width, then auto-fit
                    worksheet.Column(2).Width = 25; // Minimum width
                    worksheet.Column(2).AutoFit(); // Auto-fit based on content
                    if (worksheet.Column(2).Width < 25)
                    {
                        worksheet.Column(2).Width = 25; // Ensure minimum width
                    }
                    
                    // Content column: set to reasonable width (300px+ ≈ 50 Excel units) with wrapping
                    worksheet.Column(4).Width = 50;

                    // Enable text wrapping for Title and Content columns
                    worksheet.Column(2).Style.WrapText = true;
                    worksheet.Column(4).Style.WrapText = true;

                    // Calculate and set row heights to accommodate wrapped text
                    // Use accurate calculation with word wrapping and explicit newlines
                    for (int i = 0; i < allTypes.Count; i++)
                    {
                        int row = i + 2;
                        var type = allTypes[i];
                        
                        // Calculate lines needed for Title
                        double titleColWidth = worksheet.Column(2).Width;
                        int titleLines = CalculateWrappedLines(type.Title ?? "", titleColWidth);
                        
                        // Calculate lines needed for Content
                        double contentColWidth = worksheet.Column(4).Width;
                        int contentLines = CalculateWrappedLines(type.Content ?? "", contentColWidth);
                        
                        // Set row height based on the maximum lines needed
                        // 15 points per line, add 3 points padding per line to ensure all text is visible
                        int maxLines = Math.Max(titleLines, contentLines);
                        double rowHeight = Math.Max(20, maxLines * 18); // 15 points per line + 3 points padding, minimum 20
                        worksheet.Row(row).Height = rowHeight;
                    }

                    // Save file
                    var fileInfo = new FileInfo(saveFileDialog.FileName);
                    await package.SaveAsAsync(fileInfo);

                    System.Windows.MessageBox.Show($"Экспортировано {allTypes.Count} записей в файл:\n{saveFileDialog.FileName}", 
                        "Экспорт завершен", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка при экспорте: {ex.Message}", 
                    "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private int CalculateWrappedLines(string text, double columnWidth)
        {
            if (string.IsNullOrEmpty(text))
                return 1;
            
            // Count explicit newlines first
            int explicitNewlines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None).Length - 1;
            
            // Calculate characters per line (Excel: ~7 characters per column width unit)
            int charsPerLine = Math.Max(10, (int)(columnWidth * 7));
            
            // Remove newlines for word wrapping calculation
            string textForWrapping = text.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
            
            // Calculate word-wrapped lines
            var words = textForWrapping.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            int wrappedLines = 1;
            int currentLineChars = 0;
            
            foreach (var word in words)
            {
                int wordLength = word.Length;
                // Add space if not first word on line
                if (currentLineChars > 0)
                    wordLength++;
                
                if (currentLineChars + wordLength > charsPerLine && currentLineChars > 0)
                {
                    wrappedLines++;
                    currentLineChars = word.Length;
                }
                else
                {
                    currentLineChars += wordLength;
                }
            }
            
            // Return maximum of explicit newlines + 1 and wrapped lines
            return Math.Max(explicitNewlines + 1, wrappedLines);
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
