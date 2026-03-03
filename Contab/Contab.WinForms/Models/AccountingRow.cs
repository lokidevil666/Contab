namespace Contab.WinForms.Models;

public sealed class AccountingRow
{
    public string Company { get; init; } = string.Empty;
    public string NumberXu { get; init; } = string.Empty;
    public string PostingKey { get; init; } = string.Empty;
    public string GlAccount { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string Division { get; init; } = string.Empty;
    public string Cpgt { get; init; } = string.Empty;
    public string Organization { get; init; } = string.Empty;
    public string Assignment { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string DocumentReference { get; init; } = string.Empty;
    public string BankCode { get; init; } = string.Empty;
    public DateTime? ValueDate { get; init; }
}
