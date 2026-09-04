using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GryfLabelManager.Models
{
    // Reprezentuje jeden towar na liście. Implementuje INotifyPropertyChanged,
    // żeby checkbox (IsSelected) i pole ilości (IloscDoDruku) w UI aktualizowały się
    // automatycznie przy zmianie (two-way binding w WPF).
    public class LabelItem : INotifyPropertyChanged
    {
        // WAŻNE: Kod jako string, nie int! Symfonia ma kody z wiodącymi zerami
        // (np. "0008110661N") - int by je ucięło.
        public string Kod { get; set; } = string.Empty;

        public string Nazwa { get; set; } = string.Empty;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        private int _iloscDoDruku = 1;
        public int IloscDoDruku
        {
            get => _iloscDoDruku;
            set { _iloscDoDruku = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}