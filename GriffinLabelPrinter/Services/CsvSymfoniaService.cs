using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GryfLabelManager.Models;

namespace GryfLabelManager.Services
{
    // Tymczasowe źródło danych (do czasu odblokowania dostępu do SQL Symfonii).
    // Czyta plik CSV wyeksportowany ręcznie z Symfonii w formacie:
    //   Kod;Nazwa
    //   Karnet 24-godz. 12 zł;Karnet 24-godz. 12zł
    //
    // Uwaga: separator to średnik ";", bo Excel w polskich ustawieniach regionalnych
    // używa przecinka jako separatora dziesiętnego, więc mimo nazwy "CSV (przecinki)"
    // faktycznie eksportuje ze średnikiem.
    public class CsvSymfoniaService : ISymfoniaService
    {
        private readonly string _csvPath;
        private List<LabelItem>? _cache; // wczytujemy raz, trzymamy w pamięci na czas działania aplikacji

        public CsvSymfoniaService(string csvPath)
        {
            _csvPath = csvPath;
        }

        public Task<List<LabelItem>> GetAllProductsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<List<LabelItem>> GetDocumentItemsAsync(int documentId)
        {
            throw new NotImplementedException();
        }

        public async Task<List<LabelItem>> GetItemsAsync()
        {
            _cache ??= await LoadFromCsvAsync();
            return _cache;
        }

        public Task<List<DocumentHeader>> GetRecentDocumentsAsync()
        {
            throw new NotImplementedException();
        }

        private async Task<List<LabelItem>> LoadFromCsvAsync()
        {
            if (!File.Exists(_csvPath))
                throw new FileNotFoundException($"Nie znaleziono pliku CSV: {_csvPath}");

            // UTF-8 zakładamy jako docelowy format eksportu (zawiera polskie znaki poprawnie).
            // Jeśli plik jednak okaże się w Windows-1250 (stare Excele bez opcji "CSV UTF-8"),
            // zamień poniżej na: Encoding.GetEncoding(1250)
            var lines = await File.ReadAllLinesAsync(_csvPath, Encoding.UTF8);

            return lines
                .Skip(1) // pomijamy wiersz nagłówka "Kod;Nazwa"
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(ParseLine)
                .Where(item => item != null)
                .Select(item => item!)
                .ToList();
        }

        private LabelItem? ParseLine(string line)
        {
            var cols = line.Split(';');
            if (cols.Length < 2)
                return null; // pomijamy uszkodzone/niekompletne wiersze zamiast wywalać aplikację

            return new LabelItem
            {
                Kod = cols[0].Trim(),
                Nazwa = cols[1].Trim(),
                IloscDoDruku = 1 // brak kolumny Ilosc w tym eksporcie - użytkownik ustawi ręcznie w UI
            };
        }
    }
}