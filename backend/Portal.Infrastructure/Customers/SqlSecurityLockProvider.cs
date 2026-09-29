using Dapper;
using Microsoft.Data.SqlClient;
using Portal.Application.Customers;
using Portal.Domain.Customers;

namespace Portal.Infrastructure.Customers;

/// <summary>Reads/resets T_PRT_CUSTOMER's security-lock columns (master spec §17).</summary>
public sealed class SqlSecurityLockProvider(string connectionString) : ISecurityLockProvider
{
    private const string SelectSql = """
        SELECT
            CUST_ID            AS CustId,
            CUST_NUMBER         AS CustNumber,
            FAILED_LOGON_COUNT  AS FailedLogonCount,
            FAILED_OTP_COUNT    AS FailedOtpCount,
            CURRENT_OTP_STATUS  AS CurrentOtpStatus,
            FREEZE_STATUS_ID    AS FreezeStatusId
        FROM T_PRT_CUSTOMER
        WHERE CUST_ID = @CustomerId OR CUST_NUMBER = @CustomerId
        """;

    public async Task<SecurityLockStatus?> GetStatusAsync(string customerId, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        return await connection.QuerySingleOrDefaultAsync<SecurityLockStatus>(
            new CommandDefinition(SelectSql, new { CustomerId = customerId }, cancellationToken: ct));
    }

    public async Task<SecurityLockStatus> RemoveLockAsync(string customerId, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        const string updateSql = """
            UPDATE T_PRT_CUSTOMER
            SET FAILED_LOGON_COUNT = 0, FAILED_OTP_COUNT = 0, CURRENT_OTP_STATUS = 'Clear', FREEZE_STATUS_ID = NULL
            WHERE CUST_ID = @CustomerId OR CUST_NUMBER = @CustomerId
            """;
        await connection.ExecuteAsync(new CommandDefinition(updateSql, new { CustomerId = customerId }, cancellationToken: ct));

        return await GetStatusAsync(customerId, ct)
            ?? throw new InvalidOperationException($"Customer '{customerId}' was updated but could not be re-read.");
    }
}
