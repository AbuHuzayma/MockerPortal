using Portal.Application.Common;
using Portal.Domain.Customers;

namespace Portal.Application.Customers;

public static class CustomerErrorCodes
{
    public const string CustomerNotFound = "CUSTOMER_NOT_FOUND";
}

/// <summary>
/// Application-layer entry point for customer read operations, called directly
/// from CustomersController — no mediator/CQRS dispatcher is used in this
/// codebase (same direct-service pattern as IAuthService in Phase 1).
/// </summary>
public interface ICustomerService
{
    Task<Result<Customer>> SearchByMobileNumberAsync(string mobileNumber, CancellationToken ct);

    Task<Result<Customer>> GetProfileAsync(string customerId, CancellationToken ct);
}
