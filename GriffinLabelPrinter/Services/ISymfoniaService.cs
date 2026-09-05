using System.Collections.Generic;
using System.Threading.Tasks;
using GryfLabelManager.Models;

namespace GryfLabelManager.Services
{
    public interface ISymfoniaService
    {
        /// <summary>Ostatnie dokumenty PZ/PW - do trybu "Dokumenty".</summary>
        Task<List<DocumentHeader>> GetRecentDocumentsAsync();

        /// <summary>Pozycje wybranego dokumentu.</summary>
        Task<List<LabelItem>> GetDocumentItemsAsync(int documentId);

        /// <summary>Cała kartoteka towarów - do trybu "Wszystkie towary".</summary>
        Task<List<LabelItem>> GetAllProductsAsync();
    }
}
