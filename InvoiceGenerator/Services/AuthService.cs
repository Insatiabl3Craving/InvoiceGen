using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using InvoiceGenerator.Models;

namespace InvoiceGenerator.Services
{
    public class AuthService
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int DefaultIterations = 100000;

        private readonly SettingsService _settingsService = new();

        public async Task<bool> IsPasswordSetAsync()
        {
            var settings = await _settingsService.GetSettingsAsync();
            return HasPassword(settings);
        }

        public async Task SetPasswordAsync(string password)
        {
            var settings = await _settingsService.GetSettingsAsync();

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var iterations = DefaultIterations;
            var hash = HashPassword(password, salt, iterations);

            settings.AppPasswordSalt = Convert.ToBase64String(salt);
            settings.AppPasswordHash = Convert.ToBase64String(hash);
            settings.AppPasswordIterations = iterations;
            settings.AppPasswordCreatedUtc = DateTime.UtcNow;

            await _settingsService.UpdateSettingsAsync(settings);
        }

        public async Task<bool> VerifyPasswordAsync(string password)
        {
            var settings = await _settingsService.GetSettingsAsync();
            if (!HasPassword(settings))
            {
                return false;
            }

            var salt = Convert.FromBase64String(settings.AppPasswordSalt ?? string.Empty);
            var iterations = settings.AppPasswordIterations > 0 ? settings.AppPasswordIterations : DefaultIterations;
            var expectedHash = Convert.FromBase64String(settings.AppPasswordHash ?? string.Empty);
            var actualHash = HashPassword(password, salt, iterations);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
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
