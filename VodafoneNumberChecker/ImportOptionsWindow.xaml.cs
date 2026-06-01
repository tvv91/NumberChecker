using System.Windows;

namespace VodafoneNumberChecker
{
    public partial class ImportOptionsWindow : Window
    {
        public ImportOptionsWindow()
        {
            InitializeComponent();
        }

        public bool IsPriority => priorityCheckBox.IsChecked == true;
        public bool AddToExisting => addToExistingCheckBox.IsChecked == true;

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
