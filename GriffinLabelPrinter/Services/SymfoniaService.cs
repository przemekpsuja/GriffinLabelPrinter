using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using GryfLabelManager.Models;

namespace GryfLabelManager.Services
{
    /// <summary>
    /// Reads warehouse documents and items directly from the Symfonia MS SQL
    /// database. Confirmed real schema (see Struktura_SQL.txt):
    ///   HM.MG - document headers (id, kod, typ_dk, nazwa, data)
    ///   HM.MZ - document lines (id, super -> MG.id, idtw -> TW.id, kod, ilosc, cena)
    ///   HM.TW - product catalog (id, kod, nazwa)
    /// </summary>
    public class SymfoniaService : ISymfoniaService, IProductCatalogService
    {
        private readonly string _connectionString;

        public SymfoniaService(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>Loads the 150 most recent PZ/PW document headers from HM.MG.</summary>
        public async Task<List<DocumentHeader>> GetRecentDocumentsAsync()
        {
            var result = new List<DocumentHeader>();

            const string sql = @"SELECT TOP 50 mg.id, mg.kod, mg.typ_dk, mg.nazwa, mg.data, sc.Name AS Kontrahent
                    FROM HM.MG mg
                    LEFT JOIN SSCommon.STContractors sc ON mg.khid = sc.Id
                    WHERE mg.typ_dk IN ('PZ', 'PW')
                    ORDER BY mg.data DESC;";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new DocumentHeader
                {
                    Id = reader.GetInt32(0),
                    Numer = reader.GetString(1),   // HM.MG.kod - user-facing document number, e.g. PZ/2026/09/15/72
                    Typ = reader.GetString(2),     // HM.MG.typ_dk - 'PZ' or 'PW'
                    Data = reader.GetDateTime(4),
                    Kontrahent = reader.IsDBNull(5) ? "—" : reader.GetString(5)
                });
            }
            return result;
        }

        /// <summary>
        /// Loads all lines for a given document from HM.MZ, joined with HM.TW
        /// for the full product name. ilosc can be negative in Symfonia (depending
        /// on document direction), so it's wrapped in ABS().
        /// </summary>
        public async Task<List<LabelItem>> GetDocumentItemsAsync(int documentId)
        {
            var result = new List<LabelItem>();

            const string sql = @"
                SELECT
                    mz.kod AS KodTowaru,
                    tw.nazwa AS NazwaTowaru,
                    CAST(ABS(mz.ilosc) AS INT) AS Ilosc
                FROM HM.MZ mz
                JOIN HM.TW tw ON mz.idtw = tw.id
                WHERE mz.super = @IdDokumentu";

            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@IdDokumentu", documentId);
            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new LabelItem
                {
                    Kod = reader.GetString(0),      // string! - keeps leading zeros, e.g. 0008110661N
                    Nazwa = reader.GetString(1),
                    Ilosc = reader.GetInt32(2),
                    IsSelected = false,              // warehouse worker checks items to print manually
                    IsManual = false
                });
            }
            return result;
        }

        /// <summary>Loads the full product catalog from HM.TW for the "All products" tab.</summary>
        public async Task<List<LabelItem>> GetAllProductsAsync()
        {
            var result = new List<LabelItem>();

            // TOP 5000 as a safety cap - with a very large catalog, consider filtering in SQL too
            const string sql = @"
                SELECT kod, nazwa
                FROM HM.TW
                ORDER BY kod";

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