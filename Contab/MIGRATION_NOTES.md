# VB6 -> C# WinForms migration notes

## What was delivered

A new C# Windows application was added without deleting or changing the original VB6 sources:

- **Solution:** `Contab/Contab.WinForms.sln`
- **Project:** `Contab/Contab.WinForms/Contab.WinForms.csproj`

### Core migrated behavior

1. **Legacy INI parsing (`contab.ini`)**
   - Keeps positional mapping used in VB6.
   - Supports legacy encrypted password format (`!` + custom cipher).

2. **Legacy structure parsing (`contab.str`)**
   - Reads `IN`, `OUT`, and `HDR` sections.
   - Produces an in-memory structure model used for output generation.

3. **Processing pipeline**
   - Load transactions from:
     - SQL Server (`nrec_bank` / fallback `cash_ledger`), or
     - Input file + `IN` structure map.
   - Transform transactions into accounting rows.
   - Optional aggregation mode (when configured) for better throughput.

4. **Fixed-width export**
   - Generates output lines using `OUT` and `HDR` field definitions from `contab.str`.

5. **UI migration baseline**
   - Splash screen (`FrmIntro` equivalent),
   - Main processing form (`frmProses` baseline),
   - About window (`frmAbout` equivalent).

## Performance and maintainability improvements

- Replaced VB6 global-state/event-heavy flow with service classes.
- Async database I/O (`async/await`) to keep UI responsive.
- Parameterized SQL commands (safer than string concatenation).
- Clear model-based pipeline (`Config -> Structure -> Transactions -> AccountingRows -> Export`).

## Out-of-scope or specialized legacy behavior

The original VB6 `frmProses.frm` is very large and includes many client-specific routines (SAP RFC COM objects, OCX controls, special SQL exports, and dozens of integration variants).  
This baseline migration does **not** yet implement all specialized legacy branches one-to-one.

## Build and run

1. Open `Contab/Contab.WinForms.sln` in Visual Studio 2022+ on Windows.
2. Restore/build (target framework: `net8.0-windows`).
3. Start the app.
4. Load `contab.ini` and `contab.str`, then load data from DB or file and export.
