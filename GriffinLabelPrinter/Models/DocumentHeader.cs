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
        public string Numer { get; set; }   // HM.MG.kod - user-facing document number
        public string Typ { get; set; }     // HM.MG.typ_dk - 'PZ' or 'PW'
        public string Nazwa { get; set; }   // HM.MG.nazwa - document type label
        public DateTime Data { get; set; }

        public override string ToString() => $"{Typ}  {Numer}   ({Data:yyyy-MM-dd})";
    }
}
