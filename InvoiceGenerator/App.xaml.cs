using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
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

        private readonly FileSecurityLogger _securityLogger = new();
        private InactivityLockService? _inactivityLockService;
        private readonly AuthStateCoordinator _authCoordinator;
        private DispatcherTimer? _deferredLockTimer;
        private bool _isLockScreenActive;

        public App()
        {
            _authCoordinator = new AuthStateCoordinator(logger: _securityLogger);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            try
            {
                // Ensure database and tables exist before any view loads.
                var settingsService = new SettingsService();
                settingsService.InitializeDatabaseAsync().GetAwaiter().GetResult();

                var passwordDialog = new AppPasswordDialog(authCoordinator: _authCoordinator, logger: _securityLogger);
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

                var mainWindow = new MainWindow(_authCoordinator, _securityLogger);
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
            _inactivityLockService.SetLogger(_securityLogger);
            _inactivityLockService.TimeoutElapsed += InactivityLockService_TimeoutElapsed;

            // Subscribe to unlock event from the main window's in-place lock overlay
            ((MainWindow)MainWindow).UnlockSucceeded += MainWindow_UnlockSucceeded;

            InputManager.Current.PreProcessInput += Current_PreProcessInput;
            Activated += App_Activated;
            Exit += App_Exit;

            _inactivityLockService.ResumeAfterUnlock();
        }

        private TimeSpan ResolveInactivityTimeout(TimeSpan configuredTimeout)
        {
            if (configuredTimeout > TimeSpan.Zero)
            {
                return configuredTimeout;
            }

            _securityLogger.ConfigFallback("InactivityTimeout",
                configuredTimeout.ToString(), FallbackInactivityTimeout.ToString());
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
                ProcessInactivityTimeout();
                return;
            }

            _ = Dispatcher.InvokeAsync(ProcessInactivityTimeout);
        }

        private void ProcessInactivityTimeout()
        {
            var snapshot = BuildLockStateSnapshot();
            var decision = _authCoordinator.HandleInactivityTimeout(snapshot);

            switch (decision)
            {
                case AuthCoordinatorTimeoutAction.ShowLock:
                    StopDeferredLockRetry();
                    ShowLockScreen();
                    break;
                case AuthCoordinatorTimeoutAction.Defer:
                    ScheduleDeferredLockRetry();
                    break;
                default:
                    StopDeferredLockRetry();
                    break;
            }
        }

        private void ShowLockScreen()
        {
            if (_isLockScreenActive || MainWindow is not MainWindow mainWindow)
            {
                return;
            }

            _isLockScreenActive = true;
            _authCoordinator.MarkLockShown();
            _inactivityLockService?.PauseForLock();

            CloseSecondaryWindows();

            // Show the in-window lock overlay instead of a separate dialog
            mainWindow.ShowLockOverlay();
        }

        private AppLockStateSnapshot BuildLockStateSnapshot()
        {
            var windows = Current.Windows.OfType<Window>().ToList();
            var visibleWindows = windows.Count(window => window.IsVisible);
            var mainWindow = MainWindow as MainWindow;
            var hasBlockingModal = windows.Any(window =>
                !ReferenceEquals(window, MainWindow)
                && window.IsVisible
                && window.Owner is not null);

            var sessionIsLocked = MainWindow is null
                || !MainWindow.IsVisible
                || MainWindow.WindowState == WindowState.Minimized;

            return new AppLockStateSnapshot
            {
                IsAlreadyLocked = _isLockScreenActive || (mainWindow?.IsLockOverlayVisible ?? false),
                HasBlockingModal = hasBlockingModal,
                IsShuttingDown = _authCoordinator.IsShuttingDown,
                SessionIsLocked = sessionIsLocked,
                MainWindowReady = mainWindow is not null && mainWindow.IsLoaded,
                MainWindowActive = mainWindow?.IsActive ?? false,
                VisibleWindowCount = visibleWindows
            };
        }

        private void ScheduleDeferredLockRetry()
        {
            if (_authCoordinator.IsShuttingDown || _isLockScreenActive)
            {
                return;
            }

            _deferredLockTimer ??= new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = _authCoordinator.DeferredRetryInterval
            };

            _deferredLockTimer.Interval = _authCoordinator.DeferredRetryInterval;

            _deferredLockTimer.Stop();
            _deferredLockTimer.Tick -= DeferredLockTimer_Tick;
            _deferredLockTimer.Tick += DeferredLockTimer_Tick;
            _deferredLockTimer.Start();
        }

        private void DeferredLockTimer_Tick(object? sender, EventArgs e)
        {
            _deferredLockTimer?.Stop();
            ProcessInactivityTimeout();
        }

        private void StopDeferredLockRetry()
        {
            if (_deferredLockTimer is not null)
            {
                _deferredLockTimer.Stop();
                _deferredLockTimer.Tick -= DeferredLockTimer_Tick;
            }
        }

        private void MainWindow_UnlockSucceeded(object? sender, EventArgs e)
        {
            _isLockScreenActive = false;
            _authCoordinator.MarkUnlocked();
            StopDeferredLockRetry();
            _inactivityLockService?.ResumeAfterUnlock();
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
            _authCoordinator.MarkShuttingDown();
            StopDeferredLockRetry();

            if (_inactivityLockService is not null)
            {
                _inactivityLockService.TimeoutElapsed -= InactivityLockService_TimeoutElapsed;
                _inactivityLockService.Stop();
                _inactivityLockService.Dispose();
                _inactivityLockService = null;
            }

            if (MainWindow is MainWindow mainWindow)
            {
                mainWindow.UnlockSucceeded -= MainWindow_UnlockSucceeded;
            }

            InputManager.Current.PreProcessInput -= Current_PreProcessInput;
            Activated -= App_Activated;
            Exit -= App_Exit;

            _securityLogger.Dispose();
        }
    }
}
