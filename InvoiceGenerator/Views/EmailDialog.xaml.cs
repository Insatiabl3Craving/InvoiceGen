using System;
using System.Windows;
using InvoiceGenerator.Models;
using InvoiceGenerator.Services;

namespace InvoiceGenerator.Views
{
    public partial class EmailDialog : Window
    {
        private readonly Invoice _invoice;
        private readonly SettingsService _settingsService = new();
        private readonly EmailService _emailService = new();
        private readonly InvoiceService _invoiceService = new();

        public EmailDialog(Invoice invoice)
        {
            InitializeComponent();
            _invoice = invoice;
            InitializeForm();
        }

        private async void InitializeForm()
        {
            try
            {
                var settings = await _settingsService.GetSettingsAsync();

                ToTB.Text = _invoice.Client?.ContactEmail ?? "";
                SubjectTB.Text = settings.EmailDefaultSubject
                    .Replace("{InvoiceNumber}", _invoice.InvoiceNumber)
                    .Replace("{DateRange}", $"{_invoice.DateFrom:yyyy-MM-dd} to {_invoice.DateTo:yyyy-MM-dd}");
                BodyTB.Text = settings.EmailDefaultBody;

                if (System.IO.File.Exists(_invoice.PdfFilePath))
                {
                    AttachmentTB.Text = $"Attachment: {System.IO.Path.GetFileName(_invoice.PdfFilePath)}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing form: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SendBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ToTB.Text))
            {
                MessageBox.Show("Email address is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var settings = await _settingsService.GetSettingsAsync();

                if (string.IsNullOrWhiteSpace(settings.EmailSmtpServer))
                {
                    MessageBox.Show("Email settings not configured. Please configure in Settings.", "Configuration Required", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var password = CredentialManager.GetPassword("InvoiceGeneratorEmail");
                if (string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Email password not found in Credential Manager.", "Authentication Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await _emailService.SendInvoiceEmailAsync(
                    settings.EmailSmtpServer,
                    settings.EmailSmtpPort,
                    settings.EmailFromAddress,
                    password,
                    ToTB.Text,
                    SubjectTB.Text,
                    BodyTB.Text,
                    _invoice.PdfFilePath,
                    settings.EmailUseTls);

                _invoice.Status = "Sent";
                _invoice.EmailSentDate = System.DateTime.Now;
                await _invoiceService.UpdateInvoiceAsync(_invoice);

                MessageBox.Show("Email sent successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending email: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
