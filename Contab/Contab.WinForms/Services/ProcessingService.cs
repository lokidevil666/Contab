using Contab.WinForms.Models;
using Contab.WinForms.Utilities;

namespace Contab.WinForms.Services;

public sealed class ProcessingService
{
    public IReadOnlyList<AccountingRow> BuildRows(IReadOnlyList<LegacyTransaction> transactions, AppConfig? config)
    {
        if (transactions.Count == 0)
        {
            return new List<AccountingRow>();
        }

        List<LegacyTransaction> source;
        if (ShouldAggregate(config))
        {
            source = AggregateTransactions(transactions);
        }
        else
        {
            source = new List<LegacyTransaction>(transactions);
        }

        List<AccountingRow> rows = new List<AccountingRow>(source.Count);
        int sequence = 1;

        foreach (LegacyTransaction tx in source)
        {
            string bankCode = LegacyStringUtils.GetBankCode(tx.AccountNib);
            string company = tx.Company;
            string division = company.Length >= 2 ? company.Substring(0, 2) : company;
            string numberXu;

            if (string.IsNullOrWhiteSpace(tx.NumberXu))
            {
                numberXu = "X" + sequence.ToString().PadLeft(11, '0');
            }
            else
            {
                numberXu = tx.NumberXu;
            }

            string postingKey = tx.Amount < 0m ? "50" : "40";

            AccountingRow row = new AccountingRow
            {
                Company = company,
                NumberXu = numberXu,
                PostingKey = postingKey,
                GlAccount = tx.AccountCode,
                Amount = tx.Amount,
                Currency = tx.Currency,
                Division = division,
                Cpgt = bankCode,
                Organization = company,
                Assignment = tx.FlowCode,
                Description = BuildDescription(tx, config),
                DocumentReference = tx.ChequeNumber,
                BankCode = bankCode,
                ValueDate = tx.ValueDate
            };

            rows.Add(row);

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

    private static List<LegacyTransaction> AggregateTransactions(IReadOnlyList<LegacyTransaction> transactions)
    {
        Dictionary<string, LegacyTransaction> aggregated = new Dictionary<string, LegacyTransaction>(StringComparer.OrdinalIgnoreCase);

        foreach (LegacyTransaction tx in transactions)
        {
            string key = string.Join("|",
                tx.AccountCode ?? string.Empty,
                tx.FlowCode ?? string.Empty,
                tx.Company ?? string.Empty,
                tx.Currency ?? string.Empty,
                tx.AccountNib ?? string.Empty);

            if (!aggregated.ContainsKey(key))
            {
                LegacyTransaction first = new LegacyTransaction
                {
                    RecordId = tx.RecordId,
                    ImportDate = tx.ImportDate,
                    FlowCode = tx.FlowCode,
                    BookDate = tx.BookDate,
                    Description = tx.Description,
                    ChequeNumber = tx.ChequeNumber,
                    ValueDate = tx.ValueDate,
                    AccountCode = tx.AccountCode,
                    AccountNib = tx.AccountNib,
                    Amount = tx.Amount,
                    Currency = tx.Currency,
                    Company = tx.Company
                };

                aggregated[key] = first;
            }
            else
            {
                LegacyTransaction old = aggregated[key];
                LegacyTransaction updated = new LegacyTransaction
                {
                    RecordId = old.RecordId,
                    ImportDate = old.ImportDate,
                    FlowCode = old.FlowCode,
                    BookDate = old.BookDate,
                    Description = old.Description,
                    ChequeNumber = old.ChequeNumber,
                    ValueDate = old.ValueDate,
                    AccountCode = old.AccountCode,
                    AccountNib = old.AccountNib,
                    Amount = old.Amount + tx.Amount,
                    Currency = old.Currency,
                    Company = old.Company
                };

                aggregated[key] = updated;
            }
        }

        List<LegacyTransaction> list = new List<LegacyTransaction>(aggregated.Count);
        foreach (KeyValuePair<string, LegacyTransaction> pair in aggregated)
        {
            list.Add(pair.Value);
        }

        return list;
    }

    private static string BuildDescription(LegacyTransaction tx, AppConfig? config)
    {
        string description = tx.Description ?? string.Empty;
        if (description.Length > 42)
        {
            description = description.Substring(0, 42);
        }

        if (config is null)
        {
            return description;
        }

        string mode = (config.ErpDescriptionMode ?? "GERAL").Trim().ToUpperInvariant();
        if (mode == "DESC")
        {
            return description;
        }

        if (mode == "DESC2")
        {
            string combined = ((tx.FlowCode ?? string.Empty) + " " + description).Trim();
            if (combined.Length > 42)
            {
                return combined.Substring(0, 42);
            }

            return combined;
        }

        return description;
    }
}
