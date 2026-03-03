using System.Globalization;
using System.Text;

namespace Contab.WinForms.Utilities;

public static class LegacyStringUtils
{
    private static readonly Dictionary<string, string> BankCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["00033"] = "BCP",
        ["00007"] = "BES",
        ["00008"] = "BAI",
        ["00010"] = "BPI",
        ["00012"] = "BCA",
        ["00018"] = "BTA",
        ["00019"] = "BBVA",
        ["00021"] = "CPP",
        ["00027"] = "BPIV",
        ["00029"] = "FORTIS",
        ["00030"] = "SANT",
        ["00032"] = "BARC",
        ["00034"] = "BNP",
        ["00035"] = "CGD",
        ["00036"] = "MG",
        ["00038"] = "BANIF",
        ["00040"] = "ABN",
        ["00043"] = "DEUTB",
        ["00046"] = "BNC",
        ["00076"] = "FINI",
        ["00160"] = "BESA"
    };

    private static readonly Dictionary<string, string> BankCodesShort = new(StringComparer.OrdinalIgnoreCase)
    {
        ["0049"] = "BSCH",
        ["0065"] = "BARCLAYS",
        ["0182"] = "BBVA",
        ["0030"] = "BEC",
        ["0160"] = "BCA",
        ["0190"] = "BPIE",
        ["0131"] = "BESE",
        ["2100"] = "LACX"
    };

    private static readonly Dictionary<char, string> SignedAmountLastChar = new()
    {
        ['{'] = "+0",
        ['\u00E9'] = "+0",
        ['A'] = "+1",
        ['B'] = "+2",
        ['C'] = "+3",
        ['D'] = "+4",
        ['E'] = "+5",
        ['F'] = "+6",
        ['G'] = "+7",
        ['H'] = "+8",
        ['I'] = "+9",
        ['}'] = "-0",
        ['e'] = "-0",
        ['\u00E8'] = "-0",
        ['J'] = "-1",
        ['K'] = "-2",
        ['L'] = "-3",
        ['M'] = "-4",
        ['N'] = "-5",
        ['O'] = "-6",
        ['P'] = "-7",
        ['Q'] = "-8",
        ['R'] = "-9"
    };

    public static string FillChar(int count, string character)
    {
        if (count <= 0 || string.IsNullOrEmpty(character))
        {
            return string.Empty;
        }

        return string.Create(count, character[0], static (span, c) => span.Fill(c));
    }

    public static string SeparateIntegerDecimal(string value, string outputPart)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "0";
        }

        var decimalIndex = value.IndexOfAny(['.', ',']);
        var integerPart = decimalIndex >= 0 ? value[..decimalIndex] : value;
        var decimalPart = decimalIndex >= 0 && decimalIndex < value.Length - 1 ? value[(decimalIndex + 1)..] : "0";

        return outputPart.ToUpperInvariant() switch
        {
            "INT" => integerPart,
            "DEC" => decimalPart,
            _ => value
        };
    }

    public static string GetBankCode(string accountRaw)
    {
        if (string.IsNullOrWhiteSpace(accountRaw))
        {
            return string.Empty;
        }

        var value = accountRaw.Trim();
        if (value.Length >= 5 && BankCodes.TryGetValue(value[..5], out var bank))
        {
            return bank;
        }

        if (value.Length >= 4 && BankCodesShort.TryGetValue(value[..4], out bank))
        {
            return bank;
        }

        return string.Empty;
    }

    public static string ToLegacyLower(string phrase)
    {
        return phrase?.ToLowerInvariant() ?? string.Empty;
    }

    public static string ToLegacyUpper(string phrase)
    {
        return phrase?.ToUpperInvariant() ?? string.Empty;
    }

    public static string Encrypt(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            sb.Append(c switch
            {
                >= 'A' and <= 'Z' => (char)(c + 127),
                >= 'a' and <= 'z' => (char)(c + 121),
                >= '0' and <= '9' => (char)(c + 196),
                _ => c
            });
        }

        return sb.ToString();
    }

    public static string Decrypt(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            sb.Append(c switch
            {
                >= (char)192 and <= (char)217 => (char)(c - 127),
                >= (char)218 and <= (char)243 => (char)(c - 121),
                >= (char)244 and <= (char)253 => (char)(c - 196),
                _ => c
            });
        }

        return sb.ToString();
    }

    public static string TreatSignedAmount(string rawAmount)
    {
        if (string.IsNullOrWhiteSpace(rawAmount))
        {
            return "0";
        }

        var trimmed = rawAmount.Trim();
        var last = trimmed[^1];
        if (!SignedAmountLastChar.TryGetValue(last, out var signAndDigit))
        {
            return trimmed;
        }

        var sign = signAndDigit[0];
        var digit = signAndDigit[1];
        var body = trimmed.Length > 1 ? trimmed[..^1] : string.Empty;
        return $"{sign}{body}{digit}";
    }

    public static decimal ParseLegacyDecimal(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return 0m;
        }

        var value = raw.Trim();

        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var invariant))
        {
            return invariant;
        }

        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.GetCultureInfo("pt-PT"), out var pt))
        {
            return pt;
        }

        return 0m;
    }

    public static string NormalizeIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(char.ToUpperInvariant(c));
            }
        }

        return sb.ToString();
    }
}
