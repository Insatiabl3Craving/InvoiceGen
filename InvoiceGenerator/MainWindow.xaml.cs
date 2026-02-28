using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using InvoiceGenerator.Services;
using InvoiceGenerator.Utilities;
using InvoiceGenerator.ViewModels;
using InvoiceGenerator.Views;

namespace InvoiceGenerator
{
    public partial class MainWindow : Window
    {
        private readonly LockOverlayViewModel _viewModel;

        /// <summary>
        /// Raised after a successful unlock so the app can resume the inactivity timer.
        /// Delegates to <see cref="LockOverlayViewModel.UnlockSucceeded"/>.
        /// </summary>
        public event EventHandler? UnlockSucceeded
        {
            add => _viewModel.UnlockSucceeded += value;
            remove => _viewModel.UnlockSucceeded -= value;
        }

        public bool IsLockOverlayVisible => _viewModel.IsLocked;

        public MainWindow(AuthStateCoordinator authCoordinator, ISecurityLogger? logger = null)
        {
            _viewModel = new LockOverlayViewModel(
                authCoordinator ?? throw new ArgumentNullException(nameof(authCoordinator)),
                logger);

            InitializeComponent();

            DarkTitleBarHelper.Apply(this, IsDarkThemeActive());

            // Set DataContext for the lock overlay bindings
            LockOverlay.DataContext = _viewModel;

            // Subscribe to VM animation requests
            _viewModel.ShakeRequested += OnShakeRequested;
            _viewModel.FadeOutRequested += OnFadeOutRequested;
        }

        // ── Navigation (stays as code-behind) ────────────────────────

        private Button? _selectedNavButton;

        private void SetNavSelection(Button button)
        {
            if (_selectedNavButton != null)
                _selectedNavButton.Tag = "";

            button.Tag = "Selected";
            _selectedNavButton = button;
        }

        private void ClientManagerBtn_Click(object sender, RoutedEventArgs e)
        {
            SetNavSelection(ClientManagerBtn);
            ContentArea.Content = new ClientManagerView();
        }

        private void InvoiceBuilderBtn_Click(object sender, RoutedEventArgs e)
        {
            SetNavSelection(InvoiceBuilderBtn);
            ContentArea.Content = new InvoiceBuilderView();
        }

        private void HistoryBtn_Click(object sender, RoutedEventArgs e)
        {
            SetNavSelection(HistoryBtn);
            ContentArea.Content = new InvoiceHistoryView();
        }

        private void SettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            SetNavSelection(SettingsBtn);
            ContentArea.Content = new SettingsView();
        }

        // ── Lock overlay lifecycle ───────────────────────────────────

        public void ShowLockOverlay()
        {
            _viewModel.ShowLock();

            // Reset PasswordBox UI (not bindable)
            LockPasswordBox.Password = "";
            LockPasswordTextBox.Text = "";
            LockPasswordTextBox.Visibility = Visibility.Collapsed;
            LockPasswordBox.Visibility = Visibility.Visible;

            LockSubmitBtn.IsDefault = true;
            LockOverlay.Opacity = 1;
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
            LockSubmitBtn.IsDefault = false;
            LockOverlay.Opacity = 1; // reset for next lock
        }

        /// <summary>
        /// Prevent closing the window while locked (e.g. Alt+F4).
        /// </summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_viewModel.CanClose)
            {
                e.Cancel = true;
                return;
            }

            base.OnClosing(e);
        }

        // ── Password reveal (press-and-hold) — WPF PasswordBox limitation ──

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

        // ── Submit bridge (code-behind → ViewModel command) ──────────

        private void LockSubmitBtn_Click(object sender, RoutedEventArgs e)
        {
            var password = GetLockPassword();
            if (_viewModel.SubmitCommand.CanExecute(password))
            {
                _viewModel.SubmitCommand.Execute(password);
            }
        }

        // ── Animation callbacks from ViewModel events ────────────────

        private async void OnShakeRequested(object? sender, EventArgs e)
        {
            await PlayLockShakeAsync();
            ClearLockPassword();
            LockPasswordBox.Focus();
        }

        private async void OnFadeOutRequested(object? sender, EventArgs e)
        {
            await PlayLockFadeOutAsync();
            HideLockOverlay();
            _viewModel.CompleteFadeOut();
        }

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

        private static bool IsDarkThemeActive()
        {
            var dict = Application.Current?.Resources.MergedDictionaries.Count > 0
                ? Application.Current.Resources.MergedDictionaries[0]
                : null;

            return dict?.Source?.OriginalString.Contains("DarkTheme.xaml", StringComparison.OrdinalIgnoreCase) == true;
        }
    }
}
