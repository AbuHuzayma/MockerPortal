using System.Text.RegularExpressions;
using FluentValidation;

namespace Portal.Application.Customers;

/// <summary>
/// The master spec gives no format/business rules for most KYC fields, so this
/// stays deliberately minimal — email format is the one rule that's genuinely
/// implied by the field's own name, not invented.
/// </summary>
public sealed partial class KycUpdateValidator : AbstractValidator<KycFieldValues>
{
    public KycUpdateValidator()
    {
        RuleFor(x => x).Custom((fields, context) =>
        {
            if (fields.TryGetValue("email", out var value) && value is string email && !string.IsNullOrWhiteSpace(email)
                && !EmailPattern().IsMatch(email))
            {
                context.AddFailure("email", "Email must be a valid email address.");
            }
        });
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
