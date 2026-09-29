using Dapper;
using Microsoft.Data.SqlClient;
using Portal.Application.Merchants;
using Portal.Domain.Merchants;

namespace Portal.Infrastructure.Merchants;

/// <summary>
/// Reads/writes the Merchant DB (master spec §16). Table name and
/// MerchantId/MerchantNumber columns are not given by the spec — this
/// assumes a T_PRT_MERCHANT table with MERCHANT_ID/MERCHANT_NUMBER columns,
/// mirroring T_PRT_CUSTOMER's convention; confirm against the real schema.
/// Selected via Providers:Merchant:Mode = "Sql" (QA/PREPROD).
/// </summary>
public sealed class SqlMerchantProvider(string connectionString) : IMerchantProvider
{
    private static readonly string SelectColumns = string.Join(", ", MerchantFieldCatalog.Fields.Select(f => $"{f.ColumnName} AS [{f.Key}]"));

    public async Task<IReadOnlyList<Merchant>> SearchByNameAsync(string name, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        var sql = $"SELECT MERCHANT_ID AS MerchantId, MERCHANT_NUMBER AS MerchantNumber, {SelectColumns} " +
                  "FROM T_PRT_MERCHANT WHERE NAME_EN LIKE @Pattern OR NAME_AR LIKE @Pattern";

        var rows = await connection.QueryAsync(
            new CommandDefinition(sql, new { Pattern = $"%{name}%" }, cancellationToken: ct));

        return rows.Select(ToMerchant).ToList();
    }

    public async Task<Merchant?> FindByMerchantIdAsync(string merchantId, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        var sql = $"SELECT MERCHANT_ID AS MerchantId, MERCHANT_NUMBER AS MerchantNumber, {SelectColumns} " +
                  "FROM T_PRT_MERCHANT WHERE MERCHANT_ID = @MerchantId OR MERCHANT_NUMBER = @MerchantId";

        var row = await connection.QuerySingleOrDefaultAsync(
            new CommandDefinition(sql, new { MerchantId = merchantId }, cancellationToken: ct));

        return row is null ? null : ToMerchant(row);
    }

    public async Task<Merchant> UpdateAsync(string merchantId, IReadOnlyDictionary<string, object?> fields, CancellationToken ct)
    {
        if (fields.Count > 0)
        {
            await using var connection = new SqlConnection(connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("MerchantId", merchantId);

            var setClauses = new List<string>();
            foreach (var definition in MerchantFieldCatalog.Fields)
            {
                if (!fields.TryGetValue(definition.Key, out var value))
                {
                    continue;
                }
                setClauses.Add($"{definition.ColumnName} = @{definition.Key}");
                parameters.Add(definition.Key, value);
            }

            if (setClauses.Count > 0)
            {
                var updateSql = $"UPDATE T_PRT_MERCHANT SET {string.Join(", ", setClauses)} " +
                                 "WHERE MERCHANT_ID = @MerchantId OR MERCHANT_NUMBER = @MerchantId";
                await connection.ExecuteAsync(new CommandDefinition(updateSql, parameters, cancellationToken: ct));
            }
        }

        return await FindByMerchantIdAsync(merchantId, ct)
            ?? throw new InvalidOperationException($"Merchant '{merchantId}' was updated but could not be re-read.");
    }

    private static Merchant ToMerchant(dynamic row)
    {
        var dict = (IDictionary<string, object?>)row;
        string? Get(string key) => dict.TryGetValue(key, out var value) ? value?.ToString() : null;

        return new Merchant
        {
            MerchantId = dict["MerchantId"]!.ToString()!,
            MerchantNumber = dict["MerchantNumber"]!.ToString()!,
            NameEn = Get("nameEn"),
            NameAr = Get("nameAr"),
            BrandNameEn = Get("brandNameEn"),
            BrandNameAr = Get("brandNameAr"),
            CrExpiryDate = Get("crExpiryDate"),
            IdExpiryDate = Get("idExpiryDate"),
        };
    }
}
