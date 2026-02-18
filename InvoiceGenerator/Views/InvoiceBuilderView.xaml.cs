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
        private readonly WordTemplateService _wordTemplateService = new();
        private readonly PdfConversionService _pdfConversionService = new();
        private List<InvoiceLineItem> _currentLineItems = new();
        private Client? _selectedClient;

        public InvoiceBuilderView()
        {
            InitializeComponent();
            LoadClients();
            _ = SetNextInvoiceNumberAsync();
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

        private async System.Threading.Tasks.Task SetNextInvoiceNumberAsync()
        {
            try
            {
                InvoiceNumberTB.Text = await _invoiceService.GetNextInvoiceNumberAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating invoice number: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                await SetNextInvoiceNumberAsync();
                if (string.IsNullOrWhiteSpace(InvoiceNumberTB.Text))
                {
                    MessageBox.Show("Please enter an invoice number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
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

                if (string.IsNullOrWhiteSpace(settings.InvoicesFolderPath))
                {
                    MessageBox.Show("Please configure the invoices folder path in Settings.", "Configuration Required", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Ensure output directory exists
                System.IO.Directory.CreateDirectory(settings.InvoicesFolderPath);

                // Generate file paths
                var fileName = $"Invoice_{InvoiceNumberTB.Text}_{DateTime.Now:yyyyMMdd_HHmmss}";
                var docxPath = System.IO.Path.Combine(settings.InvoicesFolderPath, fileName + ".docx");
                var pdfPath = System.IO.Path.Combine(settings.InvoicesFolderPath, fileName + ".pdf");

                // Prepare template replacements
                var replacements = new Dictionary<string, string>
                {
                    { "INVOICE_NUMBER", InvoiceNumberTB.Text },
                    { "INVOICE_NO", InvoiceNumberTB.Text },
                    { "CLIENT_NAME", _selectedClient.DisplayName },
                    { "CUSTOMER_NAME", _selectedClient.DisplayName },
                    { "CLIENT_EMAIL", _selectedClient.ContactEmail },
                    { "EMAIL", _selectedClient.ContactEmail },
                    { "CLIENT_ADDRESS", _selectedClient.BillingAddress },
                    { "ADDRESS", _selectedClient.BillingAddress },
                    { "CUSTOMER_STREET", _selectedClient.StreetAddress ?? string.Empty },
                    { "CUSTOMER_CITY", _selectedClient.City ?? string.Empty },
                    { "CUSTOMER_POSTCODE", _selectedClient.Postcode ?? string.Empty },
                    { "CUSTOMER_ADDRESS", _selectedClient.BillingAddress },
                    { "DATE_FROM", DateFromDP.SelectedDate.Value.ToString("yyyy-MM-dd") },
                    { "DATE_TO", DateToDP.SelectedDate.Value.ToString("yyyy-MM-dd") },
                    { "INVOICE_PERIOD", $"{DateFromDP.SelectedDate.Value:dd/MM/yyyy} - {DateToDP.SelectedDate.Value:dd/MM/yyyy}" },
                    { "DATE_GENERATED", DateTime.Now.ToString("yyyy-MM-dd") },
                    { "TOTAL_AMOUNT", _currentLineItems.Sum(i => i.Amount).ToString("F2") }
                };

                // Add line items as a formatted string
                var lineItemsText = new StringBuilder();
                foreach (var item in _currentLineItems)
                {
                    lineItemsText.AppendLine($"{item.Description} - Qty: {item.Quantity} x ${item.UnitRate:F2} = ${item.Amount:F2}");
                }
                replacements["LINE_ITEMS"] = lineItemsText.ToString();

                // Generate DOCX from template
                _wordTemplateService.ReplaceTemplateFields(settings.TemplateFilePath, docxPath, replacements);

                // Convert to PDF
                _pdfConversionService.ConvertDocxToPdf(docxPath, pdfPath);

                // Create invoice record
                var invoice = new Invoice
                {
                    InvoiceNumber = InvoiceNumberTB.Text,
                    ClientId = _selectedClient.Id,
                    DateFrom = DateFromDP.SelectedDate.Value,
                    DateTo = DateToDP.SelectedDate.Value,
                    DocxFilePath = docxPath,
                    PdfFilePath = pdfPath,
                    Status = "Generated",
                    LineItems = _currentLineItems
                };

                var savedInvoice = await _invoiceService.AddInvoiceAsync(invoice);

                // Update line items with invoice ID
                foreach (var item in _currentLineItems)
                {
                    item.InvoiceId = savedInvoice.Id;
                }

                MessageBox.Show($"Invoice generated successfully!\n\nSaved to: {settings.InvoicesFolderPath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();
                await SetNextInvoiceNumberAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating invoice: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearForm()
        {
            DateFromDP.SelectedDate = null;
            DateToDP.SelectedDate = null;
            _currentLineItems.Clear();
            PreviewTB.Clear();
            ClientComboBox.SelectedIndex = -1;
        }
    }
}
