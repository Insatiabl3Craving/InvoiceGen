using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using InvoiceGenerator.Services;

namespace InvoiceGenerator.Views
{
    public partial class SettingsView : UserControl
    {
        private readonly SettingsService _settingsService = new();
        private readonly EmailService _emailService = new();

        public SettingsView()
        {
            InitializeComponent();
            LoadSettings();
        }

        private async void LoadSettings()
        {
            try
            {
                var settings = await _settingsService.GetSettingsAsync();
                TemplatePath.Text = settings.TemplateFilePath;
                InvoicesFolderPath.Text = settings.InvoicesFolderPath;
                SmtpServer.Text = settings.EmailSmtpServer;
                SmtpPort.Text = settings.EmailSmtpPort.ToString();
                FromEmail.Text = settings.EmailFromAddress;
                UseTlsCheckBox.IsChecked = settings.EmailUseTls;
                DefaultSubject.Text = settings.EmailDefaultSubject;
                DefaultBody.Text = settings.EmailDefaultBody;

                // Try to get email password from Credential Manager
                var password = CredentialManager.GetPassword("InvoiceGeneratorEmail");
                if (!string.IsNullOrEmpty(password))
                {
                    EmailPassword.Password = password;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BrowseTemplateBtn_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Word Files (*.docx)|*.docx|All files (*.*)|*.*",
                Title = "Select Word Template"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                TemplatePath.Text = openFileDialog.FileName;
            }
        }

        private void BrowseFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            // For simplicity, show a message with instructions
            MessageBox.Show(
                "Please enter the full path to your invoices folder.\n\n" +
                "Example: C:\\Users\\YourName\\Documents\\Invoices",
                "Select Invoices Folder",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            
            // Focus on the text box for user input
            InvoicesFolderPath.Focus();
        }

        private async void TestEmailBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SmtpServer.Text) || 
                string.IsNullOrWhiteSpace(FromEmail.Text) ||
                string.IsNullOrWhiteSpace(EmailPassword.Password))
            {
                MessageBox.Show("Please fill in all email settings.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (!int.TryParse(SmtpPort.Text, out var port))
                {
                    MessageBox.Show("Invalid SMTP port number.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await _emailService.TestSmtpConnectionAsync(
                    SmtpServer.Text,
                    port,
                    FromEmail.Text,
                    EmailPassword.Password,
                    UseTlsCheckBox.IsChecked ?? true);

                MessageBox.Show("Email connection test successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Email connection test failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TemplatePath.Text))
            {
                MessageBox.Show("Template path is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(InvoicesFolderPath.Text))
            {
                MessageBox.Show("Invoices folder path is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var settings = await _settingsService.GetSettingsAsync();
                settings.TemplateFilePath = TemplatePath.Text;
                settings.InvoicesFolderPath = InvoicesFolderPath.Text;
                settings.EmailSmtpServer = SmtpServer.Text;
                settings.EmailSmtpPort = int.TryParse(SmtpPort.Text, out var port) ? port : 587;
                settings.EmailFromAddress = FromEmail.Text;
                settings.EmailUseTls = UseTlsCheckBox.IsChecked ?? true;
                settings.EmailDefaultSubject = DefaultSubject.Text;
                settings.EmailDefaultBody = DefaultBody.Text;

                await _settingsService.UpdateSettingsAsync(settings);

                // Save email password to Credential Manager
                if (!string.IsNullOrEmpty(EmailPassword.Password))
                {
                    try
                    {
                        CredentialManager.SavePassword("InvoiceGeneratorEmail", FromEmail.Text, EmailPassword.Password);
                    }
                    catch
                    {
                        // If saving to credential manager fails, just continue
                    }
                }

                MessageBox.Show("Settings saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
