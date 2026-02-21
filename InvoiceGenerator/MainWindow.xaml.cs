using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using InvoiceGenerator.Services;
using InvoiceGenerator.Views;

namespace InvoiceGenerator
{
    public partial class MainWindow : Window
    {
        private readonly AuthService _authService = new();
        private bool _isLocked;
        private bool _isVerifying;

        /// <summary>
        /// Raised after a successful unlock so the app can resume the inactivity timer.
        /// </summary>
        public event EventHandler? UnlockSucceeded;

        public MainWindow()
        {
            InitializeComponent();
        }

        // ── Navigation ───────────────────────────────────────────────

        private void ClientManagerBtn_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new ClientManagerView();
        }

        private void InvoiceBuilderBtn_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new InvoiceBuilderView();
        }

        private void HistoryBtn_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new InvoiceHistoryView();
        }

        private void SettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            ContentArea.Content = new SettingsView();
        }

        // ── Lock overlay lifecycle ───────────────────────────────────

        public void ShowLockOverlay()
        {
            _isLocked = true;
            _isVerifying = false;

            // Reset UI state
            LockPasswordBox.Password = "";
            LockPasswordTextBox.Text = "";
            LockPasswordTextBox.Visibility = Visibility.Collapsed;
            LockPasswordBox.Visibility = Visibility.Visible;
            HideLockVerifyError();

            LockSubmitBtn.IsEnabled = true;
            LockSubmitBtn.IsDefault = true;
            LockOverlay.Opacity = 1;
            LockOverlay.Visibility = Visibility.Visible;
            UpdateLockPlaceholderVisibility();

            // Focus the password field after layout
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, () =>
            {
                LockPasswordBox.Focus();
                Keyboard.Focus(LockPasswordBox);
            });
        }

        private void HideLockOverlay()
        {
            _isLocked = false;
            LockSubmitBtn.IsDefault = false;
            LockOverlay.Visibility = Visibility.Collapsed;
            LockOverlay.Opacity = 1; // reset for next lock
        }

        /// <summary>
        /// Prevent closing the window while locked (e.g. Alt+F4).
        /// </summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            if (_isLocked)
            {
                e.Cancel = true;
                return;
            }

            base.OnClosing(e);
        }

        // ── Password reveal (press-and-hold) ────────────────────────

        private void ShowLockPassword()
        {
            LockPasswordTextBox.Text = LockPasswordBox.Password;
            LockPasswordBox.Visibility = Visibility.Collapsed;
            LockPasswordTextBox.Visibility = Visibility.Visible;
            LockPasswordTextBox.Focus();
            LockPasswordTextBox.CaretIndex = LockPasswordTextBox.Text.Length;
            UpdateLockPlaceholderVisibility();
        }

        private void HideLockPassword()
        {
            LockPasswordBox.Password = LockPasswordTextBox.Text;
            LockPasswordTextBox.Visibility = Visibility.Collapsed;
            LockPasswordBox.Visibility = Visibility.Visible;
            LockPasswordBox.Focus();
            UpdateLockPlaceholderVisibility();
        }

        private void LockRevealPassword_Down(object sender, MouseButtonEventArgs e) => ShowLockPassword();
        private void LockRevealPassword_Up(object sender, MouseButtonEventArgs e) => HideLockPassword();
        private void LockRevealPassword_Leave(object sender, MouseEventArgs e) => HideLockPassword();
        private void LockRevealPassword_LostCapture(object sender, MouseEventArgs e) => HideLockPassword();
        private void LockRevealPassword_TouchDown(object sender, TouchEventArgs e) => ShowLockPassword();
        private void LockRevealPassword_TouchUp(object sender, TouchEventArgs e) => HideLockPassword();

        // ── Input helpers ────────────────────────────────────────────

        private void LockPasswordInput_Changed(object sender, RoutedEventArgs e) => UpdateLockPlaceholderVisibility();
        private void LockPasswordInput_FocusChanged(object sender, RoutedEventArgs e) => UpdateLockPlaceholderVisibility();

        private void UpdateLockPlaceholderVisibility()
        {
            var password = GetLockPassword();
            var hasFocus = LockPasswordBox.IsKeyboardFocusWithin || LockPasswordTextBox.IsKeyboardFocusWithin;

            LockPasswordPlaceholder.Visibility = string.IsNullOrEmpty(password) && !hasFocus
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private string GetLockPassword()
        {
            return LockPasswordTextBox.Visibility == Visibility.Visible
                ? LockPasswordTextBox.Text
                : LockPasswordBox.Password;
        }

        private void ClearLockPassword()
        {
            LockPasswordBox.Password = "";
            LockPasswordTextBox.Text = "";
            UpdateLockPlaceholderVisibility();
        }

        // ── Error display ────────────────────────────────────────────

        private void ShowLockVerifyError(string message)
        {
            LockVerifyErrorText.Text = message;
            LockVerifyErrorText.Visibility = Visibility.Visible;
        }

        private void HideLockVerifyError()
        {
            LockVerifyErrorText.Text = string.Empty;
            LockVerifyErrorText.Visibility = Visibility.Collapsed;
        }

        // ── Submit / verify ──────────────────────────────────────────

        private void SetLockSubmittingState(bool isSubmitting)
        {
            _isVerifying = isSubmitting;
            LockSubmitBtn.IsEnabled = !isSubmitting;
        }

        private async void LockSubmitBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isVerifying)
            {
                return;
            }

            HideLockVerifyError();
            SetLockSubmittingState(true);

            try
            {
                await HandleLockVerifyAsync();
            }
            finally
            {
                SetLockSubmittingState(false);
            }
        }

        private async Task HandleLockVerifyAsync()
        {
            var password = GetLockPassword();

            if (string.IsNullOrWhiteSpace(password))
            {
                await PlayLockShakeAsync();
                ShowLockVerifyError("Please enter your password.");
                ClearLockPassword();
                LockPasswordBox.Focus();
                return;
            }

            try
            {
                var result = await _authService.VerifyPasswordWithPolicyAsync(password);

                if (result.Status == PasswordVerificationStatus.LockedOut)
                {
                    ShowLockVerifyError($"Too many attempts. Try again in {FormatDuration(result.LockoutRemaining)}.");
                    ClearLockPassword();
                    LockPasswordBox.Focus();
                    return;
                }

                if (result.Status == PasswordVerificationStatus.InvalidPassword)
                {
                    await PlayLockShakeAsync();
                    ShowLockVerifyError("That didn't work, please try again");
                    ClearLockPassword();
                    LockPasswordBox.Focus();
                    return;
                }

                if (result.Status == PasswordVerificationStatus.PasswordNotSet)
                {
                    ShowLockVerifyError("Password is not configured.");
                    return;
                }

                // Success — fade out overlay, then notify
                HideLockVerifyError();
                await PlayLockFadeOutAsync();
                HideLockOverlay();

                try
                {
                    UnlockSucceeded?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception unlockEx)
                {
                    Debug.WriteLine($"[MainWindow] UnlockSucceeded handler threw: {unlockEx}");
                }
            }
            catch (Exception ex)
            {
                await PlayLockShakeAsync();
                ShowLockVerifyError($"Error verifying password: {ex.Message}");
            }
        }

        // ── Animations ──────────────────────────────────────────────

        private async Task PlayLockShakeAsync()
        {
            var storyboard = (Storyboard)Resources["LockShakeStoryboard"];
            storyboard.Begin();
            await Task.Delay(600);
        }

        private async Task PlayLockFadeOutAsync()
        {
            var storyboard = ((Storyboard)Resources["LockFadeOutStoryboard"]).Clone();
            var tcs = new TaskCompletionSource<bool>();

            void OnCompleted(object? sender, EventArgs args)
            {
                storyboard.Completed -= OnCompleted;
                tcs.TrySetResult(true);
            }

            storyboard.Completed += OnCompleted;
            storyboard.Begin(this, true);
            await tcs.Task;
        }

        // ── Utility ─────────────────────────────────────────────────

        private static string FormatDuration(TimeSpan duration)
        {
            var safe = duration > TimeSpan.Zero ? duration : TimeSpan.Zero;
            var totalSeconds = (int)Math.Ceiling(safe.TotalSeconds);
            var minutes = totalSeconds / 60;
            var seconds = totalSeconds % 60;
            return $"{minutes:D2}:{seconds:D2}";
        }
    }
}
