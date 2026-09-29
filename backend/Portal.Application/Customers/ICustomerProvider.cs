using Portal.Domain.Customers;

namespace Portal.Application.Customers;

/// <summary>
/// Abstraction over the enterprise CustomerDatabase. Portal.Infrastructure supplies
/// a Mock implementation (seeded fixtures) and a Sql implementation (Dapper against
/// T_PRT_CUSTOMER); which one is active is a Providers:Customer:Mode config switch
/// — see docs/08-integration-architecture.md §1-2.
/// </summary>
public interface ICustomerProvider
{
    /// <summary>The only search identifier supported today — see docs/02-requirements.md §3.</summary>
    Task<Customer?> FindByMobileNumberAsync(string mobileNumber, CancellationToken ct);

    /// <summary>Matches CUST_ID or CUST_NUMBER, per the master spec's customer-matching rule.</summary>
    Task<Customer?> FindByCustomerIdAsync(string customerId, CancellationToken ct);
}
