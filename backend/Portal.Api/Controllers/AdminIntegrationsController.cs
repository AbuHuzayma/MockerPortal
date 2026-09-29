using Microsoft.AspNetCore.Mvc;
using Portal.Api.Authorization;
using Portal.Api.Common;
using Portal.Application.Common;

namespace Portal.Api.Controllers;

/// <summary>
/// Read-only status view of every provider's Mock/Sql/Real mode — docs/12 §2.
/// Mode itself is configuration (appsettings/env vars per environment), not
/// something this admin UI can change at runtime; changing it is a deployment
/// action, reviewed like any other config change.
/// </summary>
[Route("api/v1/admin/integrations")]
[HasPermission(PermissionCodes.AdminIntegrations)]
public sealed class AdminIntegrationsController(IConfiguration configuration, IEnvironmentContext environmentContext) : ApiControllerBase
{
    private static readonly string[] ProviderNames =
    [
        "Customer", "Kyc", "Ivr", "Creation", "Otp", "Biometric",
        "CardManagement", "BeneficiaryService", "Merchant", "B2BService",
    ];

    [HttpGet]
    public IActionResult List()
    {
        var statuses = ProviderNames.Select(name => new
        {
            name,
            mode = configuration[$"Providers:{name}:Mode"] ?? "Mock",
            baseUrl = configuration[$"Integrations:{name}:BaseUrl"],
        });

        return Ok(ApiResponse<object>.Ok(new { environment = environmentContext.Name, providers = statuses }, CorrelationId));
    }
}
