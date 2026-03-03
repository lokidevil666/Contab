namespace Contab.WinForms.Models;

public sealed class LegacyTransaction
{
    public string RecordId { get; init; } = string.Empty;
    public DateTime? ImportDate { get; init; }
    public string FlowCode { get; init; } = string.Empty;
    public DateTime? BookDate { get; init; }
    public string Description { get; init; } = string.Empty;
    public string ChequeNumber { get; init; } = string.Empty;
    public DateTime? ValueDate { get; init; }
    public string AccountCode { get; init; } = string.Empty;
    public string AccountNib { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string Company { get; init; } = string.Empty;

    public string NumberXu => string.IsNullOrWhiteSpace(RecordId) ? string.Empty : $"X{RecordId.Trim()}";
}
