using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using GryfLabelManager.Models;

namespace GryfLabelManager.Services
{
    public class SymfoniaService : ISymfoniaService
    {
        private readonly string _connectionString;

        public SymfoniaService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<DocumentHeader>> GetRecentDocumentsAsync()
        {
            var result = new List<DocumentHeader>();

            // UWAGA: nazwy kolumn Numer/Typ/Data to założenie na podstawie opisu projektu.
            // Dopasuj do rzeczywistych nazw kolumn w Model.Dokumenty (sprawdź np. w SSMS).
            const string sql = @"
                SELECT TOP 50 d.Id, d.Numer, d.Typ, d.Data
                FROM Model.Dokumenty d
                WHERE d.Typ IN ('PZ', 'PW')
                ORDER BY d.Data DESC";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new DocumentHeader
                {
                    Id = reader.GetInt32(0),
                    Numer = reader.GetString(1),
                    Typ = reader.GetString(2),
                    Data = reader.GetDateTime(3)
                });
            }
            return result;
        }

        public async Task<List<LabelItem>> GetDocumentItemsAsync(int documentId)
        {
            var result = new List<LabelItem>();

            // Zapytanie z karty projektu (Faza 4 z opisu), sparametryzowane przeciw SQL injection
            const string sql = @"
                SELECT
                    t.Kod AS KodTowaru,
                    t.Nazwa AS NazwaTowaru,
                    CAST(p.Ilosc AS INT) AS IloscDoDruku
                FROM Model.PozycjeDokumentu p
                INNER JOIN Model.Dokumenty d ON p.IdDokumentu = d.Id
                INNER JOIN Model.Towary t ON p.IdTowaru = t.Id
                WHERE d.Id = @IdWybranegoDokumentu";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@IdWybranegoDokumentu", documentId);
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new LabelItem
                {
                    Kod = reader.GetString(0),      // String! - zachowujemy wiodące zera
                    Nazwa = reader.GetString(1),
                    Ilosc = reader.GetInt32(2),
                    IsSelected = false,              // magazynier sam zaznacza co drukować
                    IsManual = false
                });
            }
            return result;
        }

        public async Task<List<LabelItem>> GetAllProductsAsync()
        {
            var result = new List<LabelItem>();

            // TOP 5000 jako bezpiecznik - przy bardzo dużej kartotece rozważ filtr/wyszukiwarkę w UI
            const string sql = @"
                SELECT TOP 5000 t.Kod, t.Nazwa
                FROM Model.Towary t
                ORDER BY t.Nazwa";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new LabelItem
                {
                    Kod = reader.GetString(0),
                    Nazwa = reader.GetString(1),
                    Ilosc = 1,
                    IsSelected = false,
                    IsManual = false
                });
            }
            return result;
        }
    }
}
