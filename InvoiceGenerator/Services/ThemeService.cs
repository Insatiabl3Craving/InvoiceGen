using System;
using System.Windows;
using InvoiceGenerator.Utilities;

namespace InvoiceGenerator.Services
{
    /// <summary>
    /// Swaps theme resource dictionaries at runtime and persists the choice.
    /// </summary>
    public class ThemeService
    {
        private const string LightThemeUri = "Themes/LightTheme.xaml";
        private const string DarkThemeUri  = "Themes/DarkTheme.xaml";

        private readonly SettingsService _settingsService;

        public ThemeService(SettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public ThemeService() : this(new SettingsService()) { }

        /// <summary>Current theme name ("Light" or "Dark").</summary>
        public string CurrentTheme { get; private set; } = "Light";

        /// <summary>
        /// Applies a theme by swapping the first merged dictionary in Application.Resources.
        /// </summary>
        public void ApplyTheme(string themeName)
        {
            var uri = themeName.Equals("Dark", StringComparison.OrdinalIgnoreCase)
                ? DarkThemeUri
                : LightThemeUri;

            var newDict = new ResourceDictionary
            {
                Source = new Uri(uri, UriKind.Relative)
            };

            var mergedDicts = Application.Current.Resources.MergedDictionaries;

            // Find and replace the existing theme dictionary (index 0 by convention)
            if (mergedDicts.Count > 0)
            {
                mergedDicts[0] = newDict;
            }
            else
            {
                mergedDicts.Insert(0, newDict);
            }

            CurrentTheme = themeName.Equals("Dark", StringComparison.OrdinalIgnoreCase)
                ? "Dark"
                : "Light";

            DarkTitleBarHelper.ApplyToAllOpenWindows(CurrentTheme == "Dark");
        }

        /// <summary>
        /// Reads the persisted theme preference and applies it.
        /// Call this once during application startup before any window is shown.
        /// </summary>
        public async System.Threading.Tasks.Task ApplyStoredThemeAsync()
        {
            try
            {
                var settings = await _settingsService.GetSettingsAsync();
                ApplyTheme(settings.Theme ?? "Light");
            }
            catch
            {
                // Fallback to light on any error
                ApplyTheme("Light");
            }
        }

        /// <summary>
        /// Persists the current theme choice to the database.
        /// </summary>
        public async System.Threading.Tasks.Task SaveThemePreferenceAsync(string themeName)
        {
            var settings = await _settingsService.GetSettingsAsync();
            settings.Theme = themeName.Equals("Dark", StringComparison.OrdinalIgnoreCase)
                ? "Dark"
                : "Light";
            await _settingsService.UpdateSettingsAsync(settings);
        }
    }
}
