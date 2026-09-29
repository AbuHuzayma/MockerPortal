using Portal.Domain.Customers;

namespace Portal.Application.Customers;

/// <summary>
/// ASSUMED CONTRACT — the real Beneficiary Service API shape has not been
/// supplied (docs/08 §4). A customer's single mock beneficiary uses a fixed
/// ID since no "list beneficiaries" data is available yet — see
/// docs/06-api-design.md's /customers/{id}/beneficiaries/{beneficiaryId}/activate route.
/// </summary>
public interface IBeneficiaryServiceClient
{
    public const string DefaultBeneficiaryId = "BEN-0001";

    Task<BeneficiaryStatus> GetStatusAsync(string customerId, string beneficiaryId, CancellationToken ct);

    Task<BeneficiaryStatus> ActivateBeneficiaryAsync(string customerId, string beneficiaryId, CancellationToken ct);
}
