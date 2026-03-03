using System.Data.SqlClient;

namespace Contab.WinForms.Models;

public sealed class AppConfig
{
    public string SqlServer { get; init; } = string.Empty;
    public string SqlDatabase { get; init; } = string.Empty;
    public string SqlUser { get; init; } = string.Empty;
    public string SqlPassword { get; init; } = string.Empty;

    public string SapApplicationServer { get; init; } = string.Empty;
    public string SapMessageServer { get; init; } = string.Empty;
    public string SapUser { get; init; } = string.Empty;
    public string SapPassword { get; init; } = string.Empty;
    public string SapLanguage { get; init; } = "PT";
    public string SapSystemNumber { get; init; } = string.Empty;
    public string SapSystem { get; init; } = string.Empty;
    public string SapMandante { get; init; } = string.Empty;

    public bool MarkAsExported { get; init; }
    public string OutputFormat { get; init; } = "CSV Especifico";
    public string OutputDirectory { get; init; } = string.Empty;
    public bool UseCashLedger { get; init; }
    public bool ValidateReconciledTable { get; init; }
    public string InputPathOrFilter { get; init; } = string.Empty;
    public bool AdministratorsMode { get; init; }
    public string UserNameOverride { get; init; } = string.Empty;
    public string CompanyFilter { get; init; } = string.Empty;
    public string SqlSpecificCommand { get; init; } = string.Empty;
    public bool ContabException { get; init; }
    public string Language { get; init; } = "PT";
    public int Layout { get; init; } = 1;
    public bool KeepSqlForAllRoutines { get; init; }
    public string Aggregator { get; init; } = "Sem Agregacao";
    public int MaxDaysWithoutRuleUse { get; init; } = 400;
    public bool DebugEnabled { get; init; }
    public bool HoldingFlag { get; init; }
    public string SocietyView { get; init; } = string.Empty;
    public string ErpDescriptionMode { get; init; } = "GERAL";
    public string SourcePath { get; init; } = string.Empty;

    public string BuildConnectionString()
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = SqlServer,
            InitialCatalog = SqlDatabase,
            IntegratedSecurity = string.IsNullOrWhiteSpace(SqlUser),
            TrustServerCertificate = true,
            Encrypt = false
        };

        if (!builder.IntegratedSecurity)
        {
            builder.UserID = SqlUser;
            builder.Password = SqlPassword;
        }

        return builder.ConnectionString;
    }
}
