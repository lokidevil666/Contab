using System.Text;
using Contab.WinForms.Models;
using Contab.WinForms.Utilities;

namespace Contab.WinForms.Services;

public sealed class LegacyConfigReader
{
    static LegacyConfigReader()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public AppConfig Read(string iniPath)
    {
        if (string.IsNullOrWhiteSpace(iniPath))
        {
            throw new ArgumentException("INI path cannot be empty.", nameof(iniPath));
        }

        if (!File.Exists(iniPath))
        {
            throw new FileNotFoundException("Legacy config file not found.", iniPath);
        }

        var values = ReadLegacyValues(iniPath);
        while (values.Count < 32)
        {
            values.Add(string.Empty);
        }

        var sqlPass = DecodePassword(values[3]);
        var sapPass = DecodePassword(values[7]);

        return new AppConfig
        {
            SqlServer = values[0],
            SqlDatabase = values[1],
            SqlUser = values[2],
            SqlPassword = sqlPass,
            SapApplicationServer = values[4],
            SapMessageServer = values[5],
            SapUser = values[6],
            SapPassword = sapPass,
            SapLanguage = DefaultIfEmpty(values[8], "PT"),
            SapSystemNumber = values[9],
            SapSystem = values[10],
            SapMandante = values[11],
            MarkAsExported = IsEnabled(values[12]),
            OutputFormat = DefaultIfEmpty(values[13], "CSV Especifico"),
            OutputDirectory = values[14],
            UseCashLedger = IsEnabled(values[15]),
            ValidateReconciledTable = IsEnabled(values[16]),
            InputPathOrFilter = values[17],
            AdministratorsMode = IsEnabled(values[18]),
            UserNameOverride = values[19],
            CompanyFilter = values[20],
            SqlSpecificCommand = values[21],
            ContabException = IsEnabled(values[22]),
            Language = NormalizeLanguage(values[23]),
            Layout = ParseLayout(values[24]),
            KeepSqlForAllRoutines = IsEnabled(values[25]),
            Aggregator = DefaultIfEmpty(values[26], "Sem Agregacao"),
            MaxDaysWithoutRuleUse = ParseIntOrDefault(values[27], 400),
            DebugEnabled = IsEnabled(values[28]),
            HoldingFlag = IsEnabled(values[29]),
            SocietyView = values[30],
            ErpDescriptionMode = DefaultIfEmpty(LegacyStringUtils.ToLegacyUpper(values[31]), "GERAL"),
            SourcePath = iniPath
        };
    }

    private static List<string> ReadLegacyValues(string iniPath)
    {
        var values = new List<string>(40);
        foreach (var line in File.ReadLines(iniPath, Encoding.GetEncoding(1252)))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (trimmed.StartsWith('"') && trimmed.EndsWith('"') && trimmed.Length >= 2)
            {
                trimmed = trimmed[1..^1];
            }

            if (trimmed.StartsWith(';'))
            {
                continue;
            }

            values.Add(trimmed);
        }

        return values;
    }

    private static string DecodePassword(string raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return string.Empty;
        }

        return raw.StartsWith('!') ? LegacyStringUtils.Decrypt(raw[1..]) : raw;
    }

    private static bool IsEnabled(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();
        return normalized.Equals("1", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("sim", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeLanguage(string value)
    {
        var language = LegacyStringUtils.ToLegacyUpper(value);
        return language is "PT" or "EN" or "ES" ? language : "PT";
    }

    private static int ParseLayout(string value)
    {
        var parsed = ParseIntOrDefault(value, 1);
        return parsed is 1 or 2 ? parsed : 1;
    }

    private static int ParseIntOrDefault(string value, int fallback)
    {
        return int.TryParse(value, out var parsed) ? parsed : fallback;
    }

    private static string DefaultIfEmpty(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
