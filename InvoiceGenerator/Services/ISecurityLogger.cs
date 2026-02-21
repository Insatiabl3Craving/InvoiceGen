using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace InvoiceGenerator.Services
{
    // ── Event taxonomy ───────────────────────────────────────────

    public enum SecurityEventType
    {
        InactivityTimeoutFired,
        LockPolicyDecision,
        LockScreenShown,
        UnlockAttemptStarted,
        UnlockSuccess,
        UnlockFailed,
        LockoutStarted,
        LockoutExpired,
        LockoutActiveRejection,
        PasswordSet,
        AppStartupAuth,
        AppShutdown,
        DeferredRetryScheduled,
        HandlerException
    }

    // ── Log entry ────────────────────────────────────────────────

    public sealed class SecurityLogEntry
    {
        public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
        public SecurityEventType EventType { get; init; }
        public Guid? LockCycleId { get; init; }
        public Guid? AttemptId { get; init; }
        public string? Message { get; init; }
        public Dictionary<string, object?>? Properties { get; init; }
    }

    // ── Interface ────────────────────────────────────────────────

    public interface ISecurityLogger
    {
        void Log(SecurityLogEntry entry);
    }

    // ── Null implementation (safe default) ───────────────────────

    public sealed class NullSecurityLogger : ISecurityLogger
    {
        public static NullSecurityLogger Instance { get; } = new();
        public void Log(SecurityLogEntry entry) { }
    }

    // ── Typed convenience extensions ─────────────────────────────

    public static class SecurityLoggerExtensions
    {
        public static void InactivityTimeoutFired(
            this ISecurityLogger logger, Guid lockCycleId, TimeSpan elapsed)
        {
            logger.Log(new SecurityLogEntry
            {
                EventType = SecurityEventType.InactivityTimeoutFired,
                LockCycleId = lockCycleId,
                Message = "Inactivity timeout elapsed.",
                Properties = new Dictionary<string, object?>
                {
                    ["ElapsedSeconds"] = Math.Round(elapsed.TotalSeconds, 2)
                }
            });
        }

        public static void LockPolicyDecision(
            this ISecurityLogger logger,
            Guid? lockCycleId,
            string decision,
            bool isAlreadyLocked,
            bool hasBlockingModal,
            bool isShuttingDown,
            bool sessionIsLocked,
            bool mainWindowReady,
            bool mainWindowActive,
            int visibleWindowCount)
        {
            logger.Log(new SecurityLogEntry
            {
                EventType = SecurityEventType.LockPolicyDecision,
                LockCycleId = lockCycleId,
                Message = $"Lock policy evaluated: {decision}.",
                Properties = new Dictionary<string, object?>
                {
                    ["Decision"] = decision,
                    ["IsAlreadyLocked"] = isAlreadyLocked,
                    ["HasBlockingModal"] = hasBlockingModal,
                    ["IsShuttingDown"] = isShuttingDown,
                    ["SessionIsLocked"] = sessionIsLocked,
                    ["MainWindowReady"] = mainWindowReady,
                    ["MainWindowActive"] = mainWindowActive,
                    ["VisibleWindowCount"] = visibleWindowCount
                }
            });
        }

        public static void LockScreenShown(this ISecurityLogger logger, Guid? lockCycleId)
        {
            logger.Log(new SecurityLogEntry
            {
                EventType = SecurityEventType.LockScreenShown,
                LockCycleId = lockCycleId,
                Message = "Lock overlay displayed."
            });
        }

        public static void UnlockAttemptStarted(
            this ISecurityLogger logger, Guid? lockCycleId, Guid attemptId)
        {
            logger.Log(new SecurityLogEntry
            {
                EventType = SecurityEventType.UnlockAttemptStarted,
                LockCycleId = lockCycleId,
                AttemptId = attemptId,
                Message = "Unlock attempt started."
            });
        }

        public static void UnlockSuccess(
            this ISecurityLogger logger, Guid? lockCycleId, Guid attemptId)
        {
            logger.Log(new SecurityLogEntry
            {
                EventType = SecurityEventType.UnlockSuccess,
                LockCycleId = lockCycleId,
                AttemptId = attemptId,
                Message = "Unlock succeeded."
            });
        }

        public static void UnlockFailed(
            this ISecurityLogger logger,
            Guid? lockCycleId,
            Guid attemptId,
            string status,
            int failedAttempts)
        {
            logger.Log(new SecurityLogEntry
            {
                EventType = SecurityEventType.UnlockFailed,
                LockCycleId = lockCycleId,
                AttemptId = attemptId,
                Message = $"Unlock failed: {status}.",
                Properties = new Dictionary<string, object?>
                {
                    ["Status"] = status,
                    ["FailedAttempts"] = failedAttempts
                }
            });
        }

        public static void LockoutStarted(
            this ISecurityLogger logger,
            Guid? lockCycleId,
            int failedAttempts,
            TimeSpan lockoutDuration)
        {
            logger.Log(new SecurityLogEntry
            {
                EventType = SecurityEventType.LockoutStarted,
                LockCycleId = lockCycleId,
                Message = $"Account locked out after {failedAttempts} failed attempts.",
                Properties = new Dictionary<string, object?>
                {
                    ["FailedAttempts"] = failedAttempts,
                    ["LockoutDurationSeconds"] = Math.Round(lockoutDuration.TotalSeconds, 0)
                }
            });
        }

        public static void LockoutExpired(
            this ISecurityLogger logger, Guid? lockCycleId, int previousFailedAttempts)
        {
            logger.Log(new SecurityLogEntry
            {
                EventType = SecurityEventType.LockoutExpired,
                LockCycleId = lockCycleId,
                Message = "Lockout period expired; failed attempts reset.",
                Properties = new Dictionary<string, object?>
                {
                    ["PreviousFailedAttempts"] = previousFailedAttempts
                }
            });
        }

        public static void LockoutActiveRejection(
            this ISecurityLogger logger,
            Guid? lockCycleId,
            Guid attemptId,
            TimeSpan lockoutRemaining)
        {
            logger.Log(new SecurityLogEntry
            {
                EventType = SecurityEventType.LockoutActiveRejection,
                LockCycleId = lockCycleId,
                AttemptId = attemptId,
                Message = "Unlock attempt rejected — account is locked out.",
                Properties = new Dictionary<string, object?>
                {
                    ["LockoutRemainingSeconds"] = Math.Round(lockoutRemaining.TotalSeconds, 0)
                }
            });
        }

        public static void PasswordSet(this ISecurityLogger logger)
        {
            logger.Log(new SecurityLogEntry
            {
                EventType = SecurityEventType.PasswordSet,
                Message = "Application password was set or changed."
            });
        }

        public static void AppStartupAuth(
            this ISecurityLogger logger, string mode, bool success)
        {
            logger.Log(new SecurityLogEntry
            {
                EventType = SecurityEventType.AppStartupAuth,
                Message = $"Startup authentication completed (mode={mode}, success={success}).",
                Properties = new Dictionary<string, object?>
                {
                    ["Mode"] = mode,
                    ["Success"] = success
                }
            });
        }

        public static void AppShutdown(this ISecurityLogger logger)
        {
            logger.Log(new SecurityLogEntry
            {
                EventType = SecurityEventType.AppShutdown,
                Message = "Application shutting down."
            });
        }

        public static void DeferredRetryScheduled(
            this ISecurityLogger logger,
            Guid? lockCycleId,
            int currentAttempt,
            int maxAttempts)
        {
            logger.Log(new SecurityLogEntry
            {
                EventType = SecurityEventType.DeferredRetryScheduled,
                LockCycleId = lockCycleId,
                Message = $"Lock deferred ({currentAttempt}/{maxAttempts}); retry scheduled.",
                Properties = new Dictionary<string, object?>
                {
                    ["DeferredAttempt"] = currentAttempt,
                    ["MaxDeferredAttempts"] = maxAttempts
                }
            });
        }

        public static void HandlerException(
            this ISecurityLogger logger,
            string handlerName,
            Exception exception,
            Guid? lockCycleId = null)
        {
            logger.Log(new SecurityLogEntry
            {
                EventType = SecurityEventType.HandlerException,
                LockCycleId = lockCycleId,
                Message = $"{handlerName} handler threw an exception.",
                Properties = new Dictionary<string, object?>
                {
                    ["HandlerName"] = handlerName,
                    ["ExceptionType"] = exception.GetType().FullName,
                    ["ExceptionMessage"] = exception.Message,
                    ["StackTrace"] = exception.StackTrace
                }
            });
        }

        public static void ConfigFallback(
            this ISecurityLogger logger,
            string settingName,
            string configuredValue,
            string fallbackValue)
        {
            logger.Log(new SecurityLogEntry
            {
                EventType = SecurityEventType.HandlerException,
                Message = $"Invalid config for '{settingName}'; falling back to '{fallbackValue}'.",
                Properties = new Dictionary<string, object?>
                {
                    ["SettingName"] = settingName,
                    ["ConfiguredValue"] = configuredValue,
                    ["FallbackValue"] = fallbackValue
                }
            });
        }
    }
}
