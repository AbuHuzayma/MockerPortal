using FluentValidation;

namespace Portal.Application.Customers;

public sealed record SearchCustomerRequest(string MobileNumber);

/// <summary>
/// Deliberately loose — the master spec does not pin down a specific mobile
/// number format, so this only rejects obviously-invalid input rather than
/// asserting an unstated business rule (e.g. a specific country format).
/// </summary>
public sealed class SearchCustomerRequestValidator : AbstractValidator<SearchCustomerRequest>
{
    public SearchCustomerRequestValidator()
    {
        RuleFor(x => x.MobileNumber)
            .NotEmpty()
            .Matches("^[0-9]{7,15}$")
            .WithMessage("Mobile number must be 7-15 digits.");
    }
}
