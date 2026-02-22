using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvoiceGenerator.Services;

namespace InvoiceGenerator.ViewModels
{
    /// <summary>
    /// ViewModel for the in-window lock overlay in <see cref="MainWindow"/>.
    /// Owns all lock/unlock decision logic; the view handles only animations,
    /// PasswordBox bridging, and focus management.
    /// </summary>
    public partial class LockOverlayViewModel : ViewModelBase
    {
        private readonly AuthStateCoordinator _authCoordinator;
        private readonly ISecurityLogger _logger;

        [ObservableProperty]
        private bool _isLocked;

        [ObservableProperty]
        private bool _isVerifying;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private bool _hasError;

        /// <summary>
        /// The view subscribes to <see cref="ShakeRequested"/> to play
        /// the shake storyboard imperatively (targeted named-element
        /// storyboards cannot be driven from a DataTrigger reliably).
        /// </summary>
        public event EventHandler? ShakeRequested;

        /// <summary>
        /// The view subscribes to <see cref="FadeOutRequested"/> to play
        /// the fade-out storyboard, then calls <see cref="CompleteFadeOut"/>.
        /// </summary>
        public event EventHandler? FadeOutRequested;

        /// <summary>
        /// Raised after a successful unlock so the app can resume the
        /// inactivity timer. Equivalent to the old code-behind event.
        /// </summary>
        public event EventHandler? UnlockSucceeded;

        public LockOverlayViewModel(AuthStateCoordinator authCoordinator, ISecurityLogger? logger = null)
        {
            _authCoordinator = authCoordinator ?? throw new ArgumentNullException(nameof(authCoordinator));
            _logger = logger ?? NullSecurityLogger.Instance;
        }

        /// <summary>Whether the window may be closed (false while locked).</summary>
        public bool CanClose => !IsLocked;

        // ── Public lifecycle methods ─────────────────────────────────

        /// <summary>
        /// Called by <see cref="MainWindow.ShowLockOverlay"/> to activate
        /// the lock overlay. Resets all transient state.
        /// </summary>
        public void ShowLock()
        {
            ErrorMessage = null;
            HasError = false;
            IsVerifying = false;
            IsLocked = true;
        }

        /// <summary>
        /// Called by the view after the fade-out animation completes.
        /// Finalises the unlock: hides the overlay and raises <see cref="UnlockSucceeded"/>.
        /// </summary>
        public void CompleteFadeOut()
        {
            IsLocked = false;

            try
            {
                UnlockSucceeded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.HandlerException("UnlockSucceeded", ex, _authCoordinator.CurrentLockCycleId);
            }
        }

        // ── Submit command ───────────────────────────────────────────

        /// <summary>
        /// The view calls this from code-behind after reading the PasswordBox
        /// (WPF PasswordBox.Password is not bindable for security reasons).
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSubmit))]
        private async Task SubmitAsync(string? password)
        {
            if (IsVerifying) return;

            ClearError();
            IsVerifying = true;
            SubmitCommand.NotifyCanExecuteChanged();

            try
            {
                await ExecuteUnlockAsync(password);
            }
            finally
            {
                IsVerifying = false;
                SubmitCommand.NotifyCanExecuteChanged();
            }
        }

        private bool CanSubmit() => !IsVerifying;

        // ── Core unlock logic (moved from MainWindow.HandleLockVerifyAsync) ──

        private async Task ExecuteUnlockAsync(string? password)
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

                // Success — request fade-out; CompleteFadeOut() finalises.
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
}
