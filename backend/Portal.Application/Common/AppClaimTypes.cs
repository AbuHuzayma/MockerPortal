namespace Portal.Application.Common;

/// <summary>Custom JWT claim types, shared between token issuance (Infrastructure) and authorization (Api).</summary>
public static class AppClaimTypes
{
    /// <summary>One claim per effective permission code (see docs/04-authentication-authorization.md §3).</summary>
    public const string Permission = "permission";
}
