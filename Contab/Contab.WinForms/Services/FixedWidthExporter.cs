using System.Globalization;
using System.Text;
using Contab.WinForms.Models;
using Contab.WinForms.Utilities;

namespace Contab.WinForms.Services;

public sealed class FixedWidthExporter
{
    static FixedWidthExporter()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public ExportResult Write(string outputPath, StructureDefinition structure, IReadOnlyList<AccountingRow> rows)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path cannot be empty.", nameof(outputPath));
        }

        if (rows.Count == 0)
        {
            throw new InvalidOperationException("There are no rows to export.");
        }

        if (structure.OutputFields.Count == 0)
        {
            throw new InvalidOperationException("Output structure is empty. Check contab.str.");
        }

        List<string> lines = new List<string>(rows.Count * 2);
        foreach (AccountingRow row in rows)
        {
            if (structure.HeaderFields.Count > 0)
            {
                lines.Add(ComposeLine(structure.HeaderFields, row));
            }

            lines.Add(ComposeLine(structure.OutputFields, row));
        }

        string? directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(outputPath, lines, Encoding.GetEncoding(1252));
        return new ExportResult(outputPath, lines.Count, rows.Count);
    }

    private static string ComposeLine(IReadOnlyList<StructureField> fields, AccountingRow row)
    {
        int lineLength = 0;
        foreach (StructureField field in fields)
        {
            int end = field.Start + field.Length - 1;
            if (end > lineLength)
            {
                lineLength = end;
            }
        }

        char[] chars = new char[lineLength];
        for (int i = 0; i < chars.Length; i++)
        {
            chars[i] = ' ';
        }

        List<StructureField> orderedFields = new List<StructureField>(fields);
        orderedFields.Sort((a, b) => a.Start.CompareTo(b.Start));

        foreach (StructureField field in orderedFields)
        {
            string value = ResolveFieldValue(field, row);
            string fitted = Fit(value, field);
            int start = field.Start - 1;
            int take = Math.Min(field.Length, fitted.Length);

            for (int i = 0; i < take; i++)
            {
                chars[start + i] = fitted[i];
            }
        }

        return new string(chars);
    }

    private static string ResolveFieldValue(StructureField field, AccountingRow row)
    {
        string key = LegacyStringUtils.NormalizeIdentifier(field.Name);
        string defaultValue = field.DefaultValue == "_" ? string.Empty : field.DefaultValue;

        switch (key)
        {
            case "CONSTANTE":
            case "FILLER":
                return defaultValue;

            case "NUMXU":
            case "NUMEROXU":
                return row.NumberXu;

            case "CHVLANC":
            case "CHVCONTRAPARTIDA":
            case "CHVINST":
                return row.PostingKey;

            case "CONTAGL":
            case "CONTAPARTIDA":
            case "CONTA":
                return row.GlAccount;

            case "MONTANTE":
                return FormatAmount(row.Amount, field.Length);

            case "DIVISAO":
                return row.Division;

            case "CPGT":
            case "RAZAO":
                return row.Cpgt;

            case "ORGV":
                return row.Organization;

            case "ATRIBUICAO":
                return row.Assignment;

            case "DESCRICAO":
                return row.Description;

            case "DOCREF":
                return row.DocumentReference;

            case "CODBANCOPOR":
                return row.BankCode;

            case "DATAVALOR":
                if (row.ValueDate.HasValue)
                {
                    return row.ValueDate.Value.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                }

                return string.Empty;

            default:
                return defaultValue;
        }
    }

    private static string Fit(string value, StructureField field)
    {
        if (value == null)
        {
            value = string.Empty;
        }

        if (value.Length > field.Length)
        {
            value = value.Substring(0, field.Length);
        }

        string key = LegacyStringUtils.NormalizeIdentifier(field.Name);
        bool leftPad = key == "MONTANTE" || key == "CHVLANC" || key == "CHVCONTRAPARTIDA";
        char padChar = leftPad ? '0' : ' ';

        if (leftPad)
        {
            return value.PadLeft(field.Length, padChar);
        }

        return value.PadRight(field.Length, padChar);
    }

    private static string FormatAmount(decimal amount, int fieldLength)
    {
        string sign = amount < 0m ? "-" : "+";
        string cents = decimal.Truncate(Math.Abs(amount) * 100m).ToString(CultureInfo.InvariantCulture);
        int payloadLength = Math.Max(1, fieldLength - 1);

        if (cents.Length > payloadLength)
        {
            cents = cents.Substring(cents.Length - payloadLength, payloadLength);
        }

        return sign + cents.PadLeft(payloadLength, '0');
    }
}
