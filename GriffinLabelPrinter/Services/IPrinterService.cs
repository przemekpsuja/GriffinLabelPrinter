namespace GryfLabelManager.Services
{
    /// <summary>
    /// Abstraction over the label printer. The ViewModel talks only to this
    /// interface, so the b-PAC implementation stays swappable/testable.
    /// </summary>
    public interface IPrinterService
    {
        /// <summary>
        /// Prints a single label based on data pulled from Symfonia.
        /// </summary>
        /// <param name="itemCode">Barcode value (Code128), e.g. "0008110661N"</param>
        /// <param name="itemName">Human-readable text printed under the barcode</param>
        /// <param name="copies">Number of label copies to print</param>
        void PrintLabel(string itemCode, string itemName, int copies);

        /// <summary>
        /// Hardcoded smoke test — Phase 2, no UI involved.
        /// </summary>
        void PrintHardcodedTest();
    }
}
