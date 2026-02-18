using System;
using System.Threading.Tasks;
using System.Windows;
using InvoiceGenerator.Services;

namespace InvoiceGenerator.Views
{
    public partial class AppPasswordDialog : Window
    {
        private readonly AuthService _authService = new();
        private bool _isSetupMode;

        public AppPasswordDialog()
        {
            InitializeComponent();
            Loaded += AppPasswordDialog_Loaded;
        }

        private async void AppPasswordDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await ConfigureModeAsync();
            PasswordBox.Focus();
        }

        private async Task ConfigureModeAsync()
        {
            _isSetupMode = !await _authService.IsPasswordSetAsync();

            if (_isSetupMode)
            {
                HeaderText.Text = "Set App Password";
                SubText.Text = "Create a password to protect this app.";
                ConfirmPanel.Visibility = Visibility.Visible;
                HintText.Text = "Choose a password you will remember. Minimum 8 characters.";
                ContinueBtn.Content = "Set Password";
                CancelBtn.Content = "Exit";
            }
            else
            {
                HeaderText.Text = "Unlock Invoice Generator";
                SubText.Text = "Enter the app password to continue.";
                ConfirmPanel.Visibility = Visibility.Collapsed;
                HintText.Text = "";
                ContinueBtn.Content = "Continue";
                CancelBtn.Content = "Exit";
            }
        }

        private async void ContinueBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isSetupMode)
            {
                await HandleSetupAsync();
                return;
            }

            await HandleVerifyAsync();
        }

        private async Task HandleSetupAsync()
        {
            var password = PasswordBox.Password;
            var confirm = ConfirmPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Password is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (password.Length < 8)
            {
                MessageBox.Show("Password must be at least 8 characters.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.Equals(password, confirm, StringComparison.Ordinal))
            {
                MessageBox.Show("Passwords do not match.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                await _authService.SetPasswordAsync(password);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting password: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task HandleVerifyAsync()
        {
            if (string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                MessageBox.Show("Password is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var isValid = await _authService.VerifyPasswordAsync(PasswordBox.Password);
                if (!isValid)
                {
                    MessageBox.Show("Incorrect password.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error verifying password: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
