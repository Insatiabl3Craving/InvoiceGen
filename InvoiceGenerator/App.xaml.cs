using System.Windows;
using InvoiceGenerator.Services;

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

            // Ensure database and tables exist before any view loads.
            var settingsService = new SettingsService();
            settingsService.InitializeDatabaseAsync().GetAwaiter().GetResult();
        }
    }
}
