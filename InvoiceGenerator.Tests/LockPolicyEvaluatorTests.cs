using InvoiceGenerator.Services;
using Xunit;

namespace InvoiceGenerator.Tests;

public class LockPolicyEvaluatorTests
{
    private readonly DefaultLockPolicyEvaluator _evaluator = new();

    [Fact]
    public void Evaluate_ReturnsSkip_WhenShuttingDown()
    {
        var decision = _evaluator.Evaluate(new AppLockStateSnapshot
        {
            IsShuttingDown = true,
            MainWindowReady = true,
            MainWindowActive = true
        });

        Assert.Equal(LockDecision.Skip, decision);
    }

    [Fact]
    public void Evaluate_ReturnsSkip_WhenAlreadyLocked()
    {
        var decision = _evaluator.Evaluate(new AppLockStateSnapshot
        {
            IsAlreadyLocked = true,
            MainWindowReady = true,
            MainWindowActive = true
        });

        Assert.Equal(LockDecision.Skip, decision);
    }

    [Fact]
    public void Evaluate_ReturnsDefer_WhenMainWindowNotActive()
    {
        var decision = _evaluator.Evaluate(new AppLockStateSnapshot
        {
            MainWindowReady = true,
            MainWindowActive = false
        });

        Assert.Equal(LockDecision.Defer, decision);
    }

    [Fact]
    public void Evaluate_ReturnsDefer_WhenBlockingModalExists()
    {
        var decision = _evaluator.Evaluate(new AppLockStateSnapshot
        {
            MainWindowReady = true,
            MainWindowActive = true,
            HasBlockingModal = true
        });

        Assert.Equal(LockDecision.Defer, decision);
    }

    [Fact]
    public void Evaluate_ReturnsAllow_WhenStateIsClean()
    {
        var decision = _evaluator.Evaluate(new AppLockStateSnapshot
        {
            MainWindowReady = true,
            MainWindowActive = true,
            VisibleWindowCount = 1
        });

        Assert.Equal(LockDecision.Allow, decision);
    }
}
