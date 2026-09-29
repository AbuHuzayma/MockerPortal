using Microsoft.AspNetCore.Mvc;
using Portal.Api.Authorization;
using Portal.Api.Common;
using Portal.Application.Audit;
using Portal.Application.Common;

namespace Portal.Api.Controllers;

[Route("api/v1/audit")]
[HasPermission(PermissionCodes.AuditView)]
public sealed class AuditController(IAuditQueryService auditQueryService) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Query(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] string? username,
        [FromQuery] string? customerId,
        [FromQuery] string? merchantId,
        [FromQuery] string? screen,
        [FromQuery] string? entity,
        [FromQuery] string? result,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = new AuditLogQuery(fromUtc, toUtc, username, customerId, merchantId, screen, entity, result, page, pageSize);
        var pageResult = await auditQueryService.QueryAsync(query, ct);
        return Ok(ApiResponse<object>.Ok(pageResult, CorrelationId));
    }
}
