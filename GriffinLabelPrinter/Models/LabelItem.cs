using GryfLabelManager.ViewModels;

namespace GryfLabelManager.Models
{
    /// <summary>
    /// Pojedynczy wiersz w siatce do druku. Ten sam model jest używany
    /// niezależnie od trybu (dokument PZ/PW, wszystkie towary, wpis ręczny),
    /// dzięki czemu logika drukowania jest jedna dla całej aplikacji.
    /// </summary>
    public class LabelItem : BaseViewModel
    {
        // Kod towaru - String! (Symfonia przechowuje kody z wiodącymi zerami, np. 0008110661N)
        public string Kod { get; set; }

        public string Nazwa { get; set; }

        private int _ilosc = 1;
        public int Ilosc
        {
            get => _ilosc;
            set
            {
                // Nie pozwalamy zejść poniżej 1 - po co drukować 0 etykiet
                if (value < 1) value = 1;
                _ilosc = value;
                OnPropertyChanged();
            }
        }

        private bool _isSelected = true;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        // True, jeśli pozycja pochodzi z ręcznego wpisu (nie ma jej w Symfonii).
        // Przydatne np. gdybyśmy chcieli inaczej oznaczyć takie wiersze w UI.
        public bool IsManual { get; set; }
        public int IloscDoDruku { get; internal set; }
    }
}
