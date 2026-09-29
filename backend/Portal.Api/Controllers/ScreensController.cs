using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.Api.Common;
using Portal.Application.Common;
using Portal.Application.Screens;

namespace Portal.Api.Controllers;

/// <summary>
/// Dynamic screen metadata — see docs/07-dynamic-screen-engine.md. Any
/// authenticated user may request a screen's definition; the response itself
/// is filtered to their permissions (fields/actions they lack access to are
/// omitted, and a screen-level ScreenPermissions gate can forbid it entirely).
/// </summary>
[Authorize]
public sealed class ScreensController(IScreenService screenService) : ApiControllerBase
{
    [HttpGet("{code}")]
    public async Task<IActionResult> Get(string code, CancellationToken ct)
    {
        var callerPermissions = User.FindAll(AppClaimTypes.Permission).Select(c => c.Value).ToHashSet();

        var result = await screenService.GetScreenDefinitionAsync(code, callerPermissions, ct);
        if (result.IsSuccess)
        {
            return Ok(ApiResponse<object>.Ok(result.Value, CorrelationId));
        }

        var error = new ApiError { Code = result.ErrorCode!, Message = result.ErrorMessage! };
        return result.ErrorCode == ScreenErrorCodes.ScreenForbidden
            ? StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(error, CorrelationId))
            : NotFound(ApiResponse<object>.Fail(error, CorrelationId));
    }
}
