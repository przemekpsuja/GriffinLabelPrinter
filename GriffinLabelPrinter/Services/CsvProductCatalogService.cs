using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GryfLabelManager.Models;

namespace GryfLabelManager.Services
{
    /// <summary>
    /// Wczytuje towary z pliku CSV (folder "template" w katalogu projektu).
    /// Oczekiwany format: pierwsza linia to nagłówek, kolumny Kod;Nazwa
    /// (albo Kod,Nazwa - delimiter wykrywany automatycznie z nagłówka).
    /// UWAGA: to rozwiązanie zastępcze na czas braku dostępu do SQL -
    /// docelowo podmień w App.xaml.cs na implementację SQL (np. SymfoniaService).
    /// </summary>
    public class CsvProductCatalogService : IProductCatalogService
    {
        private readonly string _csvPath;

        static CsvProductCatalogService()
        {
            // Rejestrujemy dostawcę stron kodowych, żeby Encoding.GetEncoding(1250) (Windows-1250,
            // typowy eksport polskiego Excela) działał również na .NET 5+/.NET Core, nie tylko .NET Framework.
            // Wymaga paczki NuGet: System.Text.Encoding.CodePages
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public CsvProductCatalogService(string csvPath)
        {
            _csvPath = csvPath;
        }

        public async Task<List<LabelItem>> GetAllProductsAsync()
        {
            var result = new List<LabelItem>();

            if (!File.Exists(_csvPath))
                throw new FileNotFoundException($"Nie znaleziono pliku towarów: {_csvPath}");

            var bytes = await File.ReadAllBytesAsync(_csvPath);
            var encoding = DetectEncoding(bytes);
            var content = encoding.GetString(bytes);
            var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            if (lines.Length == 0) return result;

            // wykrycie separatora na podstawie nagłówka - w polskim Excelu domyślnie ";"
            char delimiter = lines[0].Contains(';') ? ';' : ',';

            // pomijamy wiersz nagłówkowy (lines[0])
            foreach (var line in lines.Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(delimiter);
                if (parts.Length < 2) continue;

                result.Add(new LabelItem
                {
                    Kod = parts[0].Trim().Trim('"'),
                    Nazwa = parts[1].Trim().Trim('"'),
                    Ilosc = 1,
                    IsSelected = false,
                    IsManual = false
                });
            }

            return result;
        }

        /// <summary>
        /// Wykrywa kodowanie po BOM (Byte Order Mark). Jeśli BOM nie ma - zakładamy
        /// Windows-1250, bo tak najczęściej zapisuje polskie CSV Excel/Symfonia.
        /// </summary>
        private static Encoding DetectEncoding(byte[] bytes)
        {
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return Encoding.UTF8;
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
                return Encoding.Unicode;
            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
                return Encoding.BigEndianUnicode;

            return Encoding.GetEncoding(1250); // Windows-1250 (Środkowoeuropejski)
        }
    }
}