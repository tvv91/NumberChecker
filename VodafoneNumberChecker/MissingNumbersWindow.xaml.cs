using System.Windows;

namespace VodafoneNumberChecker
{
    public partial class MissingNumbersWindow : Window
    {
        private readonly List<string> _missingNumbers;

        public MissingNumbersWindow(IEnumerable<string> missingNumbers)
        {
            InitializeComponent();

            _missingNumbers = missingNumbers
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            numbersTextBox.Text = string.Join(Environment.NewLine, _missingNumbers);
            countTextBlock.Text = $"Не найдено номеров: {_missingNumbers.Count}";
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (_missingNumbers.Count == 0)
            {
                return;
            }

            Clipboard.SetText(numbersTextBox.Text);
            MessageBox.Show(
                "Список скопирован в буфер обмена.",
                "Готово",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
