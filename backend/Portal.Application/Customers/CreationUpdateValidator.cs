using FluentValidation;

namespace Portal.Application.Customers;

/// <summary>
/// The master spec gives no format/business rules beyond the field names
/// themselves — "date of birth is not in the future" is implied by the
/// field's own meaning, not an invented rule.
/// </summary>
public sealed class CreationUpdateValidator : AbstractValidator<CreationFieldValues>
{
    public CreationUpdateValidator()
    {
        RuleFor(x => x).Custom((fields, context) =>
        {
            if (fields.TryGetValue("dateOfBirth", out var value) && value is string text && !string.IsNullOrWhiteSpace(text)
                && DateTime.TryParse(text, out var dateOfBirth) && dateOfBirth.Date > DateTime.UtcNow.Date)
            {
                context.AddFailure("dateOfBirth", "Date of birth cannot be in the future.");
            }
        });
    }
}
