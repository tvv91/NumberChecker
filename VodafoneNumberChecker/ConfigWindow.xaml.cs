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
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}

