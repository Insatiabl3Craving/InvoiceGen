using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using InvoiceGenerator.Services;

namespace InvoiceGenerator.Views
{
    public partial class AppPasswordDialog : Window
    {
        private readonly AuthService _authService = new();
        private bool _isSetupMode;
        private bool _isPasswordVisible = false;

        public AppPasswordDialog()
        {
            InitializeComponent();
            Loaded += AppPasswordDialog_Loaded;
        }

        private async void AppPasswordDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await ConfigureModeAsync();
            UpdatePasswordPlaceholderVisibility();
        }

        private async Task ConfigureModeAsync()
        {
            _isSetupMode = !await _authService.IsPasswordSetAsync();

            if (_isSetupMode)
            {
                ConfirmPanel.Visibility = Visibility.Visible;
                HintText.Text = "Choose a password you will remember. Minimum 8 characters.";
                ContinueBtn.Content = "Set Password";
                CancelBtn.Content = "Exit";
            }
            else
            {
                ConfirmPanel.Visibility = Visibility.Collapsed;
                HintText.Text = "";
                ContinueBtn.Content = "Continue";
                CancelBtn.Content = "Exit";
            }
        }

        /// <summary>
        /// Toggle password visibility for the main password field
        /// </summary>
        private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                // Show password as plain text
                PasswordTextBox.Text = PasswordBox.Password;
                PasswordBox.Visibility = Visibility.Collapsed;
                PasswordTextBox.Visibility = Visibility.Visible;
                PasswordTextBox.Focus();
                PasswordTextBox.CaretIndex = PasswordTextBox.Text.Length;
            }
            else
            {
                // Hide password
                PasswordBox.Password = PasswordTextBox.Text;
                PasswordTextBox.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Visible;
                PasswordBox.Focus();
            }

            UpdatePasswordPlaceholderVisibility();
        }

        private void PasswordInput_Changed(object sender, RoutedEventArgs e)
        {
            UpdatePasswordPlaceholderVisibility();
        }

        private void PasswordInput_FocusChanged(object sender, RoutedEventArgs e)
        {
            UpdatePasswordPlaceholderVisibility();
        }

        private void UpdatePasswordPlaceholderVisibility()
        {
            var currentPassword = GetCurrentPassword();
            var hasInputFocus = PasswordBox.IsKeyboardFocusWithin || PasswordTextBox.IsKeyboardFocusWithin;

            PasswordPlaceholder.Visibility = string.IsNullOrEmpty(currentPassword) && !hasInputFocus
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        /// <summary>
        /// Toggle password visibility for the confirm password field
        /// </summary>
        private void ToggleConfirmVisibility_Click(object sender, RoutedEventArgs e)
        {
            bool isConfirmVisible = ConfirmPasswordTextBox.Visibility == Visibility.Visible;

            if (!isConfirmVisible)
            {
                // Show password as plain text
                ConfirmPasswordTextBox.Text = ConfirmPasswordBox.Password;
                ConfirmPasswordBox.Visibility = Visibility.Collapsed;
                ConfirmPasswordTextBox.Visibility = Visibility.Visible;
                ConfirmPasswordTextBox.Focus();
                ConfirmPasswordTextBox.CaretIndex = ConfirmPasswordTextBox.Text.Length;
            }
            else
            {
                // Hide password
                ConfirmPasswordBox.Password = ConfirmPasswordTextBox.Text;
                ConfirmPasswordTextBox.Visibility = Visibility.Collapsed;
                ConfirmPasswordBox.Visibility = Visibility.Visible;
                ConfirmPasswordBox.Focus();
            }
        }

        /// <summary>
        /// Get the current password value, handling both visible and hidden states
        /// </summary>
        private string GetCurrentPassword()
        {
            return _isPasswordVisible ? PasswordTextBox.Text : PasswordBox.Password;
        }

        /// <summary>
        /// Get the confirm password value, handling both visible and hidden states
        /// </summary>
        private string GetConfirmPassword()
        {
            var isConfirmVisible = ConfirmPasswordTextBox.Visibility == Visibility.Visible;
            return isConfirmVisible ? ConfirmPasswordTextBox.Text : ConfirmPasswordBox.Password;
        }

        /// <summary>
        /// Play the shake animation to indicate an error
        /// </summary>
        private async Task PlayShakeAnimationAsync()
        {
            var storyboard = (Storyboard)Resources["ShakeStoryboard"];
            storyboard.Begin();
            await Task.Delay(600); // Wait for animation to complete
        }

        /// <summary>
        /// Play the curtain animation for successful unlock
        /// </summary>
        private async Task PlayCurtainAnimationAsync()
        {
            var halfWidth = Math.Max(ActualWidth / 2d, 1d);
            var windowHeight = Math.Max(ActualHeight, 1d);

            LeftCurtain.Width = halfWidth;
            RightCurtain.Width = halfWidth;
            LeftCurtain.Height = windowHeight;
            RightCurtain.Height = windowHeight;

            CurtainCanvas.Visibility = Visibility.Visible;
            CurtainCanvas.Opacity = 1;
            System.Windows.Controls.Canvas.SetLeft(LeftCurtain, 0);
            System.Windows.Controls.Canvas.SetLeft(RightCurtain, halfWidth);

            var storyboard = (Storyboard)Resources["CurtainStoryboard"];

            foreach (var timeline in storyboard.Children)
            {
                if (timeline is not DoubleAnimation animation)
                {
                    continue;
                }

                var targetName = Storyboard.GetTargetName(animation);
                if (string.Equals(targetName, "LeftCurtain", StringComparison.Ordinal))
                {
                    animation.To = -halfWidth - 40;
                }
                else if (string.Equals(targetName, "RightCurtain", StringComparison.Ordinal))
                {
                    animation.To = ActualWidth + 40;
                }
            }

            storyboard.Begin();
            await Task.Delay(800); // Wait for animation to complete

            CurtainCanvas.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Clear both password fields
        /// </summary>
        private void ClearPasswordFields()
        {
            PasswordBox.Password = "";
            PasswordTextBox.Text = "";
            ConfirmPasswordBox.Password = "";
            ConfirmPasswordTextBox.Text = "";
            UpdatePasswordPlaceholderVisibility();
        }

        private void ShowVerifyError(string message)
        {
            VerifyErrorText.Text = message;
            VerifyErrorText.Visibility = Visibility.Visible;
        }

        private void HideVerifyError()
        {
            VerifyErrorText.Text = string.Empty;
            VerifyErrorText.Visibility = Visibility.Collapsed;
        }

        private async void ContinueBtn_Click(object sender, RoutedEventArgs e)
        {
            HideVerifyError();

            if (_isSetupMode)
            {
                await HandleSetupAsync();
                return;
            }

            await HandleVerifyAsync();
        }

        private async Task HandleSetupAsync()
        {
            var password = GetCurrentPassword();
            var confirm = GetConfirmPassword();

            if (string.IsNullOrWhiteSpace(password))
            {
                await PlayShakeAnimationAsync();
                MessageBox.Show("Password is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                ClearPasswordFields();
                PasswordBox.Focus();
                return;
            }

            if (password.Length < 8)
            {
                await PlayShakeAnimationAsync();
                MessageBox.Show("Password must be at least 8 characters.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                ClearPasswordFields();
                PasswordBox.Focus();
                return;
            }

            if (!string.Equals(password, confirm, StringComparison.Ordinal))
            {
                await PlayShakeAnimationAsync();
                MessageBox.Show("Passwords do not match.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                ClearPasswordFields();
                PasswordBox.Focus();
                return;
            }

            try
            {
                await _authService.SetPasswordAsync(password);
                await PlayCurtainAnimationAsync();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                await PlayShakeAnimationAsync();
                MessageBox.Show($"Error setting password: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task HandleVerifyAsync()
        {
            if (string.IsNullOrWhiteSpace(GetCurrentPassword()))
            {
                await PlayShakeAnimationAsync();
                MessageBox.Show("Password is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                ClearPasswordFields();
                PasswordBox.Focus();
                return;
            }

            try
            {
                var isValid = await _authService.VerifyPasswordAsync(GetCurrentPassword());
                if (!isValid)
                {
                    await PlayShakeAnimationAsync();
                    ShowVerifyError("That didn't work, please try again");
                    ClearPasswordFields();
                    PasswordBox.Focus();
                    return;
                }

                HideVerifyError();
                await PlayCurtainAnimationAsync();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                await PlayShakeAnimationAsync();
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
