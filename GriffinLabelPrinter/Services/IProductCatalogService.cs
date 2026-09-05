using System.Collections.Generic;
using System.Threading.Tasks;
using GryfLabelManager.Models;

namespace GryfLabelManager.Services
{
    /// <summary>
    /// Źródło pełnej listy towarów dla zakładki "Wszystkie towary".
    /// Oddzielone od ISymfoniaService, żeby teraz (bez dostępu do SQL) móc
    /// podpiąć CsvProductCatalogService, a docelowo podmienić na wersję SQL
    /// bez zmiany reszty aplikacji.
    /// </summary>
    public interface IProductCatalogService
    {
        Task<List<LabelItem>> GetAllProductsAsync();
    }
}
