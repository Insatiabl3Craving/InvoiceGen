using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using InvoiceGenerator.Models;
using InvoiceGenerator.Services;
using System.Text;

namespace InvoiceGenerator.Views
{
    public partial class InvoiceBuilderView : UserControl
    {
        private readonly ClientService _clientService = new();
        private readonly InvoiceService _invoiceService = new();
        private readonly CsvImportService _csvService = new();
        private readonly SettingsService _settingsService = new();
        private List<InvoiceLineItem> _currentLineItems = new();
        private Client? _selectedClient;

        public InvoiceBuilderView()
        {
            InitializeComponent();
            LoadClients();
        }

        private async void LoadClients()
        {
            try
            {
                var clients = await _clientService.GetAllClientsAsync();
                ClientComboBox.ItemsSource = clients;
                ClientComboBox.DisplayMemberPath = "DisplayName";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading clients: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClientComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClientComboBox.SelectedItem is Client client)
            {
                _selectedClient = client;
                UpdatePreview();
            }
        }

        private void ImportCsvBtn_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Import Invoice Line Items"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    if (!_csvService.ValidateCsvFile(openFileDialog.FileName))
                    {
                        MessageBox.Show("CSV file must contain columns: Description, Quantity, UnitRate", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    _currentLineItems = _csvService.ImportLineItemsFromCsv(openFileDialog.FileName, 0);
                    MessageBox.Show($"Successfully imported {_currentLineItems.Count} line items.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    UpdatePreview();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error importing CSV: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PreviewBtn_Click(object sender, RoutedEventArgs e)
        {
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            var preview = new StringBuilder();

            if (_selectedClient != null)
            {
                preview.AppendLine($"Client: {_selectedClient.DisplayName}");
                preview.AppendLine($"Email: {_selectedClient.ContactEmail}");
                preview.AppendLine($"Address: {_selectedClient.BillingAddress}");
                preview.AppendLine();
            }

            preview.AppendLine($"Invoice Number: {InvoiceNumberTB.Text}");
            preview.AppendLine($"Date From: {DateFromDP.SelectedDate:yyyy-MM-dd}");
            preview.AppendLine($"Date To: {DateToDP.SelectedDate:yyyy-MM-dd}");
            preview.AppendLine();
            preview.AppendLine("Line Items:");

            decimal total = 0;
            foreach (var item in _currentLineItems)
            {
                preview.AppendLine($"  {item.Description}: ${item.Amount:F2} (Qty: {item.Quantity} x ${item.UnitRate:F2})");
                total += item.Amount;
            }

            preview.AppendLine();
            preview.AppendLine($"Total: ${total:F2}");

            PreviewTB.Text = preview.ToString();
        }

        private async void GenerateBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedClient == null)
            {
                MessageBox.Show("Please select a client.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(InvoiceNumberTB.Text))
            {
                MessageBox.Show("Please enter an invoice number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!DateFromDP.SelectedDate.HasValue || !DateToDP.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select both from and to dates.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var settings = await _settingsService.GetSettingsAsync();

                if (string.IsNullOrWhiteSpace(settings.TemplateFilePath))
                {
                    MessageBox.Show("Please configure the Word template path in Settings.", "Configuration Required", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (!System.IO.File.Exists(settings.TemplateFilePath))
                {
                    MessageBox.Show("Template file not found at configured path.", "File Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Create invoice
                var invoice = new Invoice
                {
                    InvoiceNumber = InvoiceNumberTB.Text,
                    ClientId = _selectedClient.Id,
                    DateFrom = DateFromDP.SelectedDate.Value,
                    DateTo = DateToDP.SelectedDate.Value,
                    LineItems = _currentLineItems
                };

                var savedInvoice = await _invoiceService.AddInvoiceAsync(invoice);

                // Update line items with invoice ID
                foreach (var item in _currentLineItems)
                {
                    item.InvoiceId = savedInvoice.Id;
                }

                MessageBox.Show("Invoice generated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating invoice: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearForm()
        {
            InvoiceNumberTB.Clear();
            DateFromDP.SelectedDate = null;
            DateToDP.SelectedDate = null;
            _currentLineItems.Clear();
            PreviewTB.Clear();
            ClientComboBox.SelectedIndex = -1;
        }
    }
}
