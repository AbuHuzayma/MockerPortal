using Portal.Application.Common;
using Portal.Domain.Customers;

namespace Portal.Application.Customers;

public sealed class OnboardingService(ICustomerService customerService, ICreationService creationService) : IOnboardingService
{
    public async Task<Result<OnboardingInfo>> GetOnboardingAsync(string customerId, CancellationToken ct)
    {
        var profile = await customerService.GetProfileAsync(customerId, ct);
        if (!profile.IsSuccess)
        {
            return Result<OnboardingInfo>.Failure(profile.ErrorCode!, profile.ErrorMessage!);
        }

        var creation = await creationService.GetCreationAsync(customerId, ct);
        object? dateCreated = null;
        creation.Value?.Fields.TryGetValue("createdDate", out dateCreated);

        return Result<OnboardingInfo>.Success(new OnboardingInfo
        {
            CustId = profile.Value!.CustId,
            CustNumber = profile.Value.CustNumber,
            T24CustomerId = profile.Value.T24CustomerId,
            DateCreated = dateCreated?.ToString(),
        });
    }
}
