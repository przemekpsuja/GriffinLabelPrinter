using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices; // Marshal.ReleaseComObject
using System.Windows; // MessageBox
using bpac;
using GryfLabelManager.Models; // Interop.bpac.dll — reference to "Brother b-PAC 3.x Type Library" (COM)

namespace GryfLabelManager.Services
{
    public class BrotherBpacService : IPrinterService
    {
        private const string BarcodeObjectName = "Barcode";
        private const string TextObjectName = "Description";

        private static readonly string TemplatePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Templates",
            "new_label.lbx");

        /// <summary>
        /// GŁÓWNA METODA DRUKU. Otwiera dokument i wywołuje StartPrint() TYLKO RAZ,
        /// niezależnie od liczby pozycji — to jest różnica względem starego PrintLabel,
        /// które robiło Open()...Close() osobno dla każdej etykiety (wolne przy 100+ pozycjach,
        /// bo każde Open/Close to osobna "rozmowa" z COM/sterownikiem drukarki).
        ///
        /// Tutaj: Open() raz -> StartPrint() raz -> pętla (podmień tekst + PrintOut) -> EndPrint() raz -> Close() raz.
        /// </summary>
        public void Print(IEnumerable<LabelItem> items)
        {
            var lista = items?.ToList() ?? new List<LabelItem>();
            if (lista.Count == 0) return;

            Type? documentType = Type.GetTypeFromProgID("bpac.Document");
            if (documentType is null)
            {
                throw new InvalidOperationException(
                    "COM type 'bpac.Document' not found. Is Brother b-PAC SDK installed and registered?");
            }

            object? instance = Activator.CreateInstance(documentType);
            if (instance is null)
            {
                throw new InvalidOperationException(
                    "Failed to create an instance of 'bpac.Document' via Activator.");
            }

            Document doc = (Document)instance;

            // Flaga potrzebna w finally: jeśli StartPrint() się nie udał (albo wywalił wyjątek
            // wcześniej), nie chcemy wołać EndPrint() na sesji, która nigdy się nie zaczęła.
            bool printStarted = false;

            try
            {
                if (!File.Exists(TemplatePath))
                {
                    throw new FileNotFoundException(
                        $"Label template not found. Expected at: {TemplatePath}");
                }

                // Open() - RAZ na całą partię
                if (!doc.Open(TemplatePath))
                {
                    throw new InvalidOperationException(
                        $"Failed to open label template: {TemplatePath}");
                }

                var barcodeObject = doc.GetObject(BarcodeObjectName)
                    ?? throw new InvalidOperationException(
                        $"Object '{BarcodeObjectName}' not found in the label template.");
                var textObject = doc.GetObject(TextObjectName)
                    ?? throw new InvalidOperationException(
                        $"Object '{TextObjectName}' not found in the label template.");

                // StartPrint() - RAZ, otwiera "sesję druku" na drukarce.
                if (!doc.StartPrint("", PrintOptionConstants.bpoDefault))
                {
                    throw new InvalidOperationException("StartPrint failed — printer not ready?");
                }
                printStarted = true;

                // Od tego miejsca każda kolejna etykieta to TYLKO podmiana tekstu w już
                // otwartym dokumencie + PrintOut. Bez ponownego Open/StartPrint.
                foreach (var item in lista)
                {
                    // itemCode musi zostać stringiem — inaczej wiodące zera (np. "0008...")
                    // mogłyby zniknąć, gdyby coś po drodze potraktowało wartość jako liczbę.
                    barcodeObject.Text = item.Kod;
                    textObject.Text = item.Nazwa;

                    doc.PrintOut(item.Ilosc, PrintOptionConstants.bpoDefault);
                }
            }
            finally
            {
                // Zamykamy sesję druku i dokument niezależnie od tego, czy pętla się
                // wykonała w całości, czy wyjątek przerwał ją w środku (np. na 50. z 200 etykiet).
                if (printStarted)
                    doc.EndPrint();

                doc.Close();
                Marshal.ReleaseComObject(doc);
            }
        }

        /// <summary>
        /// Zachowane dla wygody (np. testu z Fazy 2) — teraz to tylko cienki wrapper
        /// na Print(), więc cała logika COM istnieje w jednym miejscu.
        /// </summary>
        public void PrintLabel(string itemCode, string itemName, int copies)
        {
            Print(new List<LabelItem>
            {
                new LabelItem { Kod = itemCode, Nazwa = itemName, Ilosc = copies }
            });
        }

        /// <summary>
        /// Diagnostic helper — bez zmian względem poprzedniej wersji.
        /// </summary>
        public void ListTemplateObjects()
        {
            Type? documentType = Type.GetTypeFromProgID("bpac.Document");
            if (documentType is null)
            {
                MessageBox.Show("COM type 'bpac.Document' not found.", "Diagnostics — FAILED");
                return;
            }

            object? instance = Activator.CreateInstance(documentType);
            if (instance is null)
            {
                MessageBox.Show("Failed to create bpac.Document instance.", "Diagnostics — FAILED");
                return;
            }

            Document doc = (Document)instance;

            try
            {
                if (!File.Exists(TemplatePath))
                {
                    MessageBox.Show($"Template not found at: {TemplatePath}", "Diagnostics — FAILED");
                    return;
                }

                if (!doc.Open(TemplatePath))
                {
                    MessageBox.Show($"Failed to open template: {TemplatePath}", "Diagnostics — FAILED");
                    return;
                }

                var lines = new System.Text.StringBuilder();
                foreach (dynamic obj in doc.Objects)
                {
                    lines.AppendLine($"Name: '{obj.Name}'   Type: {obj.Type}");
                }

                MessageBox.Show(
                    lines.Length > 0 ? lines.ToString() : "Template has no objects at all.",
                    "Objects found in template",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            finally
            {
                doc.Close();
                Marshal.ReleaseComObject(doc);
            }
        }

        /// <summary>
        /// Phase 2 — hardcoded smoke test. Bez zmian w wywołaniu, ale teraz
        /// pod spodem korzysta z tej samej ścieżki co prawdziwy druk z UI.
        /// </summary>
        public void PrintHardcodedTest()
        {
            try
            {
                PrintLabel(
                    itemCode: "0008110661N",
                    itemName: "TEST - Sruba M8x40 ocynk",
                    copies: 1
                );

                MessageBox.Show(
                    "Print job sent to Brother GL-600.",
                    "Print test — OK",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Print test — FAILED",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}