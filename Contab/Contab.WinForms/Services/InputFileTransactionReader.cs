using System.Globalization;
using Contab.WinForms.Models;
using Contab.WinForms.Utilities;

namespace Contab.WinForms.Services;

public sealed class InputFileTransactionReader
{
    public IReadOnlyList<LegacyTransaction> Read(string filePath, IReadOnlyList<StructureField> inputFields)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Input file path cannot be empty.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Input file not found.", filePath);
        }

        if (inputFields.Count == 0)
        {
            throw new InvalidOperationException("Input structure is empty. Load contab.str first.");
        }

        var transactions = new List<LegacyTransaction>(1024);
        var row = 1;

        foreach (var line in File.ReadLines(filePath))
        {
            if (line.Length < 2 || !line.StartsWith("04", StringComparison.Ordinal))
            {
                continue;
            }

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in inputFields)
            {
                values[LegacyStringUtils.NormalizeIdentifier(field.Name)] = Extract(line, field.Start, field.Length);
            }

            var recIdRaw = FirstNonEmpty(values, "NUMEROXU", "NUMXU", "NUMXU", "NUMERO");
            var recId = recIdRaw.StartsWith("X", StringComparison.OrdinalIgnoreCase) ? recIdRaw[1..] : recIdRaw;

            var amountRaw = FirstNonEmpty(values, "MONTANTE", "AMOUNT", "VALOR");
            var amount = ParseAmount(amountRaw);

            transactions.Add(new LegacyTransaction
            {
                RecordId = string.IsNullOrWhiteSpace(recId) ? row.ToString(CultureInfo.InvariantCulture) : recId.Trim(),
                ImportDate = ParseDate(FirstNonEmpty(values, "IMPORTDATE", "DATAIMPORT", "DATA")),
                FlowCode = FirstNonEmpty(values, "CIB", "CHVLANC", "CHVLANCAMENTO", "BANKFLOWCODE"),
                BookDate = ParseDate(FirstNonEmpty(values, "BOOKDATE", "DATAMOV", "DATACONTAB")),
                Description = FirstNonEmpty(values, "DESCRICAO", "DESCRICAOCOMPLETA", "DESCRIPTION"),
                ChequeNumber = FirstNonEmpty(values, "NCHEQUE", "CHEQUE", "DOCREF"),
                ValueDate = ParseDate(FirstNonEmpty(values, "DATAVALOR", "VALUEDATE")),
                AccountCode = FirstNonEmpty(values, "CONTA", "CONTAGL", "ACCOUNT", "ACCCODE"),
                AccountNib = FirstNonEmpty(values, "NIB", "CONTAELETRONICA"),
                Amount = amount,
                Currency = FirstNonEmpty(values, "DIVISA", "CURRENCY", "TRNCUR"),
                Company = FirstNonEmpty(values, "SOC", "EMPRESA", "ORGV")
            });

            row++;
        }

        return transactions;
    }

    private static string Extract(string line, int start, int length)
    {
        if (start <= 0 || length <= 0)
        {
            return string.Empty;
        }

        var startIndex = start - 1;
        if (line.Length <= startIndex)
        {
            return string.Empty;
        }

        var take = Math.Min(length, line.Length - startIndex);
        return line.Substring(startIndex, take).Trim();
    }

    private static string FirstNonEmpty(IReadOnlyDictionary<string, string> values, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }

    private static decimal ParseAmount(string amountRaw)
    {
        if (string.IsNullOrWhiteSpace(amountRaw))
        {
            return 0m;
        }

        var normalized = amountRaw.Trim();
        normalized = LegacyStringUtils.TreatSignedAmount(normalized);
        normalized = normalized.Replace(" ", string.Empty);

        if (normalized.IndexOfAny(['.', ',']) >= 0)
        {
            return LegacyStringUtils.ParseLegacyDecimal(normalized);
        }

        if (long.TryParse(normalized, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var whole))
        {
            return whole / 100m;
        }

        return LegacyStringUtils.ParseLegacyDecimal(normalized);
    }

    private static DateTime? ParseDate(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var formats = new[]
        {
            "dd-MM-yyyy",
            "dd/MM/yyyy",
            "yyyy-MM-dd",
            "yyyyMMdd",
            "ddMMyyyy"
        };

        return DateTime.TryParseExact(raw.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : null;
    }
}
