using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using InvoiceGenerator.Services;
using InvoiceGenerator.ViewModels;

namespace InvoiceGenerator.Views
{
    public partial class AppPasswordDialog : Window
    {
        private readonly AppPasswordDialogViewModel _viewModel;
        private readonly bool _isLockScreenMode;

        public AppPasswordDialog(bool isLockScreenMode = false, AuthStateCoordinator? authCoordinator = null, ISecurityLogger? logger = null)
        {
            _isLockScreenMode = isLockScreenMode;
            _viewModel = new AppPasswordDialogViewModel(
                authCoordinator ?? new AuthStateCoordinator(),
                logger,
                authService: null,
                isLockScreenMode: isLockScreenMode);

            InitializeComponent();
            DataContext = _viewModel;

            // Subscribe to VM animation events
            _viewModel.ShakeRequested += OnShakeRequested;
            _viewModel.FadeOutRequested += OnFadeOutRequested;

            // Watch DialogOutcome to set DialogResult
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            Loaded += AppPasswordDialog_Loaded;
        }

        private async void AppPasswordDialog_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitializeAsync();

            // Window-level properties the ViewModel can't set
            if (_isLockScreenMode)
            {
                ResizeMode = ResizeMode.NoResize;
                ShowInTaskbar = false;
                Title = string.Empty;
            }

            UpdatePasswordPlaceholderVisibility();
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppPasswordDialogViewModel.DialogOutcome) && _viewModel.DialogOutcome.HasValue)
            {
                DialogResult = _viewModel.DialogOutcome.Value;
                Close();
            }
        }

        // ── Submit bridge (code-behind → ViewModel command) ──────────

        private void ContinueBtn_Click(object sender, RoutedEventArgs e)
        {
            var pair = new PasswordPair
            {
                Password = GetCurrentPassword(),
                ConfirmPassword = GetConfirmPassword()
            };

            if (_viewModel.SubmitCommand.CanExecute(pair))
            {
                _viewModel.SubmitCommand.Execute(pair);
            }
        }

        // ── Animation callbacks from ViewModel events ────────────────

        private async void OnShakeRequested(object? sender, EventArgs e)
        {
            await PlayShakeAnimationAsync();
            ClearPasswordFields();
            PasswordBox.Focus();
        }

        private async void OnFadeOutRequested(object? sender, EventArgs e)
        {
            await PlayUnlockTransitionAsync();
            _viewModel.DialogOutcome = true;
        }

        // ── Primary password reveal (press-and-hold) ─────────────────

        private void ShowPrimaryPassword()
        {
            PasswordTextBox.Text = PasswordBox.Password;
            PasswordBox.Visibility = Visibility.Collapsed;
            PasswordTextBox.Visibility = Visibility.Visible;
            PasswordTextBox.Focus();
            PasswordTextBox.CaretIndex = PasswordTextBox.Text.Length;
            UpdatePasswordPlaceholderVisibility();
        }

        private void HidePrimaryPassword()
        {
            PasswordBox.Password = PasswordTextBox.Text;
            PasswordTextBox.Visibility = Visibility.Collapsed;
            PasswordBox.Visibility = Visibility.Visible;
            PasswordBox.Focus();
            UpdatePasswordPlaceholderVisibility();
        }

        private void RevealPasswordButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) => ShowPrimaryPassword();
        private void RevealPasswordButton_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e) => HidePrimaryPassword();
        private void RevealPasswordButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e) => HidePrimaryPassword();
        private void RevealPasswordButton_LostMouseCapture(object sender, System.Windows.Input.MouseEventArgs e) => HidePrimaryPassword();
        private void RevealPasswordButton_PreviewTouchDown(object sender, System.Windows.Input.TouchEventArgs e) => ShowPrimaryPassword();
        private void RevealPasswordButton_PreviewTouchUp(object sender, System.Windows.Input.TouchEventArgs e) => HidePrimaryPassword();

        // ── Confirm password reveal (press-and-hold) ─────────────────

        private void ShowConfirmPassword()
        {
            ConfirmPasswordTextBox.Text = ConfirmPasswordBox.Password;
            ConfirmPasswordBox.Visibility = Visibility.Collapsed;
            ConfirmPasswordTextBox.Visibility = Visibility.Visible;
            ConfirmPasswordTextBox.Focus();
            ConfirmPasswordTextBox.CaretIndex = ConfirmPasswordTextBox.Text.Length;
        }

        private void HideConfirmPassword()
        {
            ConfirmPasswordBox.Password = ConfirmPasswordTextBox.Text;
            ConfirmPasswordTextBox.Visibility = Visibility.Collapsed;
            ConfirmPasswordBox.Visibility = Visibility.Visible;
            ConfirmPasswordBox.Focus();
        }

        private void RevealConfirmButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) => ShowConfirmPassword();
        private void RevealConfirmButton_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e) => HideConfirmPassword();
        private void RevealConfirmButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e) => HideConfirmPassword();
        private void RevealConfirmButton_LostMouseCapture(object sender, System.Windows.Input.MouseEventArgs e) => HideConfirmPassword();
        private void RevealConfirmButton_PreviewTouchDown(object sender, System.Windows.Input.TouchEventArgs e) => ShowConfirmPassword();
        private void RevealConfirmButton_PreviewTouchUp(object sender, System.Windows.Input.TouchEventArgs e) => HideConfirmPassword();

        // ── Input helpers ────────────────────────────────────────────

        private void PasswordInput_Changed(object sender, RoutedEventArgs e) => UpdatePasswordPlaceholderVisibility();
        private void PasswordInput_FocusChanged(object sender, RoutedEventArgs e) => UpdatePasswordPlaceholderVisibility();

        private void UpdatePasswordPlaceholderVisibility()
        {
            var currentPassword = GetCurrentPassword();
            var hasInputFocus = PasswordBox.IsKeyboardFocusWithin || PasswordTextBox.IsKeyboardFocusWithin;

            PasswordPlaceholder.Visibility = string.IsNullOrEmpty(currentPassword) && !hasInputFocus
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private string GetCurrentPassword()
        {
            return PasswordTextBox.Visibility == Visibility.Visible ? PasswordTextBox.Text : PasswordBox.Password;
        }

        private string GetConfirmPassword()
        {
            var isConfirmVisible = ConfirmPasswordTextBox.Visibility == Visibility.Visible;
            return isConfirmVisible ? ConfirmPasswordTextBox.Text : ConfirmPasswordBox.Password;
        }

        private void ClearPasswordFields()
        {
            PasswordBox.Password = "";
            PasswordTextBox.Text = "";
            ConfirmPasswordBox.Password = "";
            ConfirmPasswordTextBox.Text = "";
            UpdatePasswordPlaceholderVisibility();
        }

        // ── Animations ──────────────────────────────────────────────

        private async Task PlayShakeAnimationAsync()
        {
            var storyboard = (Storyboard)Resources["ShakeStoryboard"];
            storyboard.Begin();
            await Task.Delay(600);
        }

        private async Task PlayUnlockTransitionAsync()
        {
            var storyboard = ((Storyboard)Resources["UnlockTransitionStoryboard"]).Clone();
            var completionSource = new TaskCompletionSource<bool>();

            void OnCompleted(object? sender, EventArgs args)
            {
                storyboard.Completed -= OnCompleted;
                completionSource.TrySetResult(true);
            }

            storyboard.Completed += OnCompleted;
            storyboard.Begin(this, true);
            await completionSource.Task;
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Cancel();
        }
    }
}
