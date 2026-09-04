using System.Collections.Generic;
using System.Threading.Tasks;
using GryfLabelManager.Models;

namespace GryfLabelManager.Services
{
    // Abstrakcja źródła danych towarowych.
    // Dzięki temu ViewModel nie wie i nie musi wiedzieć, czy dane pochodzą
    // z CSV (teraz) czy z SQL Symfonii (docelowo w Fazie 3b, po odblokowaniu dostępu).
    // Podmiana implementacji = jedna linijka w App.xaml.cs, zero zmian w ViewModelu.
    public interface ISymfoniaService
    {
        Task<List<LabelItem>> GetItemsAsync();
    }
}
