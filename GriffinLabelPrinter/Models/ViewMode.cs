namespace GryfLabelManager.Models
{
    /// <summary>Trzy tryby pracy głównego widoku.</summary>
    public enum ViewMode
    {
        Dokumenty,       // Lista dokumentów PZ/PW -> wybór -> pozycje dokumentu
        WszystkieTowary, // Cała kartoteka towarów z Symfonii
        Reczny           // Ręczny wpis kodu i nazwy (towar spoza stanu magazynowego)
    }
}
