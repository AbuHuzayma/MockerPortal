using Portal.Application.Customers;
using Portal.Domain.Customers;

namespace Portal.Infrastructure.Customers;

/// <summary>
/// Seeded fixture data for local/DEV use — selected via Providers:Customer:Mode
/// = "Mock" (the default everywhere except QA/PREPROD). See docs/08 §1.
/// </summary>
public sealed class MockCustomerProvider : ICustomerProvider
{
    private static readonly IReadOnlyList<Customer> Fixtures =
    [
        new()
        {
            CustId = "CUST-100001",
            CustNumber = "100001",
            T24CustomerId = "T24-100001",
            MobileNo = "0500000001",
            FirstName = "Sara",
            LastName = "Al-Otaibi",
            ArabicFirstName = "سارة",
            ArabicLastName = "العتيبي",
            Email = "sara.test@example.com",
            Nationality = "SA",
            LifeStatus = "Active",
            BlacklistStatus = "Clear",
        },
        new()
        {
            CustId = "CUST-100002",
            CustNumber = "100002",
            T24CustomerId = "T24-100002",
            MobileNo = "0500000002",
            FirstName = "Omar",
            LastName = "Al-Harbi",
            ArabicFirstName = "عمر",
            ArabicLastName = "الحربي",
            Email = "omar.test@example.com",
            Nationality = "SA",
            LifeStatus = "Active",
            BlacklistStatus = "Clear",
        },
        new()
        {
            CustId = "CUST-100003",
            CustNumber = "100003",
            T24CustomerId = null,
            MobileNo = "0500000003",
            FirstName = "Layla",
            LastName = "Al-Zahrani",
            ArabicFirstName = "ليلى",
            ArabicLastName = "الزهراني",
            Email = null,
            Nationality = "SA",
            LifeStatus = "Suspended",
            BlacklistStatus = "Clear",
        },
    ];

    public Task<Customer?> FindByMobileNumberAsync(string mobileNumber, CancellationToken ct) =>
        Task.FromResult(Fixtures.FirstOrDefault(c => c.MobileNo == mobileNumber));

    public Task<Customer?> FindByCustomerIdAsync(string customerId, CancellationToken ct) =>
        Task.FromResult(Fixtures.FirstOrDefault(c => c.CustId == customerId || c.CustNumber == customerId));
}
