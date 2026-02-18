using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using InvoiceGenerator.Models;
using InvoiceGenerator.Services;
using System.Diagnostics;

namespace InvoiceGenerator.Views
{
    public partial class InvoiceHistoryView : UserControl
    {
        private readonly InvoiceService _invoiceService = new();
        private readonly SettingsService _settingsService = new();
        private readonly EmailService _emailService = new();
        private List<Invoice> _allInvoices = new();

        public InvoiceHistoryView()
        {
            InitializeComponent();
            LoadInvoices();
        }

        private async void LoadInvoices()
        {
            try
            {
                _allInvoices = await _invoiceService.GetAllInvoicesAsync();
                InvoiceHistoryDataGrid.ItemsSource = _allInvoices;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading invoices: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadInvoices();
        }

        private void SearchTB_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchTB.Text;
            if (string.IsNullOrWhiteSpace(searchText))
            {
                InvoiceHistoryDataGrid.ItemsSource = _allInvoices;
            }
            else
            {
                var filtered = _allInvoices
                    .Where(i => i.InvoiceNumber.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                i.Client?.DisplayName.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();
                InvoiceHistoryDataGrid.ItemsSource = filtered;
            }
        }

        private void OpenDocxBtn_Click(object sender, RoutedEventArgs e)
        {
            if (InvoiceHistoryDataGrid.SelectedItem is Invoice invoice)
            {
                if (System.IO.File.Exists(invoice.DocxFilePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = invoice.DocxFilePath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show("DOCX file not found.", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("Please select an invoice.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OpenPdfBtn_Click(object sender, RoutedEventArgs e)
        {
            if (InvoiceHistoryDataGrid.SelectedItem is Invoice invoice)
            {
                if (System.IO.File.Exists(invoice.PdfFilePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = invoice.PdfFilePath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show("PDF file not found.", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("Please select an invoice.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SendEmailBtn_Click(object sender, RoutedEventArgs e)
        {
            if (InvoiceHistoryDataGrid.SelectedItem is Invoice invoice)
            {
                var dialog = new EmailDialog(invoice);
                dialog.ShowDialog();
            }
            else
            {
                MessageBox.Show("Please select an invoice.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void InvoiceHistoryDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (InvoiceHistoryDataGrid.SelectedItem is Invoice invoice)
            {
                if (System.IO.File.Exists(invoice.PdfFilePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = invoice.PdfFilePath,
                        UseShellExecute = true
                    });
                }
            }
        }
    }
}
