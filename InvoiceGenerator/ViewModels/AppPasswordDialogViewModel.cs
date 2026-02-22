using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvoiceGenerator.Services;

namespace InvoiceGenerator.ViewModels
{
    /// <summary>
    /// ViewModel for the startup password dialog (setup + verify modes).
    /// Owns all auth decision logic; the view handles animations,
    /// PasswordBox bridging, and DialogResult management.
    /// </summary>
    public partial class AppPasswordDialogViewModel : ViewModelBase
    {
        private readonly AuthStateCoordinator _authCoordinator;
        private readonly AuthService _authService;
        private readonly ISecurityLogger _logger;
        private readonly bool _isLockScreenMode;

        [ObservableProperty]
        private bool _isSetupMode;

        [ObservableProperty]
        private bool _isVerifying;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private bool _hasError;

        [ObservableProperty]
        private string _hintText = string.Empty;

        [ObservableProperty]
        private string _submitTooltip = "Submit";

        [ObservableProperty]
        private bool _isConfirmPanelVisible;

        /// <summary>
        /// Outcome of the dialog: <c>true</c> = success, <c>false</c> = cancelled,
        /// <c>null</c> = still pending. The view watches this to set <c>DialogResult</c>.
        /// </summary>
        [ObservableProperty]
        private bool? _dialogOutcome;

        /// <summary>Raised when the view should play the shake animation.</summary>
        public event EventHandler? ShakeRequested;

        /// <summary>Raised when the view should play the unlock/fade-out transition.</summary>
        public event EventHandler? FadeOutRequested;

        public AppPasswordDialogViewModel(
            AuthStateCoordinator authCoordinator,
            ISecurityLogger? logger = null,
            AuthService? authService = null,
            bool isLockScreenMode = false)
        {
            _authCoordinator = authCoordinator ?? throw new ArgumentNullException(nameof(authCoordinator));
            _logger = logger ?? NullSecurityLogger.Instance;
            _authService = authService ?? new AuthService();
            _isLockScreenMode = isLockScreenMode;
        }

        // ── Initialisation (called from Loaded) ─────────────────────

        /// <summary>
        /// Determines whether the dialog is in setup or verify mode.
        /// Must be called from the view's <c>Loaded</c> event.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isLockScreenMode)
            {
                IsSetupMode = false;
                IsConfirmPanelVisible = false;
                HintText = "Session locked after inactivity. Enter your password to continue.";
                SubmitTooltip = "Unlock";
                return;
            }

            IsSetupMode = !await _authService.IsPasswordSetAsync();

            if (IsSetupMode)
            {
                IsConfirmPanelVisible = true;
                HintText = "Choose a password you will remember. Minimum 8 characters.";
                SubmitTooltip = "Set Password";
            }
            else
            {
                IsConfirmPanelVisible = false;
                HintText = string.Empty;
                SubmitTooltip = "Continue";
            }
        }

        // ── Submit command ───────────────────────────────────────────

        /// <summary>
        /// The view calls this from code-behind, passing the password (and
        /// optional confirm password) read from PasswordBox controls.
        /// Tuple parameter: (password, confirmPassword).
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSubmit))]
        private async Task SubmitAsync(PasswordPair? passwords)
        {
            if (IsVerifying) return;

            var password = passwords?.Password ?? string.Empty;
            var confirm = passwords?.ConfirmPassword ?? string.Empty;

            ClearError();
            IsVerifying = true;
            SubmitCommand.NotifyCanExecuteChanged();

            try
            {
                if (IsSetupMode)
                {
                    await ExecuteSetupAsync(password, confirm);
                }
                else
                {
                    await ExecuteVerifyAsync(password);
                }
            }
            finally
            {
                IsVerifying = false;
                SubmitCommand.NotifyCanExecuteChanged();
            }
        }

        private bool CanSubmit() => !IsVerifying;

        /// <summary>
        /// Cancel the dialog.
        /// </summary>
        public void Cancel()
        {
            DialogOutcome = false;
        }

        // ── Setup mode logic (moved from AppPasswordDialog.HandleSetupAsync) ──

        private async Task ExecuteSetupAsync(string password, string confirm)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                RequestShake();
                SetError("Please enter a password.");
                return;
            }

            if (password.Length < 8)
            {
                RequestShake();
                SetError("Password must be at least 8 characters.");
                return;
            }

            if (!string.Equals(password, confirm, StringComparison.Ordinal))
            {
                RequestShake();
                SetError("Passwords do not match.");
                return;
            }

            try
            {
                await _authService.SetPasswordAsync(password);
                _logger.AppStartupAuth("setup", true);
                ClearError();
                FadeOutRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                RequestShake();
                SetError($"Error setting password: {ex.Message}");
            }
        }

        // ── Verify mode logic (moved from AppPasswordDialog.HandleVerifyAsync) ──

        private async Task ExecuteVerifyAsync(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                RequestShake();
                SetError("Please enter your password.");
                return;
            }

            try
            {
                var result = await _authCoordinator.TryUnlockAsync(password);

                switch (result.Status)
                {
                    case AuthCoordinatorUnlockStatus.LockedOut:
                        SetError($"Too many attempts. Try again in {FormatDuration(result.LockoutRemaining)}.");
                        return;

                    case AuthCoordinatorUnlockStatus.InvalidPassword:
                        RequestShake();
                        SetError(result.Message);
                        return;

                    case AuthCoordinatorUnlockStatus.EmptyPassword:
                        RequestShake();
                        SetError(result.Message);
                        return;

                    case AuthCoordinatorUnlockStatus.PasswordNotSet:
                        SetError("Password is not configured.");
                        return;

                    case AuthCoordinatorUnlockStatus.Error:
                        RequestShake();
                        SetError(result.Message);
                        return;

                    case AuthCoordinatorUnlockStatus.Success:
                        break;

                    default:
                        RequestShake();
                        SetError(result.Message);
                        return;
                }

                _logger.AppStartupAuth("verify", true);
                ClearError();
                FadeOutRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                RequestShake();
                SetError($"Error verifying password: {ex.Message}");
            }
        }

        // ── Helpers ──────────────────────────────────────────────────

        private void SetError(string message)
        {
            ErrorMessage = message;
            HasError = true;
        }

        private void ClearError()
        {
            ErrorMessage = null;
            HasError = false;
        }

        private void RequestShake()
        {
            ShakeRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Simple DTO to pass password + confirm password from the view
    /// to the ViewModel's submit command (since PasswordBox.Password
    /// is not bindable).
    /// </summary>
    public sealed class PasswordPair
    {
        public string Password { get; init; } = string.Empty;
        public string? ConfirmPassword { get; init; }
    }
}
