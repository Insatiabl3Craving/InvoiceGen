using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InvoiceGenerator.Services;
using Xunit;

namespace InvoiceGenerator.Tests;

/// <summary>
/// Verifies that correlation IDs flow consistently through the
/// <see cref="AuthStateCoordinator"/> lock→unlock cycle.
/// </summary>
public class SecurityLogCorrelationTests
{
    [Fact]
    public void HandleInactivityTimeout_ShowLock_SetsLockCycleIdAndLogs()
    {
        var logger = new CapturingSecurityLogger();
        var coordinator = new AuthStateCoordinator(
            lockPolicyEvaluator: new FixedPolicyEvaluator(LockDecision.Allow),
            deferredRetryInterval: TimeSpan.FromMilliseconds(100),
            maxDeferredAttempts: 5,
            logger: logger);

        Assert.Null(coordinator.CurrentLockCycleId);

        var action = coordinator.HandleInactivityTimeout(CleanSnapshot());

        Assert.Equal(AuthCoordinatorTimeoutAction.ShowLock, action);
        Assert.NotNull(coordinator.CurrentLockCycleId);

        // Should have logged LockPolicyDecision and InactivityTimeoutFired
        var policyEntry = logger.Entries.FirstOrDefault(e => e.EventType == SecurityEventType.LockPolicyDecision);
        var timeoutEntry = logger.Entries.FirstOrDefault(e => e.EventType == SecurityEventType.InactivityTimeoutFired);
        Assert.NotNull(policyEntry);
        Assert.NotNull(timeoutEntry);

        // Both entries share the same LockCycleId
        Assert.Equal(coordinator.CurrentLockCycleId, policyEntry!.LockCycleId);
        Assert.Equal(coordinator.CurrentLockCycleId, timeoutEntry!.LockCycleId);
    }

    [Fact]
    public void HandleInactivityTimeout_Defer_LogsDeferredRetryWithConsistentId()
    {
        var logger = new CapturingSecurityLogger();
        var coordinator = new AuthStateCoordinator(
            lockPolicyEvaluator: new FixedPolicyEvaluator(LockDecision.Defer),
            deferredRetryInterval: TimeSpan.FromMilliseconds(100),
            maxDeferredAttempts: 5,
            logger: logger);

        var snapshot = new AppLockStateSnapshot
        {
            MainWindowReady = true,
            MainWindowActive = false,
            VisibleWindowCount = 1
        };

        coordinator.HandleInactivityTimeout(snapshot);

        var deferEntry = logger.Entries.FirstOrDefault(e => e.EventType == SecurityEventType.DeferredRetryScheduled);
        Assert.NotNull(deferEntry);
        Assert.NotNull(deferEntry!.Properties);
        Assert.Equal(1, deferEntry.Properties!["DeferredAttempt"]);
        Assert.Equal(5, deferEntry.Properties!["MaxDeferredAttempts"]);
    }

    [Fact]
    public void MarkLockShown_LogsLockScreenShown()
    {
        var logger = new CapturingSecurityLogger();
        var coordinator = new AuthStateCoordinator(
            lockPolicyEvaluator: new FixedPolicyEvaluator(LockDecision.Allow),
            logger: logger);

        coordinator.HandleInactivityTimeout(CleanSnapshot());
        var cycleId = coordinator.CurrentLockCycleId;

        logger.Entries.Clear();
        coordinator.MarkLockShown();

        var entry = logger.Entries.FirstOrDefault(e => e.EventType == SecurityEventType.LockScreenShown);
        Assert.NotNull(entry);
        Assert.Equal(cycleId, entry!.LockCycleId);
    }

    [Fact]
    public void MarkUnlocked_LogsUnlockSuccessAndClearsLockCycleId()
    {
        var logger = new CapturingSecurityLogger();
        var coordinator = new AuthStateCoordinator(
            lockPolicyEvaluator: new FixedPolicyEvaluator(LockDecision.Allow),
            logger: logger);

        coordinator.HandleInactivityTimeout(CleanSnapshot());
        var cycleId = coordinator.CurrentLockCycleId;
        coordinator.MarkLockShown();

        logger.Entries.Clear();
        coordinator.MarkUnlocked();

        Assert.Null(coordinator.CurrentLockCycleId);

        var entry = logger.Entries.FirstOrDefault(e => e.EventType == SecurityEventType.UnlockSuccess);
        Assert.NotNull(entry);
        Assert.Equal(cycleId, entry!.LockCycleId);
    }

    [Fact]
    public void MarkShuttingDown_LogsAppShutdown()
    {
        var logger = new CapturingSecurityLogger();
        var coordinator = new AuthStateCoordinator(logger: logger);

        coordinator.MarkShuttingDown();

        var entry = logger.Entries.FirstOrDefault(e => e.EventType == SecurityEventType.AppShutdown);
        Assert.NotNull(entry);
    }

    [Fact]
    public async Task TryUnlockAsync_EmptyPassword_LogsUnlockFailed()
    {
        var logger = new CapturingSecurityLogger();
        var coordinator = new AuthStateCoordinator(logger: logger);

        _ = await coordinator.TryUnlockAsync(string.Empty);

        var entry = logger.Entries.FirstOrDefault(e => e.EventType == SecurityEventType.UnlockFailed);
        Assert.NotNull(entry);
        Assert.NotNull(entry!.AttemptId);
        Assert.NotEqual(Guid.Empty, entry.AttemptId!.Value);
        Assert.Equal("EmptyPassword", entry.Properties?["Status"]?.ToString());
    }

    [Fact]
    public void ConsecutiveLockCycles_HaveDifferentCycleIds()
    {
        var logger = new CapturingSecurityLogger();
        var coordinator = new AuthStateCoordinator(
            lockPolicyEvaluator: new FixedPolicyEvaluator(LockDecision.Allow),
            logger: logger);

        // Cycle 1
        coordinator.HandleInactivityTimeout(CleanSnapshot());
        var cycleId1 = coordinator.CurrentLockCycleId;
        coordinator.MarkLockShown();
        coordinator.MarkUnlocked();

        // Cycle 2
        coordinator.HandleInactivityTimeout(CleanSnapshot());
        var cycleId2 = coordinator.CurrentLockCycleId;

        Assert.NotNull(cycleId1);
        Assert.NotNull(cycleId2);
        Assert.NotEqual(cycleId1, cycleId2);
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static AppLockStateSnapshot CleanSnapshot() => new()
    {
        MainWindowReady = true,
        MainWindowActive = true,
        VisibleWindowCount = 1
    };

    internal sealed class CapturingSecurityLogger : ISecurityLogger
    {
        public List<SecurityLogEntry> Entries { get; } = new();

        public void Log(SecurityLogEntry entry)
        {
            Entries.Add(entry);
        }
    }

    private sealed class FixedPolicyEvaluator : ILockPolicyEvaluator
    {
        private readonly LockDecision _decision;

        public FixedPolicyEvaluator(LockDecision decision)
        {
            _decision = decision;
        }

        public LockDecision Evaluate(AppLockStateSnapshot state)
        {
            return _decision;
        }
    }
}
