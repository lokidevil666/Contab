namespace Contab.WinForms.Models;

public sealed record ExportResult(
    string FilePath,
    int LineCount,
    int RecordCount
);
