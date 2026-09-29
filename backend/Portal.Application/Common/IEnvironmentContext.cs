namespace Portal.Application.Common;

/// <summary>
/// The resolved business environment (DEV/QA/PREPROD) for the running instance.
/// Distinct from ASP.NET Core's own hosting environment name. See docs/12-environments.md.
/// </summary>
public interface IEnvironmentContext
{
    /// <summary>DEV, QA, or PREPROD.</summary>
    string Name { get; }

    /// <summary>Safe-to-expose metadata for the frontend (no secrets, no connection details).</summary>
    EnvironmentInfo ToPublicInfo();
}

public sealed record EnvironmentInfo(string Name, string Label, string Color);
