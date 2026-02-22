using System;
using System.Threading.Tasks;
using InvoiceGenerator.Services;
using InvoiceGenerator.ViewModels;
using Xunit;

namespace InvoiceGenerator.Tests;

public class AppPasswordDialogViewModelTests
{
    /// <summary>
    /// Test double that avoids hitting CredentialManager / the real DB.
    /// </summary>
    private sealed class StubAuthService : AuthService
    {
        private readonly bool _isPasswordSet;
        private bool _passwordWasSet;

        public StubAuthService(bool isPasswordSet = false) : base(null)
        {
            _isPasswordSet = isPasswordSet;
        }

        public override Task<bool> IsPasswordSetAsync()
            => Task.FromResult(_isPasswordSet || _passwordWasSet);

        public override Task SetPasswordAsync(string password)
        {
            _passwordWasSet = true;
            return Task.CompletedTask;
        }

        public override Task<PasswordVerificationResult> VerifyPasswordWithPolicyAsync(string password)
        {
            // Return success only for "CorrectPassword"
            if (password == "CorrectPassword")
            {
                return Task.FromResult(new PasswordVerificationResult { Status = PasswordVerificationStatus.Success });
            }

            return Task.FromResult(new PasswordVerificationResult
            {
                Status = PasswordVerificationStatus.InvalidPassword,
                FailedAttempts = 1
            });
        }
    }

    private static AppPasswordDialogViewModel CreateViewModel(
        bool isLockScreenMode = false,
        bool isPasswordSet = false,
        ISecurityLogger? logger = null)
    {
        var coordinator = new AuthStateCoordinator(logger: logger);
        var authService = new StubAuthService(isPasswordSet);
        return new AppPasswordDialogViewModel(coordinator, logger, authService, isLockScreenMode);
    }

    // ── Initialisation tests ─────────────────────────────────────

    [Fact]
    public async Task InitializeAsync_LockScreenMode_SetsExpectedState()
    {
        var vm = CreateViewModel(isLockScreenMode: true);

        await vm.InitializeAsync();

        Assert.False(vm.IsSetupMode);
        Assert.False(vm.IsConfirmPanelVisible);
        Assert.Equal("Session locked after inactivity. Enter your password to continue.", vm.HintText);
        Assert.Equal("Unlock", vm.SubmitTooltip);
    }

    [Fact]
    public async Task InitializeAsync_NoPasswordSet_EntersSetupMode()
    {
        var vm = CreateViewModel(isPasswordSet: false);

        await vm.InitializeAsync();

        Assert.True(vm.IsSetupMode);
        Assert.True(vm.IsConfirmPanelVisible);
        Assert.Contains("Minimum 8 characters", vm.HintText);
        Assert.Equal("Set Password", vm.SubmitTooltip);
    }

    // ── Cancel test ──────────────────────────────────────────────

    [Fact]
    public void Cancel_SetsDialogOutcomeFalse()
    {
        var vm = CreateViewModel();

        vm.Cancel();

        Assert.False(vm.DialogOutcome);
    }

    // ── Setup mode submission tests ──────────────────────────────

    [Fact]
    public async Task SubmitAsync_SetupMode_EmptyPassword_RaisesShake()
    {
        var vm = CreateViewModel();
        await vm.InitializeAsync();

        var shakeRaised = false;
        vm.ShakeRequested += (_, _) => shakeRaised = true;

        await vm.SubmitCommand.ExecuteAsync(new PasswordPair { Password = "", ConfirmPassword = "" });

        Assert.True(shakeRaised);
        Assert.True(vm.HasError);
        Assert.Equal("Please enter a password.", vm.ErrorMessage);
    }

    [Fact]
    public async Task SubmitAsync_SetupMode_ShortPassword_RaisesShake()
    {
        var vm = CreateViewModel();
        await vm.InitializeAsync();

        var shakeRaised = false;
        vm.ShakeRequested += (_, _) => shakeRaised = true;

        await vm.SubmitCommand.ExecuteAsync(new PasswordPair { Password = "abc", ConfirmPassword = "abc" });

        Assert.True(shakeRaised);
        Assert.True(vm.HasError);
        Assert.Equal("Password must be at least 8 characters.", vm.ErrorMessage);
    }

    [Fact]
    public async Task SubmitAsync_SetupMode_MismatchedPasswords_RaisesShake()
    {
        var vm = CreateViewModel();
        await vm.InitializeAsync();

        var shakeRaised = false;
        vm.ShakeRequested += (_, _) => shakeRaised = true;

        await vm.SubmitCommand.ExecuteAsync(new PasswordPair
        {
            Password = "LongPassword123",
            ConfirmPassword = "DifferentPassword456"
        });

        Assert.True(shakeRaised);
        Assert.True(vm.HasError);
        Assert.Equal("Passwords do not match.", vm.ErrorMessage);
    }

    // ── Verify mode submission tests ─────────────────────────────

    [Fact]
    public async Task SubmitAsync_VerifyMode_EmptyPassword_RaisesShake()
    {
        var vm = CreateViewModel(isLockScreenMode: true);
        await vm.InitializeAsync();

        var shakeRaised = false;
        vm.ShakeRequested += (_, _) => shakeRaised = true;

        await vm.SubmitCommand.ExecuteAsync(new PasswordPair { Password = "" });

        Assert.True(shakeRaised);
        Assert.True(vm.HasError);
        Assert.Equal("Please enter your password.", vm.ErrorMessage);
    }

    [Fact]
    public async Task SubmitAsync_ResetsIsVerifying_AfterCompletion()
    {
        var vm = CreateViewModel(isLockScreenMode: true);
        await vm.InitializeAsync();

        await vm.SubmitCommand.ExecuteAsync(new PasswordPair { Password = "" });

        Assert.False(vm.IsVerifying);
    }

    // ── Error state tests ────────────────────────────────────────

    [Fact]
    public async Task SubmitAsync_ClearsError_BeforeValidation()
    {
        var vm = CreateViewModel();
        await vm.InitializeAsync();

        // Trigger first error
        await vm.SubmitCommand.ExecuteAsync(new PasswordPair { Password = "" });
        Assert.True(vm.HasError);

        // Second submit should clear old error before new validation
        var errorMessages = new System.Collections.Generic.List<string?>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(vm.HasError) && !vm.HasError)
                errorMessages.Add(vm.ErrorMessage);
        };

        await vm.SubmitCommand.ExecuteAsync(new PasswordPair { Password = "short" });

        // Error was cleared (HasError went false) before new error was set
        Assert.Contains(null as string, errorMessages);
    }

    [Fact]
    public void Constructor_ThrowsOnNullCoordinator()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AppPasswordDialogViewModel(null!));
    }

    [Fact]
    public void DialogOutcome_IsNull_Initially()
    {
        var vm = CreateViewModel();

        Assert.Null(vm.DialogOutcome);
    }
}
