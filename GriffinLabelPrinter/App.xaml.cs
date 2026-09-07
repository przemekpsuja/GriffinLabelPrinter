using System;
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

            // Tymczasowo (brak dostępu do SQL) - towary czytane z pliku CSV w folderze template.
            // Docelowo: spraw, żeby SymfoniaService implementował też IProductCatalogService
            // (ma już GetAllProductsAsync) i podmień poniższą linię na tamtą implementację.
            var csvPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "towary.csv");
            IProductCatalogService productCatalogService = new CsvProductCatalogService(csvPath);

            // TODO: podmień na Twój BrotherBpacService z Fazy 2, gdy będzie gotowy
            IPrinterService printerService = new BrotherBpacService();

            var mainViewModel = new MainViewModel(symfoniaService, productCatalogService, printerService);
            var mainWindow = new MainWindow(mainViewModel);
            mainWindow.Show();
        }
    }
}
