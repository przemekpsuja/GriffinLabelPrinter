using System.Windows;
using GryfLabelManager.Services;
using GryfLabelManager.ViewModels;
using GryfLabelManager.Views;

namespace GryfLabelManager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // TODO: wczytaj z appsettings.json (Faza 3 z karty projektu) zamiast na sztywno.
            var connectionString = "Server=localhost;Database=Symfonia;Trusted_Connection=True;TrustServerCertificate=True;";

            ISymfoniaService symfoniaService = new SymfoniaService(connectionString);

            // TODO: podmień na Twój BrotherBpacService z Fazy 2, gdy będzie gotowy
            IPrinterService printerService = new MockPrinterService();

            var mainViewModel = new MainViewModel(symfoniaService, printerService);
            var mainWindow = new MainWindow(mainViewModel);
            mainWindow.Show();
        }
    }
}
