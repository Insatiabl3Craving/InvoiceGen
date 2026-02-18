using System;
using System.Windows;
using InvoiceGenerator.Models;
using InvoiceGenerator.Services;

namespace InvoiceGenerator.Views
{
    public partial class ClientEditDialog : Window
    {
        private readonly ClientService _clientService = new();
        private Client? _currentClient;

        public ClientEditDialog()
        {
            InitializeComponent();
        }

        public ClientEditDialog(Client client)
        {
            InitializeComponent();
            _currentClient = client;
            PopulateFields(client);
        }

        private void PopulateFields(Client client)
        {
            DisplayNameTB.Text = client.DisplayName;
            EmailTB.Text = client.ContactEmail;
            AddressTB.Text = client.BillingAddress;
            AdditionalTB.Text = client.AdditionalInfo ?? "";
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

            try
            {
                if (_currentClient == null)
                {
                    // Add new client
                    var newClient = new Client
                    {
                        DisplayName = DisplayNameTB.Text,
                        ContactEmail = EmailTB.Text,
                        BillingAddress = AddressTB.Text,
                        AdditionalInfo = AdditionalTB.Text
                    };
                    await _clientService.AddClientAsync(newClient);
                }
                else
                {
                    // Edit existing client
                    _currentClient.DisplayName = DisplayNameTB.Text;
                    _currentClient.ContactEmail = EmailTB.Text;
                    _currentClient.BillingAddress = AddressTB.Text;
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
    }
}
