using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace InvoiceGenerator.Utilities
{
    internal static class DarkTitleBarHelper
    {
        private const int DwmwaUseImmersiveDarkMode = 20;
        private const int DwmwaUseImmersiveDarkModeLegacy = 19;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int pvAttribute, int cbAttribute);

        public static void Apply(Window window, bool useDarkMode)
        {
            if (window == null)
            {
                return;
            }

            if (window.IsLoaded)
            {
                ApplyToHandle(new WindowInteropHelper(window).Handle, useDarkMode);
                return;
            }

            void OnSourceInitialized(object? _, EventArgs __)
            {
                window.SourceInitialized -= OnSourceInitialized;
                ApplyToHandle(new WindowInteropHelper(window).Handle, useDarkMode);
            }

            window.SourceInitialized += OnSourceInitialized;
        }

        public static void ApplyToAllOpenWindows(bool useDarkMode)
        {
            foreach (Window window in Application.Current.Windows)
            {
                Apply(window, useDarkMode);
            }
        }

        private static void ApplyToHandle(IntPtr handle, bool useDarkMode)
        {
            if (handle == IntPtr.Zero)
            {
                return;
            }

            var useDark = useDarkMode ? 1 : 0;

            try
            {
                _ = DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref useDark, Marshal.SizeOf<int>());
                _ = DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkModeLegacy, ref useDark, Marshal.SizeOf<int>());
            }
            catch
            {
                // No-op on unsupported Windows versions or if DWM is unavailable.
            }
        }
    }
}
