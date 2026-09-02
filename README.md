# GriffinLabelPrinter

A lightweight Windows desktop app that acts as a direct integrator between an ERP database and a Brother label printer — no more relying on crash-prone label editor software.

## 📌 Overview

**GriffinLabelPrinter** sits between **Symfonia Handel 2026.3** (ERP running on Microsoft SQL Server) and a **Brother GL-600** label printer.

### The problem
Brother's official software (*P-touch Editor 5.x/6.x*) freezes and crashes when connected to large databases containing thousands of items exported from Symfonia.

### The goal
Eliminate P-touch Editor from the daily printing workflow entirely. The app pulls data straight from the SQL server in the background, processes it in memory, and streams it to the Brother printer via the free **Brother b-PAC SDK**. The result should be fast, reliable, and warehouse-friendly.

---

## 🎨 Architecture & Tech Stack

Built following Clean Architecture principles and the **MVVM** pattern:

- **Language:** C# (.NET Framework 4.8 or .NET 8/9, WPF Application)
- **UI:** WPF styled with **WPF-UI** (native Windows 11 look — rounded corners, Fluent Design, NumberBox)
- **Business logic:** View (XAML) fully separated from control logic (ViewModel) via Data Binding (MVVM)
- **Database access:** `System.Data.SqlClient` / `Microsoft.Data.SqlClient` (native connection to the local Symfonia MS SQL server)
- **Hardware control:** COM reference to **Brother b-PAC 3.x Type Library** (`Interop.bpac.dll`)

---

## 📈 Workflow

1. **Startup screen** — loads the latest warehouse documents (PZ/PW) directly from the Symfonia database.
2. **Document selection** — user clicks a document; related line items load instantly.
3. **Data verification**
   - **Barcode:** generated as **Code 128** from the `Kod` column of the Symfonia item (leading zeros preserved, e.g. `0008110661N`)
   - **Human-readable text:** displayed automatically below the barcode
   - **Fixed graphic:** the label template (`.lbx`) has the "GRYF" logo baked in
4. **Interaction** — the warehouse worker checks the items to print, optionally adjusts the quantity per item, and clicks a single **"Print"** button.

---

## 🗄️ Database Mapping (SQL / Symfonia)

Data is retrieved from Symfonia's relational schema with the following query:

```sql
SELECT 
    t.Kod AS ItemCode,        -- e.g. 0008110661N (passed as String!)
    t.Nazwa AS ItemName,      -- Full name from the item catalog
    CAST(p.Ilosc AS INT) AS QuantityToPrint
FROM Model.PozycjeDokumentu p
INNER JOIN Model.Dokumenty d ON p.IdDokumentu = d.Id
INNER JOIN Model.Towary t ON p.IdTowaru = t.Id
WHERE d.Id = @SelectedDocumentId
```

---

## 🗂️ Project Structure (WPF MVVM)

```text
GriffinLabelPrinter/
├── Models/                 # Data models: Document.cs, LabelItem.cs
├── ViewModels/             # UI logic: MainViewModel.cs, BaseViewModel.cs
├── Views/                  # UI layer: MainWindow.xaml (WPF-UI)
├── Services/                # Abstraction layer: IPrinterService, ISymfoniaService
└── Helpers/                 # Helper mechanisms: RelayCommand.cs
```

---

## 🛠️ Roadmap / Milestones

- [x] **Phase 1:** Set up a clean WPF project in Visual Studio; add references to the b-PAC library and the WPF-UI NuGet package
- [ ] **Phase 2:** Implement `BrotherBpacService.cs` and run a first hardcoded print test, bypassing the UI
- [ ] **Phase 3:** Configure a secure MS SQL connection (`appsettings.json`) and fetch the first data
- [ ] **Phase 4:** Wire everything together in the XAML view (MVVM) and run final manual/automated tests

---

## Requirements

- Windows 10/11
- .NET 8/9 (or .NET Framework 4.8)
- Brother b-PAC SDK installed
- Access to the Symfonia Handel MS SQL Server instance

## License

MIT License. See [LICENSE](LICENSE) for details.
