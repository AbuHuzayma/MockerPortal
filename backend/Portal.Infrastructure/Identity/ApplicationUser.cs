using Microsoft.AspNetCore.Identity;

namespace Portal.Infrastructure.Identity;

/// <summary>
/// ASP.NET Core Identity user. Identity's own concerns (password hash, lockout
/// counters, security stamp) are inherently persistence/infrastructure details,
/// so this type lives here rather than in Portal.Domain — see docs/01-architecture.md.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public required string FullName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
