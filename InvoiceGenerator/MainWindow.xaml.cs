using System.Windows;
using InvoiceGenerator.Views;

namespace InvoiceGenerator
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ClientManagerBtn_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new ClientManagerView();
        }

        private void InvoiceBuilderBtn_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new InvoiceBuilderView();
        }

        private void HistoryBtn_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new InvoiceHistoryView();
        }

        private void SettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new SettingsView();
        }
    }
}
