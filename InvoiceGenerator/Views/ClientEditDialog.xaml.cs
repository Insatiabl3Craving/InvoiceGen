using System;
using System.Linq;
using System.Windows;
using InvoiceGenerator.Models;
using InvoiceGenerator.Services;
using InvoiceGenerator.Utilities;

namespace InvoiceGenerator.Views
{
    public partial class ClientEditDialog : Window
    {
        private readonly ClientService _clientService = new();
        private Client? _currentClient;

        public ClientEditDialog()
        {
            InitializeComponent();
            DarkTitleBarHelper.Apply(this, IsDarkThemeActive());
        }

        public ClientEditDialog(Client client)
        {
            InitializeComponent();
            DarkTitleBarHelper.Apply(this, IsDarkThemeActive());
            _currentClient = client;
            PopulateFields(client);
        }

        private void PopulateFields(Client client)
        {
            DisplayNameTB.Text = client.DisplayName;
            EmailTB.Text = client.ContactEmail;
            StreetTB.Text = client.StreetAddress ?? string.Empty;
            CityTB.Text = client.City ?? string.Empty;
            PostcodeTB.Text = client.Postcode ?? string.Empty;
            AdditionalTB.Text = client.AdditionalInfo ?? "";

            if (string.IsNullOrWhiteSpace(StreetTB.Text)
                && string.IsNullOrWhiteSpace(CityTB.Text)
                && string.IsNullOrWhiteSpace(PostcodeTB.Text)
                && !string.IsNullOrWhiteSpace(client.BillingAddress))
            {
                var parts = client.BillingAddress
                    .Replace("\r\n", "\n")
                    .Split(new[] { '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length > 0)
                {
                    StreetTB.Text = parts[0].Trim();
                }

                if (parts.Length > 1)
                {
                    CityTB.Text = parts[1].Trim();
                }

                if (parts.Length > 2)
                {
                    PostcodeTB.Text = parts[2].Trim();
                }
            }
        }

        private async void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DisplayNameTB.Text))
            {
                MessageBox.Show("Display Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(EmailTB.Text))
            {
                MessageBox.Show("Contact Email is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(StreetTB.Text)
                || string.IsNullOrWhiteSpace(CityTB.Text)
                || string.IsNullOrWhiteSpace(PostcodeTB.Text))
            {
                MessageBox.Show("Street, City, and Postcode are required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (_currentClient == null)
                {
                    // Add new client
                    var newClient = new Client
                    {
                        DisplayName = DisplayNameTB.Text,
                        ContactEmail = EmailTB.Text,
                        StreetAddress = StreetTB.Text.Trim(),
                        City = CityTB.Text.Trim(),
                        Postcode = PostcodeTB.Text.Trim(),
                        BillingAddress = BuildBillingAddress(StreetTB.Text, CityTB.Text, PostcodeTB.Text),
                        AdditionalInfo = AdditionalTB.Text
                    };
                    await _clientService.AddClientAsync(newClient);
                }
                else
                {
                    // Edit existing client
                    _currentClient.DisplayName = DisplayNameTB.Text;
                    _currentClient.ContactEmail = EmailTB.Text;
                    _currentClient.StreetAddress = StreetTB.Text.Trim();
                    _currentClient.City = CityTB.Text.Trim();
                    _currentClient.Postcode = PostcodeTB.Text.Trim();
                    _currentClient.BillingAddress = BuildBillingAddress(StreetTB.Text, CityTB.Text, PostcodeTB.Text);
                    _currentClient.AdditionalInfo = AdditionalTB.Text;
                    await _clientService.UpdateClientAsync(_currentClient);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving client: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private static string BuildBillingAddress(string street, string city, string postcode)
        {
            return string.Join(", ", new[] { street?.Trim(), city?.Trim(), postcode?.Trim() }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
        }

        private static bool IsDarkThemeActive()
        {
            var dict = Application.Current?.Resources.MergedDictionaries.Count > 0
                ? Application.Current.Resources.MergedDictionaries[0]
                : null;

            return dict?.Source?.OriginalString.Contains("DarkTheme.xaml", StringComparison.OrdinalIgnoreCase) == true;
        }
    }
}
