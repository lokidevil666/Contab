using System.Data;
using System.Data.SqlClient;
using Contab.WinForms.Models;

namespace Contab.WinForms.Services;

public sealed class LegacyDataRepository
{
    public async Task<IReadOnlyList<LegacyTransaction>> LoadPendingTransactionsAsync(
        string connectionString,
        int take,
        bool useCashLedger,
        string? companyFilter,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string cannot be empty.", nameof(connectionString));
        }

        if (take <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(take), take, "Take must be greater than zero.");
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        if (useCashLedger)
        {
            return await QueryCashLedgerAsync(connection, take, companyFilter, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            return await QueryNrecBankAsync(connection, take, companyFilter, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return await QueryCashLedgerAsync(connection, take, companyFilter, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<IReadOnlyList<LegacyTransaction>> QueryNrecBankAsync(
        SqlConnection connection,
        int take,
        string? companyFilter,
        CancellationToken cancellationToken)
    {
        var sql = """
                  SELECT TOP (@Take)
                      CAST(nrec_bank_mvt_id AS varchar(40)) AS RECID,
                      import_date,
                      bank_flow_code AS CIB,
                      book_date,
                      description,
                      cheque_nb,
                      value_date,
                      acc_code,
                      zu_01,
                      ISNULL(
                          (
                              SELECT TOP 1 LEFT(electronic_value, 40)
                              FROM ba_number_items_acc
                              WHERE baf_item_type = '3' AND acc_code = nrec_bank.acc_code
                          ),
                          ''
                      ) AS CONTA,
                      trn_amount AS MONTANTE,
                      trn_cur AS DIVISA,
                      ISNULL(
                          (
                              SELECT TOP 1 cmp_code
                              FROM accounts
                              WHERE acc_code = nrec_bank.acc_code
                          ),
                          ''
                      ) AS SOC
                  FROM nrec_bank
                  WHERE zu_02 IS NULL
                    AND description IS NOT NULL
                    AND (@CompanyFilter = '' OR EXISTS (
                        SELECT 1 FROM accounts a
                        WHERE a.acc_code = nrec_bank.acc_code AND a.cmp_code = @CompanyFilter
                    ))
                  ORDER BY nrec_bank_mvt_id
                  """;

        return await ExecuteQueryAsync(connection, sql, take, companyFilter, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<LegacyTransaction>> QueryCashLedgerAsync(
        SqlConnection connection,
        int take,
        string? companyFilter,
        CancellationToken cancellationToken)
    {
        var sql = """
                  SELECT TOP (@Take)
                      CAST(cash_ledger_id AS varchar(40)) AS RECID,
                      entry_date AS import_date,
                      flow_code AS CIB,
                      book_date,
                      description,
                      budget_code AS cheque_nb,
                      value_date,
                      acc_code,
                      zu_01,
                      ISNULL(
                          (
                              SELECT TOP 1 LEFT(electronic_value, 40)
                              FROM ba_number_items_acc
                              WHERE baf_item_type = '3' AND acc_code = cash_ledger.acc_code
                          ),
                          ISNULL(
                              (
                                  SELECT TOP 1 LEFT(electronic_value, 40)
                                  FROM ba_number_items_acc
                                  WHERE baf_item_type = '0' AND acc_code = cash_ledger.acc_code
                              ),
                              ''
                          )
                      ) AS CONTA,
                      amount AS MONTANTE,
                      cur_code AS DIVISA,
                      ISNULL(
                          (
                              SELECT TOP 1 cmp_code
                              FROM accounts
                              WHERE acc_code = cash_ledger.acc_code
                          ),
                          ''
                      ) AS SOC
                  FROM cash_ledger
                  WHERE description IS NOT NULL
                    AND (@CompanyFilter = '' OR EXISTS (
                        SELECT 1 FROM accounts a
                        WHERE a.acc_code = cash_ledger.acc_code AND a.cmp_code = @CompanyFilter
                    ))
                  ORDER BY cash_ledger_id
                  """;

        return await ExecuteQueryAsync(connection, sql, take, companyFilter, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<LegacyTransaction>> ExecuteQueryAsync(
        SqlConnection connection,
        string sql,
        int take,
        string? companyFilter,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandType = CommandType.Text;
        command.Parameters.Add("@Take", SqlDbType.Int).Value = take;
        command.Parameters.Add("@CompanyFilter", SqlDbType.VarChar, 50).Value = companyFilter?.Trim() ?? string.Empty;

        var list = new List<LegacyTransaction>(take);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            list.Add(new LegacyTransaction
            {
                RecordId = GetString(reader, "RECID"),
                ImportDate = GetDate(reader, "import_date"),
                FlowCode = GetString(reader, "CIB"),
                BookDate = GetDate(reader, "book_date"),
                Description = GetString(reader, "description"),
                ChequeNumber = GetString(reader, "cheque_nb"),
                ValueDate = GetDate(reader, "value_date"),
                AccountCode = GetString(reader, "acc_code"),
                AccountNib = GetString(reader, "CONTA"),
                Amount = GetDecimal(reader, "MONTANTE"),
                Currency = GetString(reader, "DIVISA"),
                Company = GetString(reader, "SOC")
            });
        }

        return list;
    }

    private static string GetString(SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? string.Empty : Convert.ToString(reader.GetValue(ordinal))?.Trim() ?? string.Empty;
    }

    private static DateTime? GetDate(SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        var value = reader.GetValue(ordinal);
        return value switch
        {
            DateTime dt => dt,
            _ when DateTime.TryParse(Convert.ToString(value), out var parsed) => parsed,
            _ => null
        };
    }

    private static decimal GetDecimal(SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        if (reader.IsDBNull(ordinal))
        {
            return 0m;
        }

        var value = reader.GetValue(ordinal);
        return value switch
        {
            decimal dec => dec,
            double d => Convert.ToDecimal(d),
            float f => Convert.ToDecimal(f),
            int i => i,
            long l => l,
            _ when decimal.TryParse(Convert.ToString(value), out var parsed) => parsed,
            _ => 0m
        };
    }
}
