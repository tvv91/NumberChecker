using System.Windows;
using VodafoneNumberChecker.ViewModels;

namespace VodafoneNumberChecker
{
    public partial class ConfigWindow : Window
    {
        public ConfigViewModel ViewModel { get; }

        public ConfigWindow(ConfigViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = viewModel;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.ValidateTopUpRules())
            {
                MessageBox.Show(
                    ViewModel.TopUpRulesValidationMessage,
                    "Настройки",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
        }

        private void TopUpRulesDataGrid_CellEditEnding(object sender, System.Windows.Controls.DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == System.Windows.Controls.DataGridEditAction.Cancel)
            {
                return;
            }

            Dispatcher.BeginInvoke(ViewModel.ValidateTopUpRules);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}

