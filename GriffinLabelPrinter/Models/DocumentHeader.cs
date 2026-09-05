using System;

namespace GryfLabelManager.Models
{
    /// <summary>
    /// Nagłówek dokumentu magazynowego (PZ lub PW) z Symfonii.
    /// UWAGA: nazwy kolumn (Numer, Typ, Data) to założenie - dopasuj
    /// do rzeczywistych nazw w Twoim schemacie Model.Dokumenty.
    /// </summary>
    public class DocumentHeader
    {
        public int Id { get; set; }
        public string Numer { get; set; }
        public string Typ { get; set; }   // "PZ" albo "PW"
        public DateTime Data { get; set; }

        // Wyświetlane wprost w ListBoxie/ComboBoxie w UI
        public override string ToString() => $"{Typ}  {Numer}   ({Data:yyyy-MM-dd})";
    }
}
