using Contab.WinForms.Models;
using Contab.WinForms.Utilities;

namespace Contab.WinForms.Services;

public sealed class ProcessingService
{
    public IReadOnlyList<AccountingRow> BuildRows(IReadOnlyList<LegacyTransaction> transactions, AppConfig? config)
    {
        if (transactions.Count == 0)
        {
            return Array.Empty<AccountingRow>();
        }

        var source = ShouldAggregate(config)
            ? Aggregate(transactions)
            : transactions;

        var rows = new List<AccountingRow>(source.Count);
        var sequence = 1;

        foreach (var tx in source)
        {
            var bankCode = LegacyStringUtils.GetBankCode(tx.AccountNib);
            var company = tx.Company;

            rows.Add(new AccountingRow
            {
                Company = company,
                NumberXu = string.IsNullOrWhiteSpace(tx.NumberXu)
                    ? $"X{sequence.ToString().PadLeft(11, '0')}"
                    : tx.NumberXu,
                PostingKey = tx.Amount < 0m ? "50" : "40",
                GlAccount = tx.AccountCode,
                Amount = tx.Amount,
                Currency = tx.Currency,
                Division = company.Length >= 2 ? company[..2] : company,
                Cpgt = bankCode,
                Organization = company,
                Assignment = tx.FlowCode,
                Description = BuildDescription(tx, config),
                DocumentReference = tx.ChequeNumber,
                BankCode = bankCode,
                ValueDate = tx.ValueDate
            });

            sequence++;
        }

        return rows;
    }

    private static bool ShouldAggregate(AppConfig? config)
    {
        if (config is null)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(config.Aggregator) &&
               !config.Aggregator.Equals("Sem Agregacao", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<LegacyTransaction> Aggregate(IReadOnlyList<LegacyTransaction> transactions)
    {
        return transactions
            .GroupBy(t => new { t.AccountCode, t.FlowCode, t.Company, t.Currency, t.AccountNib })
            .Select(g =>
            {
                var first = g.First();
                return new LegacyTransaction
                {
                    RecordId = first.RecordId,
                    ImportDate = first.ImportDate,
                    FlowCode = first.FlowCode,
                    BookDate = first.BookDate,
                    Description = first.Description,
                    ChequeNumber = first.ChequeNumber,
                    ValueDate = first.ValueDate,
                    AccountCode = first.AccountCode,
                    AccountNib = first.AccountNib,
                    Amount = g.Sum(x => x.Amount),
                    Currency = first.Currency,
                    Company = first.Company
                };
            })
            .ToList();
    }

    private static string BuildDescription(LegacyTransaction tx, AppConfig? config)
    {
        var description = tx.Description ?? string.Empty;
        if (description.Length > 42)
        {
            description = description[..42];
        }

        if (config is null)
        {
            return description;
        }

        var mode = (config.ErpDescriptionMode ?? "GERAL").Trim().ToUpperInvariant();
        return mode switch
        {
            "DESC" => description,
            "DESC2" => $"{tx.FlowCode} {description}".Trim().Length > 42
                ? $"{tx.FlowCode} {description}".Trim()[..42]
                : $"{tx.FlowCode} {description}".Trim(),
            _ => description
        };
    }
}
