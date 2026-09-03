using System;
using System.IO;
using System.Runtime.InteropServices; // Marshal.ReleaseComObject
using bpac; // Interop.bpac.dll — reference to "Brother b-PAC 3.x Type Library" (COM)

namespace GryfLabelManager.Services
{
    public class BrotherBpacService : IPrinterService
    {
        // Object names INSIDE the .lbx template — must match the names given
        // in P-touch Editor when the label was designed.
        private const string BarcodeObjectName = "barcode1";
        private const string TextObjectName = "text1";

        // Relative path: Templates/<file>.lbx, resolved against the folder
        // where the .exe actually runs (bin/Debug or bin/Release), NOT the
        // solution/source folder. Make sure the file's "Copy to Output
        // Directory" property is set to "Copy if newer" in Visual Studio.
        private static readonly string TemplatePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Templates",
            "label_template.lbx");

        public void PrintLabel(string itemCode, string itemName, int copies)
        {
            // bpac.DocumentClass is the b-PAC "engine" — one instance = one print session.
            // NOTE: we can't do "new DocumentClass()" directly when the COM reference
            // has "Embed Interop Types = true" (CS1752). Creating it via Activator +
            // ProgID works around that without touching project settings.
            //
            // Both calls below are declared as returning a nullable type by the BCL
            // (Type? / object?), because COM registration can legitimately fail at
            // runtime (e.g. b-PAC not installed). Rather than suppressing the
            // nullable warnings with "!", we check explicitly and fail with a
            // message that actually tells you what's wrong.
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

            try
            {
                if (!File.Exists(TemplatePath))
                {
                    throw new FileNotFoundException(
                        $"Label template not found. Expected at: {TemplatePath}");
                }

                // 1. Open the .lbx template.
                bool opened = doc.Open(TemplatePath);
                if (!opened)
                {
                    throw new InvalidOperationException(
                        $"Failed to open label template: {TemplatePath}");
                }

                // 2. Overwrite the placeholder objects in the template.
                //    IMPORTANT: itemCode must stay a string — otherwise leading
                //    zeros (e.g. "0008...") get silently dropped if anything
                //    upstream treats the value as a number.
                //    GetObject returns null if the object name doesn't exist in the
                //    .lbx — almost always a typo vs. what was set in P-touch Editor.
                var barcodeObject = doc.GetObject(BarcodeObjectName)
                    ?? throw new InvalidOperationException(
                        $"Object '{BarcodeObjectName}' not found in the label template.");
                var textObject = doc.GetObject(TextObjectName)
                    ?? throw new InvalidOperationException(
                        $"Object '{TextObjectName}' not found in the label template.");

                barcodeObject.Text = itemCode;
                textObject.Text = itemName;

                // 3. Send to the printer.
                doc.StartPrint("", PrintOptionConstants.bpoDefault);
                doc.PrintOut(copies, PrintOptionConstants.bpoDefault);
                doc.EndPrint();
            }
            finally
            {
                // Always close the document — otherwise the bpac process
                // stays alive in memory (leak).
                doc.Close();

                // Since doc was created via Activator (late-bound COM object),
                // it's good practice to explicitly release the COM reference too.
                Marshal.ReleaseComObject(doc);
            }
        }

        /// <summary>
        /// Phase 2 — hardcoded smoke test: no UI, no SQL, just verifying the
        /// chain App -> b-PAC -> Brother GL-600 works at all.
        /// Call this temporarily from App.xaml.cs -> OnStartup().
        /// </summary>
        public void PrintHardcodedTest()
        {
            Console.WriteLine("Starting print test (Phase 2)...");

            try
            {
                PrintLabel(
                    itemCode: "0008110661N",   // hardcoded code, keeping the "N" and leading zeros
                    itemName: "TEST - Sruba M8x40 ocynk",
                    copies: 1
                );

                Console.WriteLine("OK: print job sent to Brother GL-600.");
            }
            catch (Exception ex)
            {
                // Most common causes at this stage:
                // - b-PAC COM library not registered (Brother driver/SDK not installed)
                // - wrong .lbx path / file missing from output folder
                // - printer off / disconnected / wrong Windows printer name
                Console.WriteLine($"Print test FAILED: {ex.Message}");
            }
        }
    }
}