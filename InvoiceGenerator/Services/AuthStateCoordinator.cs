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
        private readonly ISecurityLogger _logger;
        private readonly int _maxDeferredAttempts;
        private int _deferredAttempts;

        public AuthStateCoordinator(
            ILockPolicyEvaluator? lockPolicyEvaluator = null,
            TimeSpan? deferredRetryInterval = null,
            int maxDeferredAttempts = 20,
            AuthService? authService = null,
            ISecurityLogger? logger = null)
        {
            if (maxDeferredAttempts < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDeferredAttempts), "Maximum deferred attempts must be at least 1.");
            }

            _lockPolicyEvaluator = lockPolicyEvaluator ?? new DefaultLockPolicyEvaluator();
            _authService = authService ?? new AuthService();
            _logger = logger ?? NullSecurityLogger.Instance;
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

        /// <summary>
        /// Correlation ID for the current lock→unlock cycle. Set when a
        /// lock is triggered, cleared when unlock succeeds.
        /// </summary>
        public Guid? CurrentLockCycleId { get; private set; }

        public AuthCoordinatorTimeoutAction HandleInactivityTimeout(AppLockStateSnapshot snapshot)
        {
            if (IsShuttingDown)
            {
                _logger.LockPolicyDecision(CurrentLockCycleId, "Skip(ShuttingDown)",
                    snapshot.IsAlreadyLocked, snapshot.HasBlockingModal, snapshot.IsShuttingDown,
                    snapshot.SessionIsLocked, snapshot.MainWindowReady, snapshot.MainWindowActive,
                    snapshot.VisibleWindowCount);
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
            AuthCoordinatorTimeoutAction action;
            switch (decision)
            {
                case LockDecision.Allow:
                    _deferredAttempts = 0;
                    if (state.IsAlreadyLocked)
                    {
                        action = AuthCoordinatorTimeoutAction.Skip;
                    }
                    else
                    {
                        CurrentLockCycleId = Guid.NewGuid();
                        action = AuthCoordinatorTimeoutAction.ShowLock;
                    }
                    break;
                case LockDecision.Defer:
                    if (state.IsAlreadyLocked)
                    {
                        action = AuthCoordinatorTimeoutAction.Skip;
                    }
                    else
                    {
                        _deferredAttempts++;
                        if (_deferredAttempts >= _maxDeferredAttempts)
                        {
                            _deferredAttempts = 0;
                            CurrentLockCycleId = Guid.NewGuid();
                            action = AuthCoordinatorTimeoutAction.ShowLock;
                        }
                        else
                        {
                            action = AuthCoordinatorTimeoutAction.Defer;
                            _logger.DeferredRetryScheduled(CurrentLockCycleId, _deferredAttempts, _maxDeferredAttempts);
                        }
                    }
                    break;
                default:
                    _deferredAttempts = 0;
                    action = AuthCoordinatorTimeoutAction.Skip;
                    break;
            }

            _logger.LockPolicyDecision(CurrentLockCycleId, action.ToString(),
                state.IsAlreadyLocked, state.HasBlockingModal, state.IsShuttingDown,
                state.SessionIsLocked, state.MainWindowReady, state.MainWindowActive,
                state.VisibleWindowCount);

            if (action == AuthCoordinatorTimeoutAction.ShowLock)
            {
                _logger.InactivityTimeoutFired(CurrentLockCycleId!.Value, TimeSpan.Zero);
            }

            return action;
        }

        public void MarkLockShown()
        {
            IsLocked = true;
            _deferredAttempts = 0;
            _logger.LockScreenShown(CurrentLockCycleId);
        }

        public void MarkUnlocked()
        {
            var cycleId = CurrentLockCycleId;
            IsLocked = false;
            _deferredAttempts = 0;
            CurrentLockCycleId = null;
            _logger.UnlockSuccess(cycleId, Guid.Empty);
        }

        public void MarkShuttingDown()
        {
            IsShuttingDown = true;
            _deferredAttempts = 0;
            _logger.AppShutdown();
        }

        public async Task<AuthCoordinatorUnlockResult> TryUnlockAsync(string password)
        {
            var attemptId = Guid.NewGuid();

            if (string.IsNullOrWhiteSpace(password))
            {
                _logger.UnlockFailed(CurrentLockCycleId, attemptId, "EmptyPassword", 0);
                return new AuthCoordinatorUnlockResult
                {
                    Status = AuthCoordinatorUnlockStatus.EmptyPassword,
                    Message = "Please enter your password."
                };
            }

            _logger.UnlockAttemptStarted(CurrentLockCycleId, attemptId);

            try
            {
                var result = await _authService.VerifyPasswordWithPolicyAsync(password);
                AuthCoordinatorUnlockResult coordResult;

                switch (result.Status)
                {
                    case PasswordVerificationStatus.Success:
                        _logger.UnlockSuccess(CurrentLockCycleId, attemptId);
                        coordResult = new AuthCoordinatorUnlockResult
                        {
                            Status = AuthCoordinatorUnlockStatus.Success,
                            Message = string.Empty,
                            LockoutRemaining = TimeSpan.Zero
                        };
                        break;

                    case PasswordVerificationStatus.LockedOut:
                        _logger.LockoutActiveRejection(CurrentLockCycleId, attemptId, result.LockoutRemaining);
                        coordResult = new AuthCoordinatorUnlockResult
                        {
                            Status = AuthCoordinatorUnlockStatus.LockedOut,
                            Message = "Too many attempts.",
                            LockoutRemaining = result.LockoutRemaining
                        };
                        break;

                    case PasswordVerificationStatus.PasswordNotSet:
                        _logger.UnlockFailed(CurrentLockCycleId, attemptId, "PasswordNotSet", 0);
                        coordResult = new AuthCoordinatorUnlockResult
                        {
                            Status = AuthCoordinatorUnlockStatus.PasswordNotSet,
                            Message = "Password is not configured.",
                            LockoutRemaining = TimeSpan.Zero
                        };
                        break;

                    default:
                        _logger.UnlockFailed(CurrentLockCycleId, attemptId,
                            "InvalidPassword", result.FailedAttempts);
                        coordResult = new AuthCoordinatorUnlockResult
                        {
                            Status = AuthCoordinatorUnlockStatus.InvalidPassword,
                            Message = "That didn't work, please try again",
                            LockoutRemaining = TimeSpan.Zero
                        };
                        break;
                }

                return coordResult;
            }
            catch (Exception ex)
            {
                _logger.HandlerException("TryUnlockAsync", ex, CurrentLockCycleId);
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
