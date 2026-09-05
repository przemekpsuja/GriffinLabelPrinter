using System.Collections.Generic;
using GryfLabelManager.Models;

namespace GryfLabelManager.Services
{
    public interface IPrinterService
    {
        /// <summary>Drukuje przekazane pozycje na Brother GL-600 przez b-PAC.</summary>
        void Print(IEnumerable<LabelItem> items);
    }

    /// <summary>
    /// Tymczasowa atrapa - podmień na Twój BrotherBpacService z Fazy 2
    /// (ten z referencją COM do Interop.bpac.dll). Trzymam ją tutaj tylko
    /// żeby MainViewModel dało się skompilować i przetestować widoki bez drukarki.
    /// </summary>
    public class MockPrinterService : IPrinterService
    {
        public void Print(IEnumerable<LabelItem> items)
        {
            foreach (var item in items)
                System.Diagnostics.Debug.WriteLine($"[MOCK PRINT] {item.Kod} x{item.Ilosc} - {item.Nazwa}");
        }
    }
}
