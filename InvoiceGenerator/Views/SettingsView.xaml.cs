using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using InvoiceGenerator.Services;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using DialogResult = System.Windows.Forms.DialogResult;

namespace InvoiceGenerator.Views
{
    public partial class SettingsView : UserControl
    {
        private readonly SettingsService _settingsService = new();
        private readonly EmailService _emailService = new();
        private readonly AuthService _authService = new();

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
            using var folderBrowser = new FolderBrowserDialog
            {
                Description = "Select the folder where invoices will be saved",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            // Set initial directory if path exists
            if (!string.IsNullOrWhiteSpace(InvoicesFolderPath.Text) && 
                System.IO.Directory.Exists(InvoicesFolderPath.Text))
            {
                folderBrowser.SelectedPath = InvoicesFolderPath.Text;
            }

            if (folderBrowser.ShowDialog() == DialogResult.OK)
            {
                InvoicesFolderPath.Text = folderBrowser.SelectedPath;
            }
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
                    FromEmail.Text.Trim(),
                    EmailPassword.Password.Trim(),
                    UseTlsCheckBox.IsChecked ?? true);

                MessageBox.Show("Email connection test successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Email connection test failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void VerifyPasswordBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var storedPassword = CredentialManager.GetPassword("InvoiceGeneratorEmail");
                
                if (string.IsNullOrEmpty(storedPassword))
                {
                    MessageBox.Show("No password found in Credential Manager.\n\nPlease enter your app-specific password and click 'Save Settings'.",
                        "Password Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    var hasWhitespace = storedPassword != storedPassword.Trim();
                    var message = $"Password found in Credential Manager.\n\n" +
                        $"Length: {storedPassword.Length} characters\n" +
                        $"Has whitespace: {(hasWhitespace ? "YES - THIS IS THE PROBLEM!" : "No")}";
                    MessageBox.Show(message,
                        "Stored Password Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error checking stored password: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                        var cleanPassword = EmailPassword.Password.Trim();
                        CredentialManager.SavePassword("InvoiceGeneratorEmail", FromEmail.Text.Trim(), cleanPassword);
                        MessageBox.Show($"Settings saved successfully!\n\nEmail password has been securely stored in Windows Credential Manager.\nPassword length: {cleanPassword.Length} characters", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception credEx)
                    {
                        MessageBox.Show($"Settings saved, but failed to save email password securely: {credEx.Message}\n\nYou'll need to re-enter the password each time.", "Partial Success", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    // Check if password already exists in credential manager
                    var existingPassword = CredentialManager.GetPassword("InvoiceGeneratorEmail");
                    if (!string.IsNullOrEmpty(existingPassword))
                    {
                        MessageBox.Show("Settings saved successfully!\n\nYour previously saved email password is still stored securely.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Settings saved successfully!\n\nNote: Email password was not provided. Please enter it to send emails.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ChangeAppPasswordBtn_Click(object sender, RoutedEventArgs e)
        {
            var newPassword = NewAppPassword.Password;
            var confirmPassword = ConfirmAppPassword.Password;

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                MessageBox.Show("New password is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPassword.Length < 8)
            {
                MessageBox.Show("New password must be at least 8 characters.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
            {
                MessageBox.Show("New passwords do not match.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var isPasswordSet = await _authService.IsPasswordSetAsync();
                if (isPasswordSet)
                {
                    if (string.IsNullOrWhiteSpace(CurrentAppPassword.Password))
                    {
                        MessageBox.Show("Current password is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var verifyResult = await _authService.VerifyPasswordWithPolicyAsync(CurrentAppPassword.Password);
                    if (verifyResult.Status == PasswordVerificationStatus.LockedOut)
                    {
                        var lockoutSeconds = Math.Max(0, (int)Math.Ceiling(verifyResult.LockoutRemaining.TotalSeconds));
                        var lockoutMinutes = lockoutSeconds / 60;
                        var lockoutRemainder = lockoutSeconds % 60;
                        MessageBox.Show($"Too many incorrect passwords. Try again in {lockoutMinutes:D2}:{lockoutRemainder:D2}.", "Temporarily Locked", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (verifyResult.Status != PasswordVerificationStatus.Success)
                    {
                        MessageBox.Show("Current password is incorrect.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                await _authService.SetPasswordAsync(newPassword);
                CurrentAppPassword.Password = string.Empty;
                NewAppPassword.Password = string.Empty;
                ConfirmAppPassword.Password = string.Empty;

                MessageBox.Show("App password updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating app password: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
