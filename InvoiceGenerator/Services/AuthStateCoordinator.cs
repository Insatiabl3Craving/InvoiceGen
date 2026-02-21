using System;
using System.Threading.Tasks;

namespace InvoiceGenerator.Services
{
    public enum AuthCoordinatorTimeoutAction
    {
        ShowLock,
        Defer,
        Skip
    }

    public enum AuthCoordinatorUnlockStatus
    {
        Success,
        EmptyPassword,
        InvalidPassword,
        LockedOut,
        PasswordNotSet,
        Error
    }

    public sealed class AuthCoordinatorUnlockResult
    {
        public AuthCoordinatorUnlockStatus Status { get; init; }
        public string Message { get; init; } = string.Empty;
        public TimeSpan LockoutRemaining { get; init; }

        public bool IsSuccess => Status == AuthCoordinatorUnlockStatus.Success;
    }

    public sealed class AuthStateCoordinator
    {
        private readonly AuthService _authService;
        private readonly ILockPolicyEvaluator _lockPolicyEvaluator;
        private readonly int _maxDeferredAttempts;
        private int _deferredAttempts;

        public AuthStateCoordinator(
            ILockPolicyEvaluator? lockPolicyEvaluator = null,
            TimeSpan? deferredRetryInterval = null,
            int maxDeferredAttempts = 20,
            AuthService? authService = null)
        {
            if (maxDeferredAttempts < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDeferredAttempts), "Maximum deferred attempts must be at least 1.");
            }

            _lockPolicyEvaluator = lockPolicyEvaluator ?? new DefaultLockPolicyEvaluator();
            _authService = authService ?? new AuthService();
            _maxDeferredAttempts = maxDeferredAttempts;
            DeferredRetryInterval = deferredRetryInterval.GetValueOrDefault(TimeSpan.FromMilliseconds(500));

            if (DeferredRetryInterval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(deferredRetryInterval), "Deferred retry interval must be greater than zero.");
            }
        }

        public bool IsLocked { get; private set; }

        public bool IsShuttingDown { get; private set; }

        public TimeSpan DeferredRetryInterval { get; }

        public AuthCoordinatorTimeoutAction HandleInactivityTimeout(AppLockStateSnapshot snapshot)
        {
            if (IsShuttingDown)
            {
                return AuthCoordinatorTimeoutAction.Skip;
            }

            var state = new AppLockStateSnapshot
            {
                IsAlreadyLocked = snapshot.IsAlreadyLocked || IsLocked,
                HasBlockingModal = snapshot.HasBlockingModal,
                IsShuttingDown = snapshot.IsShuttingDown || IsShuttingDown,
                SessionIsLocked = snapshot.SessionIsLocked,
                MainWindowReady = snapshot.MainWindowReady,
                MainWindowActive = snapshot.MainWindowActive,
                VisibleWindowCount = snapshot.VisibleWindowCount
            };

            var decision = _lockPolicyEvaluator.Evaluate(state);
            switch (decision)
            {
                case LockDecision.Allow:
                    _deferredAttempts = 0;
                    return state.IsAlreadyLocked ? AuthCoordinatorTimeoutAction.Skip : AuthCoordinatorTimeoutAction.ShowLock;
                case LockDecision.Defer:
                    if (state.IsAlreadyLocked)
                    {
                        return AuthCoordinatorTimeoutAction.Skip;
                    }

                    _deferredAttempts++;
                    if (_deferredAttempts >= _maxDeferredAttempts)
                    {
                        _deferredAttempts = 0;
                        return AuthCoordinatorTimeoutAction.ShowLock;
                    }

                    return AuthCoordinatorTimeoutAction.Defer;
                default:
                    _deferredAttempts = 0;
                    return AuthCoordinatorTimeoutAction.Skip;
            }
        }

        public void MarkLockShown()
        {
            IsLocked = true;
            _deferredAttempts = 0;
        }

        public void MarkUnlocked()
        {
            IsLocked = false;
            _deferredAttempts = 0;
        }

        public void MarkShuttingDown()
        {
            IsShuttingDown = true;
            _deferredAttempts = 0;
        }

        public async Task<AuthCoordinatorUnlockResult> TryUnlockAsync(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return new AuthCoordinatorUnlockResult
                {
                    Status = AuthCoordinatorUnlockStatus.EmptyPassword,
                    Message = "Please enter your password."
                };
            }

            try
            {
                var result = await _authService.VerifyPasswordWithPolicyAsync(password);
                return result.Status switch
                {
                    PasswordVerificationStatus.Success => new AuthCoordinatorUnlockResult
                    {
                        Status = AuthCoordinatorUnlockStatus.Success,
                        Message = string.Empty,
                        LockoutRemaining = TimeSpan.Zero
                    },
                    PasswordVerificationStatus.LockedOut => new AuthCoordinatorUnlockResult
                    {
                        Status = AuthCoordinatorUnlockStatus.LockedOut,
                        Message = "Too many attempts.",
                        LockoutRemaining = result.LockoutRemaining
                    },
                    PasswordVerificationStatus.PasswordNotSet => new AuthCoordinatorUnlockResult
                    {
                        Status = AuthCoordinatorUnlockStatus.PasswordNotSet,
                        Message = "Password is not configured.",
                        LockoutRemaining = TimeSpan.Zero
                    },
                    _ => new AuthCoordinatorUnlockResult
                    {
                        Status = AuthCoordinatorUnlockStatus.InvalidPassword,
                        Message = "That didn't work, please try again",
                        LockoutRemaining = TimeSpan.Zero
                    }
                };
            }
            catch (Exception ex)
            {
                return new AuthCoordinatorUnlockResult
                {
                    Status = AuthCoordinatorUnlockStatus.Error,
                    Message = $"Error verifying password: {ex.Message}",
                    LockoutRemaining = TimeSpan.Zero
                };
            }
        }
    }
}
