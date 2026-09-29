using Microsoft.AspNetCore.Mvc;
using Portal.Api.Authorization;
using Portal.Api.Common;
using Portal.Application.Common;
using Portal.Application.Screens;

namespace Portal.Api.Controllers;

/// <summary>Read-only view of dynamic screen metadata for admins — docs/07 §6:
/// screens are seeded from code (ScreenSeeder), never admin-edited at runtime.</summary>
[Route("api/v1/admin/screens")]
[HasPermission(PermissionCodes.AdminScreens)]
public sealed class AdminScreensController(IScreenService screenService) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await screenService.ListAllAsync(ct), CorrelationId));
}
