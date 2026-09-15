using GryfLabelManager.Services;
using GryfLabelManager.ViewModels;
using GryfLabelManager.Views;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Microsoft.Extensions.Configuration;
using Wpf.Ui.Appearance;

namespace GryfLabelManager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ApplicationThemeManager.ApplySystemTheme();

            var connectionString = LoadConnectionString();

            var symfoniaServiceImpl = new SymfoniaService(connectionString);

            ISymfoniaService symfoniaService = symfoniaServiceImpl;
            IProductCatalogService productCatalogService = symfoniaServiceImpl;

            IPrinterService printerService = new BrotherBpacService();

            // Sets the app-wide accent color to the classic Windows blue (#0078D4)
            ApplicationAccentColorManager.Apply(Color.FromRgb(0x00, 0x78, 0xD4));

            var mainViewModel = new MainViewModel(symfoniaService, productCatalogService, printerService);
            var mainWindow = new MainWindow(mainViewModel);

            // Nasłuchuje zmiany motywu Windows (Ustawienia -> Personalizacja -> Kolory)
            // i automatycznie przełącza appkę bez restartu.
            SystemThemeWatcher.Watch(mainWindow);

            mainWindow.Show();
        }

        /// <summary>
        /// Reads the Symfonia connection string from appsettings.json.
        /// Shows a clear error instead of crashing silently if the file
        /// (or the expected key) is missing - this is the file every new
        /// dev has to create locally from appsettings.example.json.
        /// </summary>
        private static string LoadConnectionString()
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var configPath = Path.Combine(basePath, "appsettings.json");

            if (!File.Exists(configPath))
            {
                MessageBox.Show(
                    "Brak pliku appsettings.json.\n\n" +
                    "Skopiuj appsettings.example.json -> appsettings.json " +
                    "i uzupełnij dane połączenia do bazy Symfonii.",
                    "Brak konfiguracji", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }

            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var connectionString = config.GetConnectionString("Symfonia");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                MessageBox.Show(
                    "W appsettings.json brakuje klucza ConnectionStrings:Symfonia.",
                    "Błędna konfiguracja", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }

            return connectionString!;
        }
    }
}