# Simple guide (for non-advanced programmers)

This project was organized to be easy to follow.

## 1) Where the app starts

- `Program.cs`  
  Starts the application.

- `Forms/SplashForm.cs`  
  Small intro screen.

- `Forms/MainForm.cs`  
  Main screen where you load config/data and export.

## 2) What each service does

- `Services/LegacyConfigReader.cs`  
  Reads `contab.ini`.

- `Services/StructureDefinitionReader.cs`  
  Reads `contab.str`.

- `Services/LegacyDataRepository.cs`  
  Loads pending transactions from SQL Server.

- `Services/InputFileTransactionReader.cs`  
  Loads transactions from a text file using the `IN` structure.

- `Services/ProcessingService.cs`  
  Converts transactions into accounting rows.

- `Services/FixedWidthExporter.cs`  
  Creates the final output file with fixed-width columns.

## 3) Typical flow in the UI

1. Click **Load Config**
2. Click **Load Structure**
3. Click **Load from DB** (or **Load from File**)
4. Click **Process + Export**

## 4) If something fails

The bottom log panel shows each step and error message with time.

## 5) Safe places to start editing

If you want to customize behavior, start here:

- `ProcessingService.cs` (business rules)
- `FixedWidthExporter.cs` (output format)
- `MainForm.cs` (button flow and UI behavior)
