using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using InvoiceGenerator.Services;
using InvoiceGenerator.Views;

namespace InvoiceGenerator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly TimeSpan InactivityTimeout = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan FallbackInactivityTimeout = TimeSpan.FromMinutes(5);

        private InactivityLockService? _inactivityLockService;
        private bool _isLockScreenActive;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            try
            {
                // Ensure database and tables exist before any view loads.
                var settingsService = new SettingsService();
                settingsService.InitializeDatabaseAsync().GetAwaiter().GetResult();

                var passwordDialog = new AppPasswordDialog();
                var result = passwordDialog.ShowDialog();
                if (result != true)
                {
                    Shutdown();
                    return;
                }

                var startupLeft = passwordDialog.Left;
                var startupTop = passwordDialog.Top;
                var startupWidth = passwordDialog.ActualWidth > 0 ? passwordDialog.ActualWidth : passwordDialog.Width;
                var startupHeight = passwordDialog.ActualHeight > 0 ? passwordDialog.ActualHeight : passwordDialog.Height;

                var mainWindow = new MainWindow();
                mainWindow.WindowStartupLocation = WindowStartupLocation.Manual;
                mainWindow.Left = startupLeft;
                mainWindow.Top = startupTop;
                mainWindow.Width = startupWidth;
                mainWindow.Height = startupHeight;
                MainWindow = mainWindow;
                ShutdownMode = ShutdownMode.OnMainWindowClose;
                mainWindow.Show();

                InitializeInactivityLock();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing application: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}", 
                    "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        private void InitializeInactivityLock()
        {
            var effectiveTimeout = ResolveInactivityTimeout(InactivityTimeout);
            _inactivityLockService = new InactivityLockService(effectiveTimeout);
            _inactivityLockService.TimeoutElapsed += InactivityLockService_TimeoutElapsed;

            InputManager.Current.PreProcessInput += Current_PreProcessInput;
            Activated += App_Activated;
            Exit += App_Exit;

            _inactivityLockService.Start();
        }

        private static TimeSpan ResolveInactivityTimeout(TimeSpan configuredTimeout)
        {
            if (configuredTimeout > TimeSpan.Zero)
            {
                return configuredTimeout;
            }

            Debug.WriteLine($"Invalid inactivity timeout '{configuredTimeout}'. Falling back to '{FallbackInactivityTimeout}'.");
            return FallbackInactivityTimeout;
        }

        private void App_Activated(object? sender, EventArgs e)
        {
            RegisterUserActivity();
        }

        private void Current_PreProcessInput(object? sender, PreProcessInputEventArgs e)
        {
            if (_isLockScreenActive)
            {
                return;
            }

            if (e.StagingItem.Input is KeyboardEventArgs
                || e.StagingItem.Input is MouseEventArgs)
            {
                RegisterUserActivity();
            }
        }

        private void RegisterUserActivity()
        {
            _inactivityLockService?.RegisterActivity();
        }

        private void InactivityLockService_TimeoutElapsed(object? sender, EventArgs e)
        {
            if (Dispatcher.CheckAccess())
            {
                ShowLockScreen();
                return;
            }

            _ = Dispatcher.InvokeAsync(ShowLockScreen);
        }

        private void ShowLockScreen()
        {
            if (_isLockScreenActive || MainWindow is null)
            {
                return;
            }

            _isLockScreenActive = true;

            try
            {
                CloseSecondaryWindows();

                var lockDialog = new AppPasswordDialog(isLockScreenMode: true)
                {
                    Owner = MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var result = lockDialog.ShowDialog();
                if (result != true)
                {
                    Shutdown();
                    return;
                }

                _inactivityLockService?.Start();
            }
            finally
            {
                _isLockScreenActive = false;
            }
        }

        private void CloseSecondaryWindows()
        {
            var windows = Current.Windows
                .OfType<Window>()
                .Where(window => !ReferenceEquals(window, MainWindow))
                .ToList();

            foreach (var window in windows)
            {
                if (window.IsVisible)
                {
                    window.Close();
                }
            }
        }

        private void App_Exit(object? sender, ExitEventArgs e)
        {
            if (_inactivityLockService is not null)
            {
                _inactivityLockService.TimeoutElapsed -= InactivityLockService_TimeoutElapsed;
                _inactivityLockService.Stop();
                _inactivityLockService.Dispose();
                _inactivityLockService = null;
            }

            InputManager.Current.PreProcessInput -= Current_PreProcessInput;
            Activated -= App_Activated;
            Exit -= App_Exit;
        }
    }
}
