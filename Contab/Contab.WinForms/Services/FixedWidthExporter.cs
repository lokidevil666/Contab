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

        var lines = new List<string>(rows.Count * 2);
        foreach (var row in rows)
        {
            if (structure.HeaderFields.Count > 0)
            {
                lines.Add(ComposeLine(structure.HeaderFields, row));
            }

            lines.Add(ComposeLine(structure.OutputFields, row));
        }

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(outputPath, lines, Encoding.GetEncoding(1252));
        return new ExportResult(outputPath, lines.Count, rows.Count);
    }

    private static string ComposeLine(IReadOnlyList<StructureField> fields, AccountingRow row)
    {
        var lineLength = fields.Max(f => f.Start + f.Length - 1);
        var chars = Enumerable.Repeat(' ', lineLength).ToArray();

        foreach (var field in fields.OrderBy(f => f.Start))
        {
            var value = ResolveFieldValue(field, row);
            var fitted = Fit(value, field);
            var start = field.Start - 1;
            var take = Math.Min(field.Length, fitted.Length);

            for (var i = 0; i < take; i++)
            {
                chars[start + i] = fitted[i];
            }
        }

        return new string(chars);
    }

    private static string ResolveFieldValue(StructureField field, AccountingRow row)
    {
        var key = LegacyStringUtils.NormalizeIdentifier(field.Name);
        return key switch
        {
            "CONSTANTE" => field.DefaultValue == "_" ? string.Empty : field.DefaultValue,
            "FILLER" => field.DefaultValue == "_" ? string.Empty : field.DefaultValue,
            "NUMXU" or "NUMEROXU" => row.NumberXu,
            "CHVLANC" or "CHVCONTRAPARTIDA" or "CHVINST" => row.PostingKey,
            "CONTAGL" or "CONTAPARTIDA" or "CONTA" => row.GlAccount,
            "MONTANTE" => FormatAmount(row.Amount, field.Length),
            "DIVISAO" => row.Division,
            "CPGT" or "RAZAO" => row.Cpgt,
            "ORGV" => row.Organization,
            "ATRIBUICAO" => row.Assignment,
            "DESCRICAO" => row.Description,
            "DOCREF" => row.DocumentReference,
            "CODBANCOPOR" => row.BankCode,
            "DATAVALOR" => row.ValueDate?.ToString("yyyyMMdd", CultureInfo.InvariantCulture) ?? string.Empty,
            _ => field.DefaultValue == "_" ? string.Empty : field.DefaultValue
        };
    }

    private static string Fit(string value, StructureField field)
    {
        value ??= string.Empty;

        if (value.Length > field.Length)
        {
            value = value[..field.Length];
        }

        var key = LegacyStringUtils.NormalizeIdentifier(field.Name);
        var leftPad = key is "MONTANTE" or "CHVLANC" or "CHVCONTRAPARTIDA";
        var padChar = leftPad ? '0' : ' ';
        return leftPad ? value.PadLeft(field.Length, padChar) : value.PadRight(field.Length, padChar);
    }

    private static string FormatAmount(decimal amount, int fieldLength)
    {
        var sign = amount < 0 ? "-" : "+";
        var cents = decimal.Truncate(Math.Abs(amount) * 100m).ToString(CultureInfo.InvariantCulture);
        var payloadLength = Math.Max(1, fieldLength - 1);

        if (cents.Length > payloadLength)
        {
            cents = cents[^payloadLength..];
        }

        return sign + cents.PadLeft(payloadLength, '0');
    }
}
