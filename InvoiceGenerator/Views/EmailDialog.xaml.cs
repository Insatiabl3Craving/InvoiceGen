using System;
using System.Windows;
using InvoiceGenerator.Models;
using InvoiceGenerator.Services;
using InvoiceGenerator.Utilities;

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
            DarkTitleBarHelper.Apply(this, IsDarkThemeActive());
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

            // Validate PDF file exists
            if (string.IsNullOrWhiteSpace(_invoice.PdfFilePath) || !System.IO.File.Exists(_invoice.PdfFilePath))
            {
                MessageBox.Show(
                    $"PDF file not found: {_invoice.PdfFilePath}\n\n" +
                    "The invoice file may have been moved or deleted. Please regenerate the invoice.",
                    "File Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
                    MessageBox.Show("Email password not found in Credential Manager.\n\nPlease go to Settings and save your email password again.", "Authentication Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Trim password in case of whitespace issues
                password = password.Trim();

                await _emailService.SendInvoiceEmailAsync(
                    settings.EmailSmtpServer,
                    settings.EmailSmtpPort,
                    settings.EmailFromAddress.Trim(),
                    password,
                    ToTB.Text.Trim(),
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
                var errorMessage = $"Error sending email: {ex.Message}";
                
                // Add more details for common Gmail errors
                if (ex.Message.Contains("5.7.9"))
                {
                    errorMessage += "\n\nThis is a Gmail app password error. Please verify:\n" +
                        "1. You're using an App-Specific Password (not your regular password)\n" +
                        "2. Two-factor authentication is enabled on your Gmail\n" +
                        "3. The password is exactly 16 characters\n" +
                        "4. The 'From Email' matches the Gmail account that created the app password";
                }
                else if (ex.Message.Contains("authentication"))
                {
                    errorMessage += "\n\nAuthentication failed. Double-check your email settings and app-specific password.";
                }
                else if (ex.InnerException != null)
                {
                    errorMessage += $"\n\nInner error: {ex.InnerException.Message}";
                }
                
                MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
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
