using System;
using System.Threading.Tasks;
using InvoiceGenerator.Services;
using Xunit;

namespace InvoiceGenerator.Tests;

public class AuthStateCoordinatorTests
{
    [Fact]
    public void HandleInactivityTimeout_ReturnsShowLock_WhenPolicyAllows()
    {
        var coordinator = new AuthStateCoordinator(
            lockPolicyEvaluator: new FixedPolicyEvaluator(LockDecision.Allow),
            deferredRetryInterval: TimeSpan.FromMilliseconds(100),
            maxDeferredAttempts: 3);

        var command = coordinator.HandleInactivityTimeout(new AppLockStateSnapshot
        {
            MainWindowReady = true,
            MainWindowActive = true,
            VisibleWindowCount = 1
        });

        Assert.Equal(AuthCoordinatorTimeoutAction.ShowLock, command);
    }

    [Fact]
    public void HandleInactivityTimeout_ForcesShowLock_AfterMaxDeferredAttempts()
    {
        var coordinator = new AuthStateCoordinator(
            lockPolicyEvaluator: new FixedPolicyEvaluator(LockDecision.Defer),
            deferredRetryInterval: TimeSpan.FromMilliseconds(100),
            maxDeferredAttempts: 3);

        var snapshot = new AppLockStateSnapshot
        {
            MainWindowReady = true,
            MainWindowActive = false,
            VisibleWindowCount = 1
        };

        Assert.Equal(AuthCoordinatorTimeoutAction.Defer, coordinator.HandleInactivityTimeout(snapshot));
        Assert.Equal(AuthCoordinatorTimeoutAction.Defer, coordinator.HandleInactivityTimeout(snapshot));
        Assert.Equal(AuthCoordinatorTimeoutAction.ShowLock, coordinator.HandleInactivityTimeout(snapshot));
    }

    [Fact]
    public async Task TryUnlockAsync_WithEmptyPassword_ReturnsValidationError()
    {
        var coordinator = new AuthStateCoordinator();

        var result = await coordinator.TryUnlockAsync(string.Empty);

        Assert.Equal(AuthCoordinatorUnlockStatus.EmptyPassword, result.Status);
        Assert.False(result.IsSuccess);
        Assert.Equal("Please enter your password.", result.Message);
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
