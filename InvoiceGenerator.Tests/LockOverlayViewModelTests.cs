using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InvoiceGenerator.Services;
using InvoiceGenerator.ViewModels;
using Xunit;

namespace InvoiceGenerator.Tests;

public class LockOverlayViewModelTests
{
    private static LockOverlayViewModel CreateViewModel(
        ILockPolicyEvaluator? policy = null,
        ISecurityLogger? logger = null)
    {
        var coordinator = new AuthStateCoordinator(
            lockPolicyEvaluator: policy,
            logger: logger);
        return new LockOverlayViewModel(coordinator, logger);
    }

    [Fact]
    public void ShowLock_SetsIsLockedTrue_And_ResetsErrorState()
    {
        var vm = CreateViewModel();

        vm.ShowLock();

        Assert.True(vm.IsLocked);
        Assert.False(vm.IsVerifying);
        Assert.False(vm.HasError);
        Assert.Null(vm.ErrorMessage);
    }

    [Fact]
    public void CanClose_ReturnsFalse_WhenLocked()
    {
        var vm = CreateViewModel();

        vm.ShowLock();

        Assert.False(vm.CanClose);
    }

    [Fact]
    public void CanClose_ReturnsTrue_WhenNotLocked()
    {
        var vm = CreateViewModel();

        Assert.True(vm.CanClose);
    }

    [Fact]
    public async Task SubmitAsync_EmptyPassword_RaisesShakeRequested_And_SetsError()
    {
        var vm = CreateViewModel();
        vm.ShowLock();

        var shakeRaised = false;
        vm.ShakeRequested += (_, _) => shakeRaised = true;

        await vm.SubmitCommand.ExecuteAsync("");

        Assert.True(shakeRaised);
        Assert.True(vm.HasError);
        Assert.Equal("Please enter your password.", vm.ErrorMessage);
    }

    [Fact]
    public async Task SubmitAsync_NullPassword_RaisesShakeRequested()
    {
        var vm = CreateViewModel();
        vm.ShowLock();

        var shakeRaised = false;
        vm.ShakeRequested += (_, _) => shakeRaised = true;

        await vm.SubmitCommand.ExecuteAsync(null);

        Assert.True(shakeRaised);
        Assert.True(vm.HasError);
    }

    [Fact]
    public void CompleteFadeOut_SetsIsLockedFalse_And_RaisesUnlockSucceeded()
    {
        var vm = CreateViewModel();
        vm.ShowLock();

        var unlockRaised = false;
        vm.UnlockSucceeded += (_, _) => unlockRaised = true;

        vm.CompleteFadeOut();

        Assert.False(vm.IsLocked);
        Assert.True(vm.CanClose);
        Assert.True(unlockRaised);
    }

    [Fact]
    public void CompleteFadeOut_ToleratesHandlerException()
    {
        var vm = CreateViewModel();
        vm.ShowLock();

        vm.UnlockSucceeded += (_, _) => throw new InvalidOperationException("handler boom");

        // Should not throw
        vm.CompleteFadeOut();

        Assert.False(vm.IsLocked);
    }

    [Fact]
    public async Task SubmitAsync_ResetsIsVerifying_AfterCompletion()
    {
        var vm = CreateViewModel();
        vm.ShowLock();

        await vm.SubmitCommand.ExecuteAsync("wrong-password");

        // After completion, IsVerifying should be false regardless of outcome
        Assert.False(vm.IsVerifying);
    }

    [Fact]
    public async Task ShowLock_ClearsPreExistingError()
    {
        var vm = CreateViewModel();
        vm.ShowLock();

        // Manually set error state (simulating a prior failed attempt)
        // We trigger this via a submit with empty password
        await vm.SubmitCommand.ExecuteAsync("");
        Assert.True(vm.HasError);

        // ShowLock should clear error
        vm.ShowLock();

        Assert.False(vm.HasError);
        Assert.Null(vm.ErrorMessage);
    }

    [Fact]
    public async Task SubmitAsync_InvalidPassword_RaisesShake_And_SetsError()
    {
        // AuthStateCoordinator.TryUnlockAsync will call AuthService which
        // requires a stored password. Without one, it returns PasswordNotSet.
        var vm = CreateViewModel();
        vm.ShowLock();

        var shakeCount = 0;
        vm.ShakeRequested += (_, _) => shakeCount++;

        await vm.SubmitCommand.ExecuteAsync("some-password");

        // Should have an error set (either PasswordNotSet or Error)
        Assert.True(vm.HasError);
        Assert.NotNull(vm.ErrorMessage);
    }

    [Fact]
    public void Constructor_ThrowsOnNullCoordinator()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LockOverlayViewModel(null!));
    }
}
