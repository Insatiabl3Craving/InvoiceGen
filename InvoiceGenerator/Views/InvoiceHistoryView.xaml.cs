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
            if (InvoiceHistoryDataGrid.SelectedItems.Count > 1)
            {
                MessageBox.Show("Please select only one invoice to open.", "Multiple Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

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
            if (InvoiceHistoryDataGrid.SelectedItems.Count > 1)
            {
                MessageBox.Show("Please select only one invoice to open.", "Multiple Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

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
            if (InvoiceHistoryDataGrid.SelectedItems.Count > 1)
            {
                MessageBox.Show("Please select only one invoice to email.", "Multiple Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (InvoiceHistoryDataGrid.SelectedItem is Invoice invoice)
            {
                // Check if PDF file exists
                if (string.IsNullOrWhiteSpace(invoice.PdfFilePath) || !System.IO.File.Exists(invoice.PdfFilePath))
                {
                    MessageBox.Show(
                        "PDF file not found for this invoice. The invoice may have been created before file generation was implemented.\n\n" +
                        "Please regenerate the invoice to create the PDF file.",
                        "PDF Not Found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var dialog = new EmailDialog(invoice)
                {
                    Owner = Window.GetWindow(this)
                };
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

        private async void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedInvoices = InvoiceHistoryDataGrid.SelectedItems.Cast<Invoice>().ToList();

            if (selectedInvoices.Count == 0)
            {
                MessageBox.Show("Please select at least one invoice to delete.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirmMessage = selectedInvoices.Count == 1
                ? $"Are you sure you want to delete invoice '{selectedInvoices[0].InvoiceNumber}'?\n\nThis will also delete the DOCX and PDF files."
                : $"Are you sure you want to delete {selectedInvoices.Count} invoices?\n\nThis will also delete all associated DOCX and PDF files.";

            var result = MessageBox.Show(
                confirmMessage,
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                int successCount = 0;
                int errorCount = 0;
                var errors = new List<string>();

                foreach (var invoice in selectedInvoices)
                {
                    try
                    {
                        // Delete physical files
                        if (!string.IsNullOrWhiteSpace(invoice.DocxFilePath) && System.IO.File.Exists(invoice.DocxFilePath))
                        {
                            System.IO.File.Delete(invoice.DocxFilePath);
                        }

                        if (!string.IsNullOrWhiteSpace(invoice.PdfFilePath) && System.IO.File.Exists(invoice.PdfFilePath))
                        {
                            System.IO.File.Delete(invoice.PdfFilePath);
                        }

                        // Delete database record
                        await _invoiceService.DeleteInvoiceAsync(invoice.Id);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        errors.Add($"{invoice.InvoiceNumber}: {ex.Message}");
                    }
                }

                // Show result summary
                if (errorCount == 0)
                {
                    var successMessage = successCount == 1
                        ? "Invoice deleted successfully."
                        : $"{successCount} invoices deleted successfully.";
                    MessageBox.Show(successMessage, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    var errorSummary = $"Deleted {successCount} invoice(s) successfully.\n\n";
                    errorSummary += $"Failed to delete {errorCount} invoice(s):\n";
                    errorSummary += string.Join("\n", errors.Take(5));
                    if (errors.Count > 5)
                    {
                        errorSummary += $"\n... and {errors.Count - 5} more error(s)";
                    }
                    MessageBox.Show(errorSummary, "Partial Success", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                LoadInvoices();
            }
        }
    }
}
