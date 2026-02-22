using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using InvoiceGenerator.Models;

namespace InvoiceGenerator.Services
{
    public enum PasswordVerificationStatus
    {
        Success,
        InvalidPassword,
        LockedOut,
        PasswordNotSet
    }

    public sealed class PasswordVerificationResult
    {
        public PasswordVerificationStatus Status { get; init; }
        public int FailedAttempts { get; init; }
        public TimeSpan AppliedDelay { get; init; }
        public TimeSpan LockoutRemaining { get; init; }

        public bool IsSuccess => Status == PasswordVerificationStatus.Success;
    }

    public class AuthService
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int DefaultIterations = 100000;
        private const int LockoutThreshold = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);

        private readonly SettingsService _settingsService = new();
        private readonly ISecurityLogger _logger;

        public AuthService(ISecurityLogger? logger = null)
        {
            _logger = logger ?? NullSecurityLogger.Instance;
        }

        public virtual async Task<bool> IsPasswordSetAsync()
        {
            var settings = await _settingsService.GetSettingsAsync();
            return HasPassword(settings);
        }

        public virtual async Task SetPasswordAsync(string password)
        {
            var settings = await _settingsService.GetSettingsAsync();

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var iterations = DefaultIterations;
            var hash = HashPassword(password, salt, iterations);

            settings.AppPasswordSalt = Convert.ToBase64String(salt);
            settings.AppPasswordHash = Convert.ToBase64String(hash);
            settings.AppPasswordIterations = iterations;
            settings.AppPasswordCreatedUtc = DateTime.UtcNow;
            settings.AppPasswordFailedAttempts = 0;
            settings.AppPasswordLastFailedUtc = null;
            settings.AppPasswordLockoutUntilUtc = null;

            await _settingsService.UpdateSettingsAsync(settings);
            _logger.PasswordSet();
        }

        public async Task<bool> VerifyPasswordAsync(string password)
        {
            var result = await VerifyPasswordWithPolicyAsync(password);
            return result.IsSuccess;
        }

        public virtual async Task<PasswordVerificationResult> VerifyPasswordWithPolicyAsync(string password)
        {
            var settings = await _settingsService.GetSettingsAsync();
            if (!HasPassword(settings))
            {
                return new PasswordVerificationResult
                {
                    Status = PasswordVerificationStatus.PasswordNotSet,
                    FailedAttempts = 0,
                    AppliedDelay = TimeSpan.Zero,
                    LockoutRemaining = TimeSpan.Zero
                };
            }

            var now = DateTime.UtcNow;

            if (settings.AppPasswordLockoutUntilUtc.HasValue && settings.AppPasswordLockoutUntilUtc.Value > now)
            {
                var lockoutRemaining = settings.AppPasswordLockoutUntilUtc.Value - now;
                return new PasswordVerificationResult
                {
                    Status = PasswordVerificationStatus.LockedOut,
                    FailedAttempts = settings.AppPasswordFailedAttempts,
                    AppliedDelay = TimeSpan.Zero,
                    LockoutRemaining = lockoutRemaining
                };
            }

            if (settings.AppPasswordLockoutUntilUtc.HasValue && settings.AppPasswordLockoutUntilUtc.Value <= now)
            {
                _logger.LockoutExpired(null, settings.AppPasswordFailedAttempts);
                settings.AppPasswordFailedAttempts = 0;
                settings.AppPasswordLastFailedUtc = null;
                settings.AppPasswordLockoutUntilUtc = null;
            }

            var salt = Convert.FromBase64String(settings.AppPasswordSalt ?? string.Empty);
            var iterations = settings.AppPasswordIterations > 0 ? settings.AppPasswordIterations : DefaultIterations;
            var expectedHash = Convert.FromBase64String(settings.AppPasswordHash ?? string.Empty);
            var actualHash = HashPassword(password, salt, iterations);

            if (CryptographicOperations.FixedTimeEquals(actualHash, expectedHash))
            {
                settings.AppPasswordFailedAttempts = 0;
                settings.AppPasswordLastFailedUtc = null;
                settings.AppPasswordLockoutUntilUtc = null;
                await _settingsService.UpdateSettingsAsync(settings);

                return new PasswordVerificationResult
                {
                    Status = PasswordVerificationStatus.Success,
                    FailedAttempts = 0,
                    AppliedDelay = TimeSpan.Zero,
                    LockoutRemaining = TimeSpan.Zero
                };
            }

            var failedAttempts = settings.AppPasswordFailedAttempts + 1;
            settings.AppPasswordFailedAttempts = failedAttempts;
            settings.AppPasswordLastFailedUtc = now;

            var appliedDelay = failedAttempts switch
            {
                1 => TimeSpan.FromSeconds(1),
                2 => TimeSpan.FromSeconds(2),
                3 => TimeSpan.FromSeconds(4),
                4 => TimeSpan.FromSeconds(8),
                _ => TimeSpan.Zero
            };

            if (failedAttempts >= LockoutThreshold)
            {
                settings.AppPasswordLockoutUntilUtc = now.Add(LockoutDuration);
                _logger.LockoutStarted(null, failedAttempts, LockoutDuration);
            }
            else
            {
                settings.AppPasswordLockoutUntilUtc = null;
            }

            await _settingsService.UpdateSettingsAsync(settings);

            if (appliedDelay > TimeSpan.Zero)
            {
                await Task.Delay(appliedDelay);
            }

            if (failedAttempts >= LockoutThreshold)
            {
                return new PasswordVerificationResult
                {
                    Status = PasswordVerificationStatus.LockedOut,
                    FailedAttempts = failedAttempts,
                    AppliedDelay = TimeSpan.Zero,
                    LockoutRemaining = LockoutDuration
                };
            }

            return new PasswordVerificationResult
            {
                Status = PasswordVerificationStatus.InvalidPassword,
                FailedAttempts = failedAttempts,
                AppliedDelay = appliedDelay,
                LockoutRemaining = TimeSpan.Zero
            };
        }

        private static bool HasPassword(AppSettings settings)
        {
            return !string.IsNullOrWhiteSpace(settings.AppPasswordHash)
                && !string.IsNullOrWhiteSpace(settings.AppPasswordSalt)
                && settings.AppPasswordIterations > 0;
        }

        private static byte[] HashPassword(string password, byte[] salt, int iterations)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(HashSize);
        }
    }
}
