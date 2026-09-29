using Portal.Application.Common;
using Portal.Domain.Customers;

namespace Portal.Application.Customers;

/// <summary>
/// Read-only (master spec §21: "Do not provide update operations unless
/// explicitly added later"). Composed from ICustomerService + ICreationService
/// — no new provider, since every field it needs already exists elsewhere.
/// </summary>
public interface IOnboardingService
{
    Task<Result<OnboardingInfo>> GetOnboardingAsync(string customerId, CancellationToken ct);
}
