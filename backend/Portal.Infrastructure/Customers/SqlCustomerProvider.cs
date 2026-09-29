using Dapper;
using Microsoft.Data.SqlClient;
using Portal.Application.Customers;
using Portal.Domain.Customers;

namespace Portal.Infrastructure.Customers;

/// <summary>
/// Reads T_PRT_CUSTOMER in the enterprise CustomerDatabase (MSSQL). Column
/// presence is confirmed by the master spec's KYC field list; exact types/
/// nullability/constraints are not — see docs/05-database-design.md §3
/// "Unknowns". Selected via Providers:Customer:Mode = "Sql" (QA/PREPROD).
/// </summary>
public sealed class SqlCustomerProvider(string connectionString) : ICustomerProvider
{
    private const string SelectColumns = """
        SELECT
            CUST_ID            AS CustId,
            CUST_NUMBER        AS CustNumber,
            T24_CUSTOMER_ID    AS T24CustomerId,
            MOBILE_NO          AS MobileNo,
            FIRST_NAME         AS FirstName,
            LAST_NAME          AS LastName,
            ARABIC_FIRST_NAME  AS ArabicFirstName,
            ARABIC_LAST_NAME   AS ArabicLastName,
            EMAIL              AS Email,
            NATIONALITY        AS Nationality,
            LIFE_STATUS        AS LifeStatus,
            BLACKLIST_STATUS   AS BlacklistStatus
        FROM T_PRT_CUSTOMER
        """;

    public async Task<Customer?> FindByMobileNumberAsync(string mobileNumber, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        var command = new CommandDefinition(
            $"{SelectColumns} WHERE MOBILE_NO = @MobileNumber",
            new { MobileNumber = mobileNumber },
            cancellationToken: ct);

        return await connection.QuerySingleOrDefaultAsync<Customer>(command);
    }

    public async Task<Customer?> FindByCustomerIdAsync(string customerId, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        // Master spec: customer matching is by CUST_ID or CUST_NUMBER.
        var command = new CommandDefinition(
            $"{SelectColumns} WHERE CUST_ID = @CustomerId OR CUST_NUMBER = @CustomerId",
            new { CustomerId = customerId },
            cancellationToken: ct);

        return await connection.QuerySingleOrDefaultAsync<Customer>(command);
    }
}
