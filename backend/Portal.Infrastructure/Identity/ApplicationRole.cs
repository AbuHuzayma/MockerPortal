using Microsoft.AspNetCore.Identity;

namespace Portal.Infrastructure.Identity;

public sealed class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }

    public ApplicationRole() { }

    public ApplicationRole(string name) : base(name) { }
}
