using System;
using System.Windows;
using InvoiceGenerator.Services;
using InvoiceGenerator.Views;

namespace InvoiceGenerator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
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

                var mainWindow = new MainWindow();
                MainWindow = mainWindow;
                ShutdownMode = ShutdownMode.OnMainWindowClose;
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing application: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}", 
                    "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }
    }
}
