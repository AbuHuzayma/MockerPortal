using Dapper;
using Microsoft.Data.SqlClient;
using Portal.Application.Customers;
using Portal.Domain.Customers;

namespace Portal.Infrastructure.Customers;

/// <summary>
/// Reads/writes T_PRT_CUSTOMER's creation-date columns (master spec §12).
/// Two of the three columns are ambiguous in the spec itself — see
/// CreationFieldCatalog's doc comment. Selected via
/// Providers:Creation:Mode = "Sql" (QA/PREPROD).
/// </summary>
public sealed class SqlCreationProvider(string connectionString) : ICreationProvider
{
    public async Task<CreationRecord?> GetByCustomerIdAsync(string customerId, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        var columns = string.Join(", ", CreationFieldCatalog.Fields.Select(f => $"{f.ColumnName} AS [{f.Key}]"));
        var sql = $"SELECT CUST_ID AS CustId, CUST_NUMBER AS CustNumber, {columns} FROM T_PRT_CUSTOMER " +
                  "WHERE CUST_ID = @CustomerId OR CUST_NUMBER = @CustomerId";

        var row = await connection.QuerySingleOrDefaultAsync(
            new CommandDefinition(sql, new { CustomerId = customerId }, cancellationToken: ct));

        return row is null ? null : ToRecord(row);
    }

    public async Task<CreationRecord> UpdateAsync(string customerId, IReadOnlyDictionary<string, object?> fields, CancellationToken ct)
    {
        if (fields.Count > 0)
        {
            await using var connection = new SqlConnection(connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("CustomerId", customerId);

            var setClauses = new List<string>();
            foreach (var definition in CreationFieldCatalog.Fields)
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
                var updateSql = $"UPDATE T_PRT_CUSTOMER SET {string.Join(", ", setClauses)} " +
                                 "WHERE CUST_ID = @CustomerId OR CUST_NUMBER = @CustomerId";
                await connection.ExecuteAsync(new CommandDefinition(updateSql, parameters, cancellationToken: ct));
            }
        }

        return await GetByCustomerIdAsync(customerId, ct)
            ?? throw new InvalidOperationException($"Customer '{customerId}' was updated but could not be re-read.");
    }

    private static CreationRecord ToRecord(dynamic row)
    {
        var dict = (IDictionary<string, object?>)row;
        var resultFields = new Dictionary<string, object?>();

        foreach (var definition in CreationFieldCatalog.Fields)
        {
            dict.TryGetValue(definition.Key, out var value);
            resultFields[definition.Key] = value?.ToString();
        }

        return new CreationRecord
        {
            CustId = dict["CustId"]!.ToString()!,
            CustNumber = dict["CustNumber"]!.ToString()!,
            Fields = resultFields,
        };
    }
}
