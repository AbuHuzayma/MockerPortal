using Portal.Application.Common;
using Portal.Domain.Customers;

namespace Portal.Application.Customers;

public sealed class CustomerService(ICustomerProvider provider) : ICustomerService
{
    public async Task<Result<Customer>> SearchByMobileNumberAsync(string mobileNumber, CancellationToken ct)
    {
        var customer = await provider.FindByMobileNumberAsync(mobileNumber, ct);
        return customer is null
            ? Result<Customer>.Failure(CustomerErrorCodes.CustomerNotFound, "No customer was found for that mobile number.")
            : Result<Customer>.Success(customer);
    }

    public async Task<Result<Customer>> GetProfileAsync(string customerId, CancellationToken ct)
    {
        var customer = await provider.FindByCustomerIdAsync(customerId, ct);
        return customer is null
            ? Result<Customer>.Failure(CustomerErrorCodes.CustomerNotFound, "Customer was not found.")
            : Result<Customer>.Success(customer);
    }
}
